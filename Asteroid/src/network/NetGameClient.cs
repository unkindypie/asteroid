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
            public AutoResetEvent synchronizerWorkDoneSignla = new AutoResetEvent(false);
        }

        public NetGameClient()
        {
            scope = new SharedThreadScope() {
                client = new UdpClient()
            };
        }

        public AutoResetEvent CanContinueUpdatingPhysicsSignal => scope.synchronizerCanWorkSignal;
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
        public Dictionary<IPEndPoint, RoomInfo> ScanNetwork()
        {
            Dictionary<IPEndPoint, RoomInfo> rooms = new Dictionary<IPEndPoint, RoomInfo>();

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
                        rooms.Add(serverEp, (RoomInfo)roomInfo);
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

            scope.client.Client.ReceiveTimeout = 300;
            var mp = new MemberPackage(
                (new RoomEnterRequest() { Username = username }).GetBytes()
                ).GetBytes();
            scope.client.Send(mp, mp.Length, serverEP);
            try
            {
                var ownerPackage = new OwnerPackage(scope.client.Receive(ref serverEP));
                ownerPackage.Parse();
                scope.client.Client.ReceiveTimeout = -1;
                if (ownerPackage.PackageType == OwnerPackageType.RoomEnterRequestAcception)
                {
                    scope.client.Connect(serverEP);
                    scope.isConnected = true;
                    scope.serverEndPoint = serverEP;
                    return true;
                }
                else
                {
                    return false;
                }
            } catch(SocketException)
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
                    if (sender.Port != scope.serverEndPoint.Port
                   && sender.Address.GetHashCode() == sender.Address.GetHashCode())
                    {

                        OwnerPackage ownerPackage = new OwnerPackage(received);
                        var pData = ownerPackage.Parse();
                        switch (ownerPackage.PackageType)
                        {
                            case OwnerPackageType.AccumulatedRemoteActions:
                                scope.receivedActions = Parser.DeserealizeAccumulatedActions(ownerPackage.Data);
                                Debug.WriteLine($"Deserealized {ownerPackage.Data.Length} of actions", "net-client");
                                scope.synchronizerShouldStopFlag = false;
                                scope.synchronizerCanWorkSignal.Set();
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
