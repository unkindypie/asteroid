using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

using Asteroid.src.worlds;
using Asteroid.src.physics;
using Asteroid.src.input;
using System.Threading;
using System.Diagnostics;

namespace Asteroid.src.network
{
    enum SynchronizerType
    {
        WorldOwner, Member
    }
    class Synchronizer
    {
        BaseWorld world;
        ActionGeneratorsManager inputManager;
        byte checkpointInterval = 5;
        byte curFrame = 0;
        ulong lastCheckpoint;

        object pendingToExecutionStacksCoppyingLock = 42; //объект синхронизации для критической секции
        List<RemoteActionBase>[] executionStacks;
        bool canUpdate = false;

        NetGameServer server = null;

        public Synchronizer(BaseWorld world, SynchronizerType sessionType = SynchronizerType.WorldOwner)
        {
            this.world = world;
            //на каждый кадр по своему стеку
            executionStacks = new List<RemoteActionBase>[checkpointInterval];
            for(byte i = 0; i < checkpointInterval; i++)
            {
                executionStacks[i] = new List<RemoteActionBase>();
            }

            inputManager = new ActionGeneratorsManager(checkpointInterval);
            world.Initialize(inputManager);
            if(sessionType == SynchronizerType.WorldOwner)
            {
                server = new NetGameServer(checkpointInterval);
                server.Listen();
                canUpdate = true;
                server.StartSending();
            }
            Task.Run(() =>
            {
                Thread.Sleep(100);
                Debug.WriteLine("Started scanning...", "Synchronizer-client");
                var rooms = world.NetClient.ScanNetwork();
                foreach (var ipAndRoom in rooms)
                {
                    Debug.WriteLine(ipAndRoom.Key.ToString() + " "
                        + ipAndRoom.Value.OwnersName, "Synchronizer-client");
                }
            });
        }

        public void Update(GameTime elapsed)
        {
            if (!canUpdate) return;

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
            foreach (RemoteActionBase actionData in executionStacks[curFrame])
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