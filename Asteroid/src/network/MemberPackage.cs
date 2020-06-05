using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asteroid.src.network
{
    enum MemberPackageType
    {
        RoomEnterRequest = 1545611,
        ActionsAcknowledgment = 412577,
        RemoteAction = 6698898,
        BroadcastScanning = 445454,
    }
    //типы сообщений от учасника комнаты к серверу
    class RoomEnterRequest
    {
        //ASCII
        public string Username { get; set; } = "";
        public byte[] GetBytes()
        {
            return BitConverter
                .GetBytes(Username.Length)
                .Concat(Encoding.ASCII.GetBytes(Username)).ToArray();
        }
    }
    class ActionsAcknowledgment
    {
        public ulong Checkpoint { get; set; }
        public ushort AverageFrameExecutionTime { get; set; }
        
    }

    //и еще есть IRemoteAction

    //класс пакета учасника
    class MemberPackage
    {
        public MemberPackageType PackageType { get; set; }
        byte[] Data { get; set; }
        public MemberPackage(byte[] data)
        {
            Data = data;
        }

        public object Parse()
        {
            PackageType = (MemberPackageType)BitConverter.ToInt32(Data, 0);

            switch (PackageType)
            {
                case MemberPackageType.BroadcastScanning:
                    return null;
                case MemberPackageType.RoomEnterRequest:
                    int nameLen = BitConverter.ToInt32(Data, 4);
                    return new RoomEnterRequest() {
                        Username = Encoding.Unicode.GetString(Data, 8, nameLen),
                    };
                case MemberPackageType.ActionsAcknowledgment:
                    return new ActionsAcknowledgment() {
                        Checkpoint = BitConverter.ToUInt64(Data, 4),
                        AverageFrameExecutionTime = BitConverter.ToUInt16(Data, 12)
                    };
                case MemberPackageType.RemoteAction:
                    return Parser.ParseAction(Data.Skip(4).ToArray());
                default:
                    return null;
            }
        }

        public byte[] GetBytes()
        {
            byte[] result = BitConverter.GetBytes((int)PackageType);
            switch (PackageType)
            {
                case MemberPackageType.RoomEnterRequest:
                    return result.Concat(Data).ToArray();
                case MemberPackageType.BroadcastScanning:
                default:
                    return result;
            }
        }

        static public MemberPackage BroadcastScanningPackage => 
            new MemberPackage(new byte[0]) { PackageType = MemberPackageType.BroadcastScanning };
    }
}