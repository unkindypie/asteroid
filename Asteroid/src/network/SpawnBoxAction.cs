using Box2DX.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asteroid.src.network
{
    class SpawnBoxAction : IRemoteAction
    {
        public RemoteActionType ActionType
        {
            get
            {
                return RemoteActionType.SpawnBox;
            }
        }
        public Vec2 Position { get; set; }
        public byte Frame { get; set; }
    }
}
