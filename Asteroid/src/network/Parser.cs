using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Asteroid.src.worlds;

namespace Asteroid.src.network
{
    static class Parser
    {
        static Dictionary<RemoteActionType, Action<IRemoteAction, BaseWorld>> handlers 
            = new Dictionary<RemoteActionType, Action<IRemoteAction, BaseWorld>>();


        // TODO:
        //  нужен ли пакетам номер чекпоинта, для которого они отправлены?
        //  что делать с очень старыми? а очень новыми?
        public static IRemoteAction Parse(byte[] buff)
        {
            //var result = new RemoteActionType(BitConverter.ToInt32(buff, 0))
            return null;
        }

        public static void RunHandle(IRemoteAction actionData, BaseWorld world)
        {
            handlers[actionData.ActionType](actionData, world);
        }

        public static void AddHandler(RemoteActionType actionType, Action<IRemoteAction, BaseWorld> action)
        {
            handlers.Add(actionType, action);
        }
    }
}
