using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asteroid.src.network
{
    enum RemoteActionType
    {
        SpawnBox
    }
    interface IRemoteAction
    {
        RemoteActionType ActionType { get; }
    }
}
