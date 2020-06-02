using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Asteroid.src.network
{
    enum NetServiceType
    {
        RoomOwner, RoomMember
    }
    class NetService
    {
        NetServiceType serviceType;

        readonly ushort inviteReceivingPort = 2557;
        object syncObj = 42;
        bool didActionsRecieved = false;
        IPEndPoint roomOwner;
        List<IPEndPoint> members;
        Thread senderThread;
        Thread recieverThread;
        UdpClient sendClient;
        Socket recieveSocket;
        //для владельца - тут лежат свои действия и действия, отправленные пользователями
        //для учасника тут будут лежать отпарсенные данные от владельца
        List<RemoteActionBase>[] confirmedActions;
        //временное хранилище
        List<RemoteActionBase>[] pendingActions;
        byte checkpointInterval;

        public NetService(NetServiceType serviceType, byte checkpointInterval)
        {
            this.serviceType = serviceType;
            this.checkpointInterval = checkpointInterval;

            confirmedActions = new List<RemoteActionBase>[checkpointInterval];
            pendingActions = new List<RemoteActionBase>[checkpointInterval];
            for (byte i = 0; i < checkpointInterval; i++)
            {
                confirmedActions[i] = new List<RemoteActionBase>();
                pendingActions[i] = new List<RemoteActionBase>();
            }

            if (serviceType == NetServiceType.RoomMember)
            {
               
            }
            else if(serviceType == NetServiceType.RoomOwner)
            {
                members = new List<IPEndPoint>();
            }

            sendClient = new UdpClient();
        }
        //если это владелец, то в другом потоке в критической секции(не в этом методе)
        // будет ожидание, пока все подтвердят получение confirmedActions
        //если это учасник, то в другом потоке в крит секции будет ожидание получения
        //confirmedActions'ов владельца
        public List<RemoteActionBase>[] RecieveActions()
        {
            if (didActionsRecieved) return confirmedActions;

            lock(syncObj)
            {
                return confirmedActions;
            }
        }
        //отправляет IRemoteAction владельцу либо сохраняет, если это владелец
        public void RegisterAction(RemoteActionBase action, byte frame)
        {
            if (serviceType == NetServiceType.RoomMember)
            {

            }
            else if (serviceType == NetServiceType.RoomOwner)
            {
                pendingActions[frame].Add(action);
            }
        }
        public void SendActionsToMembers()
        {
            foreach(IPEndPoint member in members)
            {
                //сериализация и отправка actions
                //sendClient.Send()
            }
        }
        
    }
}
