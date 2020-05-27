using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asteroid.src.network
{
    enum RemoteActionType
    {
        Spawn
    }
    class RemoteActionData
    {
        byte[] actionData;

        public RemoteActionType RemoteActionType { get; set; }
        public byte[] ActionData {
            get
            {
                return actionData;
            }
        }

        public RemoteActionData(RemoteActionType remoteActionType, byte[] actionData)
        {
            this.actionData = actionData;
            RemoteActionType = RemoteActionType;
        }
    }
}
