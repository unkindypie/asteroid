using Asteroid.src.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asteroid.src.network
{
    enum OwnerPackageType
    {
        RoomEnterRequestAcception = 981556,
        RoomEnterRequestRejection = 998777,
        BroadcastScanningAnswer = 45954,
        AccumulatedRemoteActions = 973642,
        SynchronizationDone = 787271,
    }

    class OPRoomInfo
    {
        /// <summary>
        /// OwnersName - ASCII string
        /// </summary>
        public string OwnersName { get; set; }
        public byte UserCount { get; set; }
        public byte MaxUserCount { get; set; }

        public static OPRoomInfo FromBytes(byte[] data)
        {
            var r = new OPRoomInfo();
            r.OwnersName = Encoding.ASCII.GetString(data, 4, BitConverter.ToInt32(data, 0));
            r.MaxUserCount = data[r.OwnersName.Length + 4];
            r.MaxUserCount = data[r.OwnersName.Length + 5];
            return r;
        }
        public byte[] GetBytes()
        {
            if (OwnersName.Length > 20)
            {
                OwnersName = OwnersName.Substring(0, 20);
            }
            return BitConverter
                .GetBytes(OwnersName.Length)
                .Concat(Encoding.ASCII.GetBytes(OwnersName))
                .Concat(new byte[] { UserCount, MaxUserCount }).ToArray();
        }
    }

    class OPSynchronizationDone
    {
        public ulong Checkpoint { get; set; }
        public byte[] GetBytes()
        {
            return BitConverter.GetBytes(Checkpoint)
                .ToArray();
        }
        public static OPAccumulatedActions Parse(byte[] data)
        {
            return new OPAccumulatedActions()
            {
                Checkpoint = BitConverter.ToUInt64(data, 0),
            };
        }
    }

    class OPAccumulatedActions
    {
        public ulong Checkpoint { get; set; }
        public SynchronizedList<SynchronizedList<RemoteActionBase>> Actions { get; set; } = null;
        public byte[] GetBytes()
        {
            return BitConverter.GetBytes(Checkpoint)
                .Concat(Parser.SerealizeAccumulatedActions(Actions))
                .ToArray();
        }
        public static OPAccumulatedActions Parse(byte[] data)
        {
            return new OPAccumulatedActions()
            {
                Checkpoint = BitConverter.ToUInt64(data, 0),
                Actions = Parser.DeserealizeAccumulatedActions(data.Skip(8).ToArray())
            };
        }
    }
    class OwnerPackage
    {
        public OwnerPackageType PackageType { get; set; }
        public byte[] Data { get; set; }

        public OwnerPackage(byte[] data)
        {
            Data = data;
        }

        public object Parse()
        {
            PackageType = (OwnerPackageType)BitConverter.ToInt32(Data, 0);
            switch (PackageType)
            {
                case OwnerPackageType.AccumulatedRemoteActions:
                    return OPAccumulatedActions.Parse(Data.Skip(4).ToArray());
                case OwnerPackageType.RoomEnterRequestAcception:
                    return this;
                case OwnerPackageType.RoomEnterRequestRejection:
                    return this;
                case OwnerPackageType.SynchronizationDone:
                    return OPSynchronizationDone.Parse(Data.Skip(4).ToArray());
                case OwnerPackageType.BroadcastScanningAnswer:
                    return OPRoomInfo.FromBytes(Data.Skip(4).ToArray());
                default:
                    return null;
            }

        }

        public byte[] GetBytes()
        {
            byte[] result = BitConverter.GetBytes((int)(PackageType));
            switch (PackageType)
            {
                case OwnerPackageType.RoomEnterRequestAcception:
                    return result;
                case OwnerPackageType.RoomEnterRequestRejection:
                    return result;
                case OwnerPackageType.BroadcastScanningAnswer:
                    return result.Concat(Data).ToArray();
                case OwnerPackageType.AccumulatedRemoteActions:
                    return result.Concat(Data).ToArray();
                case OwnerPackageType.SynchronizationDone:
                    return result.Concat(Data).ToArray();
                default:
                    return null;
            }
        }
    }
}
