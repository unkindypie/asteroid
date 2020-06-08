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
        static public readonly byte MaxUserCount = 1;
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
            public Action StartSending;

            //чисто внутренний сигнал, котого будет ждать sender чтоб продолжить работать
            public AutoResetEvent allAcknowlegesCameSignal = new AutoResetEvent(false);
            public volatile bool senderShouldStop = true;

            public byte CheckpointInterval
            {
                get
                {
                    return checkpointInterval;
                }
            }

            public SharedThreadScope(byte checkpointInterval, int sleepTime, string ownerName, Action startSenderThread)
            {
                this.checkpointInterval = checkpointInterval;
                this.sleepTime = sleepTime;
                maxSleepTime = (int)(maxSleepTime * 1.15);
                this.ownerName = ownerName;
                StartSending = startSenderThread;

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
            public long LastAcknowlegmentCheckpoint { get; set; } = -1;
            public int AverageFrameTime { get; set; } = 0;
        }


        public NetGameServer(byte checkpointInterval, string ownerName = "Mr. Smith")
        {
            this.checkpointInterval = checkpointInterval;

            //общая область вызова для потоков
            scope = new SharedThreadScope(checkpointInterval, (int)(SyncSimulation.Delta * 1000), ownerName, this.StartSending)
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
                    ).GetBytes())
                { 
                    PackageType = OwnerPackageType.AccumulatedRemoteActions
                }.GetBytes();
                //Debug.WriteLine($"Sending {packagedActions.Length}", "server-sender");
                foreach (RoomMember member in scope.members)
                {
                    averageTime += member.AverageFrameTime;

                    // отправка собранных действий
                    scope.client.Send(packagedActions, packagedActions.Length, member.EndPoint);
                }

                foreach (var frame in scope.accumulatedActions) frame.Clear();

                if(scope.members.Count != 0) averageTime /= scope.members.Count;
                  
                // жду, пока все подтверждения придут
                if(scope.senderShouldStop)
                {
                    DateTime s = DateTime.Now;

                    scope.allAcknowlegesCameSignal.WaitOne();
                    //Task.Run(() => Debug.WriteLine("Server slept for " + (DateTime.Now - s).TotalMilliseconds.ToString() + " on " + scope.CurrentCheckpoint));

                    scope.senderShouldStop = true;
                }
                var pSyncDone = (new OwnerPackage(
                    new OPSynchronizationDone() { Checkpoint = scope.CurrentCheckpoint }.GetBytes()
                    )
                { PackageType = OwnerPackageType.SynchronizationDone }).GetBytes();
                // разсылаю разрешение на выполнение учасникам комнаты
                foreach (RoomMember member in scope.members)
                {
                    scope.client.Send(pSyncDone, pSyncDone.Length, member.EndPoint);
                }

                scope.CurrentCheckpoint++;
                scope.IsInSynchronizationState = false;

                int waitingTime =  // 1000/60
                    (scope.SleepTime +
                    // плюс среднее время выполнения кадра у пользователей
                    averageTime) * scope.CheckpointInterval -
                        // минус время выполнения тика SenderFunction
                        (int)((DateTime.Now - sendingStarted).TotalMilliseconds);

                //waitingTime = 70;
                //Debug.WriteLine($"Sender update: {waitingTime} ms, {packagedActions.Length} b", "server");
                //Debug.WriteLine($"Server checkpoint: {scope.CurrentCheckpoint}");
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
            Debug.WriteLine($"Listener is up on port {NetGameServer.RoomHostPort}", "server");
            SynchronizedList<Tuple<IPEndPoint, DateTime>> broadcastingMeets 
                = new SynchronizedList<Tuple<IPEndPoint, DateTime>>();
            while (true)
            {
                IPEndPoint remoteEndPoint = null;
                byte[] received = scope.client.Receive(ref remoteEndPoint);
                
                //отправляю обработку в тредпул
                Task.Run(() => {
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
                            { PackageType = OwnerPackageType.BroadcastScanningAnswer }.GetBytes();

                            Debug.WriteLine("Answering to broadcast scan from " + remoteEndPoint.ToString(), "server");
                            scope.client.Send(broadcastAnswer, broadcastAnswer.Length, remoteEndPoint);
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
                                    
                                //заполняю ответ
                                response = (new OwnerPackage(null) { PackageType = OwnerPackageType.RoomEnterRequestAcception }).GetBytes();
                            }
                            else
                            {
                                //отказ
                                response = (new OwnerPackage(null) { PackageType = OwnerPackageType.RoomEnterRequestRejection }).GetBytes();
                            }
                            scope.client.Send(response, response.Length, remoteEndPoint);
                            if (scope.members.Count >= MaxUserCount)
                            {
                                byte[] startAllowedPkg = new OwnerPackage(null)
                                {
                                    PackageType = OwnerPackageType.StartAllowed
                                }.GetBytes();
                                foreach(var member in scope.members)
                                {
                                    scope.client.Send(startAllowedPkg, startAllowedPkg.Length, member.EndPoint);
                                }
                                scope.client.EnableBroadcast = false;
                                scope.StartSending();
                            }
                            break;
                        case MemberPackageType.ActionsAcknowledgment:
                            int receivedAcknowlgegmentCount = 0;
                            foreach (RoomMember member in scope.members)
                            {
                                //обработка подтверждения
                                if (member.EndPoint.GetHashCode() == remoteEndPoint.GetHashCode())
                                {
                                    var acknowledgment = (MPActionsAcknowledgment)memberPackageContent;
                                    member.AverageFrameTime = acknowledgment.AverageFrameExecutionTime;
                                    member.LastAcknowlegmentCheckpoint = (long)acknowledgment.Checkpoint;
                                }
                                if(member.LastAcknowlegmentCheckpoint == (long)scope.CurrentCheckpoint)
                                {
                                    receivedAcknowlgegmentCount++;
                                }
                            }
                            if(receivedAcknowlgegmentCount == scope.members.Count)
                            {
                                scope.senderShouldStop = false;
                                scope.allAcknowlegesCameSignal.Set();
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
                            Debug.WriteLine($"Got action from {action.Checkpoint} being on {scope.CurrentCheckpoint}", "server");
                            break;
                    }
                });  
            }
        }
    }
}