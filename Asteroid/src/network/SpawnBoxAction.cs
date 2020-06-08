using Box2DX.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asteroid.src.network
{
    [Serializable]
    class SpawnBoxAction : RemoteActionBase
    {
        //public override RemoteActionType ActionType
        //{
        //    get
        //    {
        //        return RemoteActionType.SpawnBox;
        //    }
        //}
        public AVec2 Position { get; set; }

    }
}
