using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;

namespace Asteroid.src.network
{

    class NetGameClient
    {
        SharedThreadScope scope;

        class SharedThreadScope
        {
            public UdpClient client;
            
        }

        public NetGameClient()
        {
            scope = new SharedThreadScope() {
                client = new UdpClient()
            };
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
            for (int i = 0; i < 5; i++)
            {
                scope.client.Send(broadcastPackage, broadcastPackage.Length, broadcastEp);
                IPEndPoint serverEp = null;
                try
                {
                    var response = scope.client.Receive(ref serverEp);

                    if (!rooms.ContainsKey(serverEp))
                    {
                        rooms.Add(serverEp, RoomInfo.FromBytes(response));
                    }
                } catch(SocketException) //прошел таймаут
                {
                    break;
                }

            }
            scope.client.Client.ReceiveTimeout = -1;
            return rooms;
        }

        public void SendAction(RemoteActionBase action)
        {

        }
    }
}
