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
    class RoomInfo
    {
        /// <summary>
        /// OwnersName - ASCII string
        /// </summary>
        public string OwnersName { get; set; }  
        public byte UserCount { get; set; }
        public byte MaxUserCount { get; set; }

        public static RoomInfo FromBytes(byte[] data)
        {
            var r = new RoomInfo();
            r.OwnersName = Encoding.ASCII.GetString(data, 4, BitConverter.ToInt32(data, 0));
            r.MaxUserCount = data[r.OwnersName.Length + 4];
            r.MaxUserCount = data[r.OwnersName.Length + 5];
            return r;
        }
        public byte[] GetBytes()
        {
            if(OwnersName.Length > 20)
            {
                OwnersName = OwnersName.Substring(0, 20);
            }  
            return BitConverter
                .GetBytes(OwnersName.Length)
                .Concat(Encoding.ASCII.GetBytes(OwnersName))
                .Concat(new byte[] { UserCount, MaxUserCount }).ToArray();
        }
    }
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
            public bool IsInSynchronizationState = false;
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

                byte[] sActions = Parser.SerealizeAccumulatedActions(scope.accumulatedActions);

                foreach (RoomMember member in scope.members)
                {
                    averageTime += member.AverageFrameTime;

                    // отправка собранных действий
                    scope.client.Send(sActions, sActions.Length);
                }

                foreach (var frame in scope.accumulatedActions) frame.Clear();

                if(scope.members.Count != 0) averageTime /= scope.members.Count;

                scope.CurrentCheckpoint++;
                scope.IsInSynchronizationState = false;
 
                int waitingTime =  // 1000/60
                    (scope.SleepTime +
                    // плюс среднее время выполнения кадра у пользователей
                    averageTime) * scope.CheckpointInterval -
                    // минус время выполнения тика SenderFunction
                    (int)((DateTime.Now - sendingStarted).TotalMilliseconds);

                Debug.WriteLine($"Sender update: {waitingTime} ms, {sActions.Length} b", "server");

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
                if (scope.IsInSynchronizationState) continue;
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
                                if (t.Item1.Address.GetHashCode() == remoteEndPoint.Address.GetHashCode())
                                {
                                    Debug.WriteLine("already have it");
                                    return;
                                }
                                
                            }
                            broadcastingMeets.Add(new Tuple<IPEndPoint, DateTime>(remoteEndPoint, now));
                            //отправляю данные о комнате
                            var roomInfo = (new RoomInfo()
                            {
                                OwnersName = scope.OwnerName,
                                MaxUserCount = MaxUserCount,
                                UserCount = scope.UserCount,
                            }).GetBytes();
                            Debug.WriteLine("Answering to broadcast from " + remoteEndPoint.ToString(), "server");
                            scope.client.Send(roomInfo, roomInfo.Length, remoteEndPoint);
                            break;
                        case MemberPackageType.RoomEnterRequest:
                            //добавляю в комнату
                            if(scope.members.Count < MaxUserCount)
                            {
                                scope.members.Add(new RoomMember()
                                {
                                    EndPoint = remoteEndPoint
                                });

                                if(scope.members.Count >= MaxUserCount)
                                    scope.client.EnableBroadcast = false;
                            }
                            break;
                        case MemberPackageType.Acknowledgment:
                            foreach (RoomMember member in scope.members)
                            {
                                //обработка подтверждения
                                if (member.EndPoint == remoteEndPoint)
                                {
                                    var acknowledgment = (Acknowledgment)memberPackageContent;
                                    member.AverageFrameTime = acknowledgment.AverageFrameExecutionTime;
                                    member.LastAcknowlegmentCheckpoint = acknowledgment.Checkpoint;
                                }
                            }
                            break;
                        case MemberPackageType.RemoteAction:
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