using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;

using Asteroid.Core.utils;

namespace Asteroid.Core.network
{

    class NetGameClient
    {
        SharedThreadScope scope;
        Thread listenerThread;

        class SharedThreadScope
        {
            public UdpClient client;
            public string username;
            public volatile bool isConnected = false;
            public volatile IPEndPoint serverEndPoint;
            public volatile bool isListenerRunning = false;
            public volatile SynchronizedList<SynchronizedList<RemoteActionBase>> receivedActions;

            // можно ли Synchronizer'у продолжать работу, когда он зашел в чекпоинт
            public AutoResetEvent accumulatedActionsCameSignal = new AutoResetEvent(false);
            public volatile bool shouildWaitActionsSignal = true;
            public volatile bool isGameStarted = false;

            public ulong lastRecievedActionsCheckpoint = 0;
        }

        public NetGameClient(string username)
        {
            scope = new SharedThreadScope() {
                client = new UdpClient()
            };
            scope.username = username;
        }

        public SynchronizedList<SynchronizedList<RemoteActionBase>> ReceivedActions => scope.receivedActions;

        public bool IsGameStarted => scope.isGameStarted;


        /// <summary>
        /// Синхронно шлет широковещательную дейтаграмму на порт сервера и ждет ответа
        /// </summary>
        /// <returns></returns>
        public Dictionary<IPEndPoint, OPRoomInfo> ScanNetwork()
        {
            Dictionary<IPEndPoint, OPRoomInfo> rooms = new Dictionary<IPEndPoint, OPRoomInfo>();

            scope.client.EnableBroadcast = true;
            var broadcastEp = new IPEndPoint(IPAddress.Broadcast, NetGameServer.RoomHostPort);
            Console.WriteLine($"IPv4 Broadasting scan-package on mask {broadcastEp}", "client");
            var broadcastPackage = MemberPackage.BroadcastScanningPackage.GetBytes();
            scope.client.Client.ReceiveTimeout = 200;
            // TODO(в NetGameServer): если этот EP уже недавно слал BroadcastScanning,
            // то ему ответ не слать
            for (int i = 0; i < 10; i++)
            {
                int r = scope.client.Send(broadcastPackage, broadcastPackage.Length, broadcastEp);
                Console.WriteLine($"Sent {r} bytes", "client");
                //чтобы на линуксе не кидало argument null exception
                IPEndPoint serverEp = new IPEndPoint(0, 8000);
                try
                {
                    var response = new OwnerPackage(scope.client.Receive(ref serverEp));
                    Console.WriteLine($"Answer from {serverEp}");
                    var roomInfo = response.Parse();

                    if (!rooms.ContainsKey(serverEp) 
                        && response.PackageType == OwnerPackageType.BroadcastScanningAnswer)
                    {
                        Console.WriteLine($"{serverEp} answered to your prayer.");
                        rooms.Add(serverEp, (OPRoomInfo)roomInfo);
                    }
                } catch(Exception ex) //прошел таймаут
                {
                    Console.WriteLine($"No response ({ex.ToString()})", "client");
                    break;
                }

            }
            scope.client.EnableBroadcast = false;
            scope.client.Client.ReceiveTimeout = -1;
            return rooms;
        }

        public bool TryConnect(IPEndPoint serverEP)
        {
            if (scope.isListenerRunning) throw new Exception("Listener is running, can't receive RoomEnterRequestAcception");
            //если через 300 мс не ответят, то сервер не сервер
            scope.client.Client.ReceiveTimeout = 300;
            //запрашиваю вход
            var mp = new MemberPackage(
                (new MPRoomEnterRequest() { Username = scope.username }).GetBytes()
                )
            { PackageType = MemberPackageType.RoomEnterRequest }.GetBytes();

            int r = scope.client.Send(mp, mp.Length, serverEP);
            Console.WriteLine("Sent " + r + " bts of enter request");
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

        public void Acknowlege(ulong checkpoint)
        {
            Task.Run(() =>
            {
                byte[] package = new MemberPackage(new MPActionsAcknowledgment()
                {
                    Checkpoint = checkpoint,
                    AverageFrameExecutionTime = 0
                }.GetBytes())
                {
                    PackageType = MemberPackageType.ActionsAcknowledgment
                }.GetBytes();
                scope.client.Send(package, package.Length);
            });
   
            if(scope.shouildWaitActionsSignal)
            {
                scope.accumulatedActionsCameSignal.WaitOne();
                scope.shouildWaitActionsSignal = true;
                scope.lastRecievedActionsCheckpoint = checkpoint;
            }

        }

        static void ListenerThreadFunction(object _scope)
        {
            SharedThreadScope scope = (SharedThreadScope)_scope;

            while(true)
            {
                IPEndPoint sender = new IPEndPoint(0, 8000); ;
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
                                //десереализую действия
                                scope.receivedActions = (pData as OPAccumulatedActions).Actions;
                                scope.shouildWaitActionsSignal = false;
                                scope.accumulatedActionsCameSignal.Set();
                                //Debug.WriteLine($"Got actions chunk with {(pData as OPAccumulatedActions).Checkpoint}", "client " + scope.username);
                                break;
                            case OwnerPackageType.SynchronizationDone:
                                if((pData as OPSynchronizationDone).Checkpoint == scope.lastRecievedActionsCheckpoint)
                                {
                                    scope.accumulatedActionsCameSignal.Set();
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
