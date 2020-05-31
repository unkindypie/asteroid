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

namespace Asteroid.src.network
{
    class SharedThreadScope
    {
        byte checkpointInterval;

        public int SleepTime { get; set; } // мс
        public ulong CurrentCheckpoint { get; set; }
        public int UserCount { get; set; }
        public SynchronizedList<SynchronizedList<IRemoteAction>> accumulatedActions;
        public SynchronizedList<RoomMember> members;


        public byte CheckpointInterval {
            get
            {
                return checkpointInterval;
            }
        }

        public SharedThreadScope(byte checkpointInterval)
        {
            this.checkpointInterval = checkpointInterval;
            accumulatedActions = new SynchronizedList<SynchronizedList<IRemoteAction>>();
            for (byte i = 0; i < checkpointInterval; i++)
            {
                accumulatedActions.Add(new SynchronizedList<IRemoteAction>());
            }
            members = new SynchronizedList<RoomMember>();
        }
    }
    class RoomMember
    {
        public IPEndPoint SendEndPoint { get; set; }
        public IPEndPoint RecvEndPoint { get; set; }
        public ulong LastAcknowlegmentCheckpoint { get; set; } = 0;
        public int AverageFrameTime { get; set; } = 0;
    }
    class NetGameServer
    {
        static public readonly ushort RoomHostPort = 2557;
        byte checkpointInterval;
        Thread senderThread;
        Thread recieverThread;

        SharedThreadScope scope;


        public NetGameServer(byte checkpointInterval)
        {
            this.checkpointInterval = checkpointInterval;
           
            //общая область вызова для потоков
            scope = new SharedThreadScope(checkpointInterval)
            { 
                SleepTime = (int)(SyncSimulation.Delta),
                CurrentCheckpoint = 0
            };
        }

        public void StartSending()
        {
            senderThread = new Thread(SenderFunction);
            senderThread.IsBackground = true;
            senderThread.Start(scope);
        }

        static void SenderFunction(object arg)
        {
            SharedThreadScope scope = (SharedThreadScope)arg;
            DateTime sendingStarted;
            UdpClient udpClient = new UdpClient();

            while (true)
            {
                sendingStarted = DateTime.Now;
                // высчитывание времени сна(зависит от среднего времени кадра) и отправка 
                int averageTime = 0;
                foreach(RoomMember member in scope.members)
                {
                    averageTime += member.AverageFrameTime;
                    // отправка собранных действий
                    //udpClient.Send(..., ..., member.EndPoint);
                }
                averageTime /= scope.members.Count;

                scope.CurrentCheckpoint++;
                Thread.Sleep(
                    // 1000/60
                    (scope.SleepTime + 
                    // плюс среднее время выполнения кадра у пользователей
                    averageTime) * scope.CheckpointInterval - 
                    // минус время выполнения тика SenderFunction
                    (int)((DateTime.Now - sendingStarted).TotalMilliseconds));
            }
        }
        static void ReceiverFunction(object arg)
        {
            SharedThreadScope scope = (SharedThreadScope)arg;
            UdpClient udpClient = new UdpClient(RoomHostPort);

            while (true)
            {
                IPEndPoint remoteEndPoint = null;
                byte[] received = udpClient.Receive(ref remoteEndPoint);
                //todo: мб стоит выделить по сокету и треду на каждого юзера, хз как лучше
                MemberPackage _memberPackage = new MemberPackage(received);
                var memberPackage = _memberPackage.Parse();
                switch (_memberPackage.PackageType)
                {
                    case MemberPackageType.RoomEnterRequest:
                        //запись в member.RecvEndPoint его endpoint'а по проту
                        break;
                    case MemberPackageType.Acknowledgment:
                        foreach(RoomMember member in scope.members)
                        {
                            //обработка подтверждения
                            if(member.SendEndPoint == remoteEndPoint)
                            {
                                var acknowledgment = (Acknowledgment)memberPackage;
                                member.AverageFrameTime = acknowledgment.AverageFrameExecutionTime;
                                member.LastAcknowlegmentCheckpoint = acknowledgment.Checkpoint;
                            }
                        }
                        break;
                    case MemberPackageType.RemoteAction:
                        break;
                }
            }
        }
    }
}