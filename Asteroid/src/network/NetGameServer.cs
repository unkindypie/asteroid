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
        static public readonly byte MaxUserCount = 2;
        byte checkpointInterval;
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
            public long LastAcknowlegmentCheckpoint { get; set; } = -1;
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

        public void Listen()
        {
            recieverThread = new Thread(ReceiverFunction);
            recieverThread.IsBackground = true;
            recieverThread.Start(scope);
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
                //чтобы на линуксе не кидало argument null exception(моно - говно)
                IPEndPoint remoteEndPoint = new IPEndPoint(0, 8000);
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

                            
                            int r = scope.client.Send(broadcastAnswer, broadcastAnswer.Length, remoteEndPoint);
                            Debug.WriteLine($"Answering {r} bytes to broadcast scan from " + remoteEndPoint.ToString(), "server");
                            break;
                        case MemberPackageType.RoomEnterRequest:
                            byte[] response;
                            Console.WriteLine("Answering to broadcast from " + remoteEndPoint);
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
                            if (scope.members.Count == MaxUserCount)
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
                                    member.LastAcknowlegmentCheckpoint = (long)acknowledgment.Checkpoint;
                                }
                                if(member.LastAcknowlegmentCheckpoint == (long)scope.CurrentCheckpoint)
                                {
                                    receivedAcknowlgegmentCount++;
                                }
                            }
                            if(receivedAcknowlgegmentCount == scope.members.Count)
                            {
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
                                foreach (RoomMember member in scope.members)
                                {
                                    // отправка собранных действий
                                    scope.client.Send(packagedActions, packagedActions.Length, member.EndPoint);
                                }
                                foreach (var frame in scope.accumulatedActions) frame.Clear();
                                scope.CurrentCheckpoint++;
                                scope.IsInSynchronizationState = false;
                                Debug.WriteLine($"Sent actions chunk for {scope.CurrentCheckpoint - 1}", "server");
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