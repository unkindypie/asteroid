using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asteroid.src.network
{
    //enum RemoteActionType
    //{
    //    SpawnBox
    //}
    [Serializable]
    abstract class RemoteActionBase
    {
        //public abstract RemoteActionType ActionType { get; }
        public byte Frame { get; set; }
        public ulong Checkpoint { get; set; }
    }
}
