using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

using Asteroid.src.worlds;
using Asteroid.src.physics;
using Asteroid.src.input;

namespace Asteroid.src.network
{

    class Synchronizer
    {
        BaseWorld world;
        ActionGeneratorsManager inputManager;
        byte checkpointInterval = 5;
        byte curFrame = 0;
        ulong lastCheckpoint;

        object pendingToExecutionStacksCoppyingLock = 42; //объект синхронизации для критической секции
        List<IRemoteAction>[] executionStacks;
       
        public Synchronizer(BaseWorld world)
        {
            this.world = world;
            //на каждый кадр по своему стеку
            executionStacks = new List<IRemoteAction>[checkpointInterval];
            for(byte i = 0; i < checkpointInterval; i++)
            {
                executionStacks[i] = new List<IRemoteAction>();
            }

            inputManager = new ActionGeneratorsManager(checkpointInterval);
            world.Initialize(inputManager);
        }

        public void Update(GameTime elapsed)
        {
            if(curFrame % checkpointInterval == 0)
            {
                curFrame = 0;
                lastCheckpoint++;
                //сливаю в executionStacks инпуты с буфера класса, работающего с 
                // сетью и протоколом
                executionStacks = inputManager.GeneratedActions;
                inputManager.ClearActions();
            }
            //тут генерируется инпут
            inputManager.Update(elapsed, curFrame);
            //применяю инпут, который должен быть применен в этом кадре
            foreach (IRemoteAction actionData in executionStacks[curFrame])
            {
                world.ExecuteAction(actionData);
            }
            executionStacks[curFrame].Clear();

            world.Update(elapsed);
            SyncSimulation.Step();
            curFrame++;
        }
    }
}