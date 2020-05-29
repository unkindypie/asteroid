using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Asteroid.src.network;

namespace Asteroid.src
{
    class CurrentPlayer
    {
        List<IRemoteAction>[] ownActions;
        ushort id;

        public CurrentPlayer(ushort checkpointInterval)
        {
            ownActions = new List<IRemoteAction>[checkpointInterval];
        }
    }
}
