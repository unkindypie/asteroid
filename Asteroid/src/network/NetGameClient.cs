using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;

using Asteroid.src.utils;

namespace Asteroid.src.network
{

    class NetGameClient
    {
        SharedThreadScope scope;
        Thread listenerThread;

        class SharedThreadScope
        {
            public UdpClient client;
            public volatile bool isConnected = false;
            public volatile IPEndPoint serverEndPoint;
            public volatile bool isListenerRunning = false;
            public volatile SynchronizedList<SynchronizedList<RemoteActionBase>> receivedActions;

            // можно ли Synchronizer'у продолжать работу, когда он зашел в чекпоинт
            public AutoResetEvent synchronizerCanWorkSignal = new AutoResetEvent(false);
            public volatile bool synchronizerShouldStopFlag = true;
            // можно ли слать Acknowlegment 
            public AutoResetEvent synchronizersWorkDoneSignal = new AutoResetEvent(false);
            public volatile bool isGameStarted = false;

            public ulong lastRecievedActionsCheckpoint = 0;
        }

        public NetGameClient()
        {
            scope = new SharedThreadScope() {
                client = new UdpClient()
            };
        }

        public AutoResetEvent SynchronizerCanContinue => scope.synchronizerCanWorkSignal;
        public AutoResetEvent SynchronizationDoneSignal => scope.synchronizersWorkDoneSignal;

        public SynchronizedList<SynchronizedList<RemoteActionBase>> ReceivedActions => scope.receivedActions;

        public bool IsGameStarted => scope.isGameStarted;

        public bool SynchronizerShouldStopFlag
        {
            get {
                return scope.synchronizerShouldStopFlag;
            }
            set
            {
                scope.synchronizerShouldStopFlag = value;
            }
        }

        /// <summary>
        /// Синхронно шлет широковещательную дейтаграмму на порт сервера и ждет ответа
        /// </summary>
        /// <returns></returns>
        public Dictionary<IPEndPoint, OPRoomInfo> ScanNetwork()
        {
            Dictionary<IPEndPoint, OPRoomInfo> rooms = new Dictionary<IPEndPoint, OPRoomInfo>();

            scope.client.EnableBroadcast = true;
            var broadcastEp = new IPEndPoint(IPAddress.Broadcast, NetGameServer.RoomHostPort);
            Debug.WriteLine($"IPv4 Broadasting scan-package on mask {broadcastEp}", "client");
            var broadcastPackage = MemberPackage.BroadcastScanningPackage.GetBytes();
            scope.client.Client.ReceiveTimeout = 200;
            // TODO(в NetGameServer): если этот EP уже недавно слал BroadcastScanning,
            // то ему ответ не слать
            for (int i = 0; i < 10; i++)
            {
                scope.client.Send(broadcastPackage, broadcastPackage.Length, broadcastEp);
                IPEndPoint serverEp = null;
                try
                {
                    var response = new OwnerPackage(scope.client.Receive(ref serverEp));
                    var roomInfo = response.Parse();
                    if (!rooms.ContainsKey(serverEp) 
                        && response.PackageType == OwnerPackageType.BroadcastScanningAnswer)
                    {
                        rooms.Add(serverEp, (OPRoomInfo)roomInfo);
                    }
                } catch(SocketException) //прошел таймаут
                {
                    break;
                }

            }
            scope.client.Client.ReceiveTimeout = -1;
            return rooms;
        }

        public bool TryConnect(IPEndPoint serverEP, string username)
        {
            if (scope.isListenerRunning) throw new Exception("Listener is running, can't receive RoomEnterRequestAcception");
            //если через 300 мс не ответят, то сервер не сервер
            scope.client.Client.ReceiveTimeout = 300;
            //запрашиваю вход
            var mp = new MemberPackage(
                (new MPRoomEnterRequest() { Username = username }).GetBytes()
                )
            { PackageType = MemberPackageType.RoomEnterRequest }.GetBytes();

            scope.client.Send(mp, mp.Length, serverEP);
            try
            {
                //жду и парсю ответ
                var ownerPackage = new OwnerPackage(scope.client.Receive(ref serverEP));
                ownerPackage.Parse();
                scope.client.Client.ReceiveTimeout = -1;
                //вход удался
                if (ownerPackage.PackageType == OwnerPackageType.RoomEnterRequestAcception)
                {
                    scope.client.Connect(serverEP);
                    scope.isConnected = true;
                    scope.serverEndPoint = serverEP;
                    //запускаю поток, читающий прослушиваемый сокет и обрабатывающий пакеты
                    listenerThread = new Thread(ListenerThreadFunction);
                    listenerThread.IsBackground = true;
                    listenerThread.Start(scope);
                    return true;
                }
                else
                {
                    return false;
                }
            } catch(SocketException) //если таймаут в 300 мс прошел, то сервер не сервер
            {
                scope.client.Client.ReceiveTimeout = -1;
                return false;
            }
        }
        /// <summary>
        /// Ассинхронно отправляет действие подключенному серверу без гарантии доставки
        /// </summary>
        /// <param name="action"></param>
        public async void SendAction(RemoteActionBase action)
        {
            if(scope.isConnected)
            {
                var memberPackage = new MemberPackage(Parser.SerealizeAction(action))
                {
                    PackageType = MemberPackageType.RemoteAction
                }.GetBytes();
                await scope.client.SendAsync(memberPackage, memberPackage.Length);
            }
        }

        static void ListenerThreadFunction(object _scope)
        {
            SharedThreadScope scope = (SharedThreadScope)_scope;

            while(true)
            {
                IPEndPoint sender = null;
                var received = scope.client.Receive(ref sender);
                Task.Run(() =>
                {
                   
                    if (sender.GetHashCode() == scope.serverEndPoint.GetHashCode())
                    {
                        OwnerPackage ownerPackage = new OwnerPackage(received);
                        var pData = ownerPackage.Parse();
                        switch (ownerPackage.PackageType)
                        {
                            case OwnerPackageType.AccumulatedRemoteActions:
                                //сереализую действия
                                scope.receivedActions = (pData as OPAccumulatedActions).Actions;
                                //Debug.WriteLine($"Deserealized {ownerPackage.Data.Length} of actions", "net-client");
                                //разблокирываю основой поток
                                scope.synchronizerShouldStopFlag = false;
                                scope.synchronizerCanWorkSignal.Set();
                                //жду пока основой поток сольет действия в свой буфер
                                scope.synchronizersWorkDoneSignal.WaitOne();
                                //отправляю подтверждение серверу
                                byte[] package = new MemberPackage(new MPActionsAcknowledgment()
                                {
                                    Checkpoint = (pData as OPAccumulatedActions).Checkpoint,
                                    AverageFrameExecutionTime = 0
                                }.GetBytes())
                                {
                                    PackageType = MemberPackageType.ActionsAcknowledgment
                                }.GetBytes();
                                scope.client.Send(package, package.Length);
                                scope.lastRecievedActionsCheckpoint = (pData as OPAccumulatedActions).Checkpoint;
                                break;
                            case OwnerPackageType.SynchronizationDone:
                                if((pData as OPSynchronizationDone).Checkpoint == scope.lastRecievedActionsCheckpoint)
                                {
                                    scope.synchronizerCanWorkSignal.Set();
                                }
                                break;
                            case OwnerPackageType.StartAllowed:
                                scope.isGameStarted = true;
                                break;
                            default:
                                break;
                        }
                    }
                });        
            }
        }
    }
}
