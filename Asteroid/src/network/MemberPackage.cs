using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asteroid.src.network
{
    enum MemberPackageType
    {
        RoomEnterRequest,
        Acknowledgment,
        RemoteAction
    }
    class RoomEnterRequest
    {
        public string Username { get; set; }
        public ushort RecvPort { get; set; }
    }
    class Acknowledgment
    {
        public ulong Checkpoint { get; set; }
        public ushort AverageFrameExecutionTime { get; set; } 
    }
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
                case MemberPackageType.RoomEnterRequest:
                    int nameLen = BitConverter.ToInt32(Data, 4);
                    return new RoomEnterRequest() {
                        RecvPort = BitConverter.ToUInt16(Data, 8),
                        Username = Encoding.Unicode.GetString(Data, 10, nameLen),
                    };
                case MemberPackageType.Acknowledgment:
                    return new Acknowledgment() {
                        Checkpoint = BitConverter.ToUInt64(Data, 4),
                        AverageFrameExecutionTime = BitConverter.ToUInt16(Data, 12)
                    };
                case MemberPackageType.RemoteAction:
                    return Parser.Parse(Data.Skip(4).ToArray());
                default:
                    return null;
            }
        }
    }
}