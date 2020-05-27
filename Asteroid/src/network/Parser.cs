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
        static Dictionary<RemoteActionType, Action<RemoteActionData, BaseWorld>> handlers 
            = new Dictionary<RemoteActionType, Action<RemoteActionData, BaseWorld>>();


        // TODO:
        //  нужен ли пакетам номер чекпоинта, для которого они отправлены?
        //  что делать с очень старыми? а очень новыми?
        public static RemoteActionData ParseHeaders(byte[] buff)
        {
            //var result = new RemoteActionType(BitConverter.ToInt32(buff, 0))
            return null;
        }

        public static void RunHandle(RemoteActionData actionData, BaseWorld world)
        {
            handlers[actionData.RemoteActionType](actionData, world);
        }

        public static void AddHandler(RemoteActionType actionType, Action<RemoteActionData, BaseWorld> action)
        {
            handlers.Add(actionType, action);
        }
    }
}
