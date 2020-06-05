using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;

using Asteroid.src.physics;
using Asteroid.src.utils;
using System.Diagnostics;

namespace Asteroid.src.network
{
    // TODO: volatile в переменных, которые используются везде

    class NetGameServer
    {
        static public readonly ushort RoomHostPort = 2557;
        static public readonly byte MaxUserCount = 4;
        byte checkpointInterval;
        Thread senderThread;
        Thread recieverThread;

        //нужен чтобы расшаривать ресурсы между тредами
        SharedThreadScope scope;
        class SharedThreadScope
        {
            byte checkpointInterval;
            int sleepTime;
            int maxSleepTime;
            string ownerName;

            public int SleepTime => sleepTime; // мс
            public int MaxSleepTime => maxSleepTime;
            public ulong CurrentCheckpoint { get; set; }
            public byte UserCount { get; set; }
            public SynchronizedList<SynchronizedList<RemoteActionBase>> accumulatedActions;
            public SynchronizedList<RoomMember> members;
            public UdpClient client;
            public volatile bool IsInSynchronizationState = false;
            public string OwnerName => ownerName;
            public byte CheckpointInterval
            {
                get
                {
                    return checkpointInterval;
                }
            }

            public SharedThreadScope(byte checkpointInterval, int sleepTime, string ownerName)
            {
                this.checkpointInterval = checkpointInterval;
                this.sleepTime = sleepTime;
                maxSleepTime = (int)(maxSleepTime * 1.15);
                this.ownerName = ownerName;

                accumulatedActions = new SynchronizedList<SynchronizedList<RemoteActionBase>>();
                for (byte i = 0; i < checkpointInterval; i++)
                {
                    accumulatedActions.Add(new SynchronizedList<RemoteActionBase>());
                }
                members = new SynchronizedList<RoomMember>();
            }
        }
        class RoomMember
        {
            public IPEndPoint EndPoint { get; set; }
            public ulong LastAcknowlegmentCheckpoint { get; set; } = 0;
            public int AverageFrameTime { get; set; } = 0;
        }


        public NetGameServer(byte checkpointInterval, string ownerName = "Mr. Smith")
        {
            this.checkpointInterval = checkpointInterval;

            //общая область вызова для потоков
            scope = new SharedThreadScope(checkpointInterval, (int)(SyncSimulation.Delta * 1000), ownerName)
            {
                CurrentCheckpoint = 0,
                client = new UdpClient(RoomHostPort),
            };
            scope.client.EnableBroadcast = true;
        }

        public void StartSending()
        {
            senderThread = new Thread(SenderFunction);
            senderThread.IsBackground = true;
            senderThread.Start(scope);
        }

        public void Listen()
        {
            recieverThread = new Thread(ReceiverFunction);
            recieverThread.IsBackground = true;
            recieverThread.Start(scope);
        }

        static void SenderFunction(object arg)
        {
            SharedThreadScope scope = (SharedThreadScope)arg;
            DateTime sendingStarted;

            //TODO: избавиться от averageTime, если на клиенте все всегда работает быстро за 0 мс
            while (true)
            {
                sendingStarted = DateTime.Now;
                // высчитывание времени сна(зависит от среднего времени кадра) и отправка 
                int averageTime = 0;
                scope.IsInSynchronizationState = true;

                byte[] packagedActions = new OwnerPackage(
                        (new OPAccumulatedActions()
                        {
                            Checkpoint = scope.CurrentCheckpoint,
                            Actions = scope.accumulatedActions,
                        }
                        ).GetBytes()
                    )
                {
                    PackageType = OwnerPackageType.AccumulatedRemoteActions
                }.GetBytes();

                foreach (RoomMember member in scope.members)
                {
                    averageTime += member.AverageFrameTime;

                    // отправка собранных действий
                    scope.client.Send(packagedActions, packagedActions.Length);
                }

                foreach (var frame in scope.accumulatedActions) frame.Clear();

                if(scope.members.Count != 0) averageTime /= scope.members.Count;

                //TODO: тут стопить до момента, пока не придут подтверждения
                //(и waitingTime не считать, удачи там)

                scope.CurrentCheckpoint++;
                scope.IsInSynchronizationState = false;
 
                int waitingTime =  // 1000/60
                    (scope.SleepTime +
                    // плюс среднее время выполнения кадра у пользователей
                    averageTime) * scope.CheckpointInterval -
                    // минус время выполнения тика SenderFunction
                    (int)((DateTime.Now - sendingStarted).TotalMilliseconds);

                Debug.WriteLine($"Sender update: {waitingTime} ms, {packagedActions.Length} b", "server");

                Thread.Sleep(
                    waitingTime  < 0 ? 0 : (waitingTime > 80 ? 80 : waitingTime)
                   );
            }
        }

        static void ReceiverFunction(object arg)
        {
            SharedThreadScope scope = (SharedThreadScope)arg;
            //этот список нужен, чтобы не овечать каждый раз на BroadcastScanning 
            // от одного и того же ep
            SynchronizedList<Tuple<IPEndPoint, DateTime>> broadcastingMeets 
                = new SynchronizedList<Tuple<IPEndPoint, DateTime>>();
            while (true)
            {
                IPEndPoint remoteEndPoint = null;
                byte[] received = scope.client.Receive(ref remoteEndPoint);
                
                //отправляю обработку в тредпул
                Task.Run(() => {
                    if (scope.IsInSynchronizationState) return;
                    MemberPackage memberPackage = new MemberPackage(received);
                    var memberPackageContent = memberPackage.Parse();
                    switch (memberPackage.PackageType)
                    {
                        case MemberPackageType.BroadcastScanning:
                            // если этот EP уже недавно слал BroadcastScanning,
                            // то ему ответ не слать
                            var now = DateTime.Now;
                            broadcastingMeets.RemoveAll(t => (now - t.Item2).TotalMilliseconds > 1000);
                            foreach(var t in broadcastingMeets)
                            {
                                if (t.Item1.Port == remoteEndPoint.Port 
                                && t.Item1.Address.GetHashCode() == remoteEndPoint.Address.GetHashCode()) return;
                            }
                            broadcastingMeets.Add(new Tuple<IPEndPoint, DateTime>(remoteEndPoint, now));

                            //отправляю данные о комнате
                            var broadcastAnswer = new OwnerPackage((new OPRoomInfo()
                            {
                                OwnersName = scope.OwnerName,
                                MaxUserCount = MaxUserCount,
                                UserCount = scope.UserCount,
                            }).GetBytes())
                            { PackageType = OwnerPackageType.BroadcastScanningAnswer };

                            Debug.WriteLine("Answering to broadcast scan from " + remoteEndPoint.ToString(), "server");
                            scope.client.Send(broadcastAnswer.Data, broadcastAnswer.Data.Length, remoteEndPoint);
                            break;
                        case MemberPackageType.RoomEnterRequest:
                            byte[] response;
                            //добавляю в комнату
                            if(scope.members.Count < MaxUserCount)
                            {
                                scope.members.Add(new RoomMember()
                                {
                                    EndPoint = remoteEndPoint
                                });

                                if(scope.members.Count >= MaxUserCount)
                                    scope.client.EnableBroadcast = false;
                                //заполняю ответ
                                response = (new OwnerPackage(null) { PackageType = OwnerPackageType.RoomEnterRequestAcception }).GetBytes();
                            }
                            else
                            {
                                //отказ
                                response = (new OwnerPackage(null) { PackageType = OwnerPackageType.RoomEnterRequestRejection }).GetBytes();
                            }
                            scope.client.Send(response, response.Length, remoteEndPoint);
                            break;
                        case MemberPackageType.ActionsAcknowledgment:
                            foreach (RoomMember member in scope.members)
                            {
                                //TODO: разсылай SynhronizationDone когда все подтвердили 

                                //обработка подтверждения
                                if (member.EndPoint == remoteEndPoint)
                                {
                                    var acknowledgment = (MPActionsAcknowledgment)memberPackageContent;
                                    member.AverageFrameTime = acknowledgment.AverageFrameExecutionTime;
                                    member.LastAcknowlegmentCheckpoint = acknowledgment.Checkpoint;
                                }
                            }
                            break;
                        case MemberPackageType.RemoteAction:
                            if (scope.IsInSynchronizationState) return;
                            //TODO: контролить ситуацию, когда юзер спамит RemoteAction'ами
                            var action = (RemoteActionBase)memberPackageContent;
                            if (action.Checkpoint >= scope.CurrentCheckpoint)
                            {
                                scope.accumulatedActions[action.Frame].Add(action);
                            }
                            break;
                    }
                });  
            }
        }
    }
}