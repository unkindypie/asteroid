using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Asteroid.src.utils;
using Asteroid.src.worlds;

namespace Asteroid.src.network
{
    delegate RemoteActionBase ParserDelegete(byte[] data);
    static class Parser
    {
        static BinaryFormatter formatter = new BinaryFormatter();
        static byte[] serBuf = new byte[1472];

        public static RemoteActionBase ParseAction(byte[] buff)
        {
            using(MemoryStream ms = new MemoryStream(buff))
            {
                return (RemoteActionBase)formatter.Deserialize(ms);
            }
        }

        public static byte[] SerealizeAction(RemoteActionBase action)
        {
            using (MemoryStream ms = new MemoryStream(serBuf))
            {
                formatter.Serialize(ms, action);
                return ms.GetBuffer().Take((int)ms.Position).ToArray();
            }
        }

        public static byte[] SerealizeAccumulatedActions(
            SynchronizedList<SynchronizedList<RemoteActionBase>> accumulatedActions)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                //foreach(var frame in accumulatedActions)
                //{
                //    foreach(var action in frame)
                //    {
                //        formatter.Serialize(ms, action);
                //    } 
                //}
                formatter.Serialize(ms, accumulatedActions.ToArray());
                //return serBuf.Take((int)ms.Position).ToArray();
                return ms.GetBuffer().Take((int)ms.Position).ToArray();
            }
        }

        public static SynchronizedList<SynchronizedList<RemoteActionBase>> DeserealizeAccumulatedActions(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                return (SynchronizedList<SynchronizedList<RemoteActionBase>>)formatter.Deserialize(ms);
            }
        }
    }
}
