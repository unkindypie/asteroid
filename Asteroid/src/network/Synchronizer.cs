using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Asteroid.src.worlds;
using Asteroid.src.physics;

namespace Asteroid.src.network
{

    class Synchronizer
    {
        BaseWorld world;
        long checkpointInterval = 5;
        long curFrame = 0;

        object queueAddingLockObj = 42; //объект синхронизации для критической секции
        List<RemoteActionData> actionsQueue = new List<RemoteActionData>();
       


        public Synchronizer(BaseWorld world)
        {
            this.world = world;
        }
        public void Update(TimeSpan elapsed)
        {
            if(curFrame % checkpointInterval == 0)
            {
                lock(queueAddingLockObj)
                {
                    foreach (RemoteActionData remoteActionData in actionsQueue)
                    {
                        Parser.Handle(remoteActionData, world);
                    }
                    actionsQueue.Clear();
                }

            }
            world.Update(elapsed);
            SyncSimulation.Step();
            curFrame++;
        }
    }
}
