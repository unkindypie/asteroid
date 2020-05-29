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
        ushort checkpointInterval = 5;
        ushort curFrame = 0;
        ulong lastCheckpoint;

        object pendingToExecutionStacksCoppyingLock = 42; //объект синхронизации для критической секции
        List<IRemoteAction>[] executionStacks;
       
        public Synchronizer(BaseWorld world)
        {
            this.world = world;
            //на каждый кадр по своему стеку
            executionStacks = new List<IRemoteAction>[checkpointInterval];
        }

        public void Update(TimeSpan elapsed)
        {
            if(curFrame % checkpointInterval == 0)
            {
                curFrame = 0;
                lastCheckpoint++;
                //сливаю в executionStacks инпуты с буфера класса, работающего с 
                // сетью и протоколом
            }

            //применяю инпут, который должен быть применен в этом кадре
            foreach (IRemoteAction actionData in executionStacks[curFrame])
            {
                ExecuteAction(actionData);
            }
            executionStacks[curFrame].Clear();

            world.Update(elapsed);
            SyncSimulation.Step();
            curFrame++;
        }

        void ExecuteAction(IRemoteAction action)
        {
            switch (action)
            {
                case SpawnBoxAction a:
                    // TODO
                    break;
                default:
                    break;
            }
        }
    }
}