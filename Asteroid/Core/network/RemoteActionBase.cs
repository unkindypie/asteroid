using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asteroid.Core.network
{
    [Serializable]
    struct AVec2
    {
        public float X { get; set; }
        public float Y { get; set; }
        public AVec2(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    [Serializable]
    abstract class RemoteActionBase
    {
        //public abstract RemoteActionType ActionType { get; }
        public byte Frame { get; set; }
        public ulong Checkpoint { get; set; }
    }
}
