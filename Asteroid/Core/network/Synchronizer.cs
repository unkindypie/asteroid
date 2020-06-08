using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

using Asteroid.Core.worlds;
using Asteroid.Core.physics;
using Asteroid.Core.input;
using System.Threading;
using System.Diagnostics;

namespace Asteroid.Core.network
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
        ulong checkpoint;

        object pendingToExecutionStacksCoppyingLock = 42; //объект синхронизации для критической секции
        List<RemoteActionBase>[] executionStacks;

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

            inputManager = new ActionGeneratorsManager(checkpointInterval, world);
            world.Initialize(inputManager);
            //if(sessionType == SynchronizerType.WorldOwner)
            //{
            //    server = new NetGameServer(checkpointInterval);
            //    server.Listen();
            //    canUpdate = true;
            //}
            //Task.Run(() =>
            //{
            //    Thread.Sleep(100);
            //    Debug.WriteLine("Started scanning...", "Synchronizer");
            //    var rooms = world.NetClient.ScanNetwork();
            //    foreach (var ipAndRoom in rooms)
            //    {
            //        Debug.WriteLine(ipAndRoom.Key.ToString() + " "
            //            + ipAndRoom.Value.OwnersName, "Synchronizer");
            //        if(world.NetClient.TryConnect(ipAndRoom.Key, "usual-player-name"))
            //        {
            //            Debug.WriteLine("Connected to " + ipAndRoom.Key, "Synchronizer");
            //        }
            //    }

            //});

            Task.Run(() =>
            {
                Thread.Sleep(100);
                Console.WriteLine("Started scanning...", "Synchronizer");
                var rooms = world.NetClient.ScanNetwork();
                foreach (var ipAndRoom in rooms)
                {
                    //Debug.WriteLine(ipAndRoom.Key.ToString() + " "
                    //    + ipAndRoom.Value.OwnersName, "Synchronizer");

                    Console.WriteLine(ipAndRoom.Key.ToString() + " "
                        + ipAndRoom.Value.OwnersName);

                    if (world.NetClient.TryConnect(ipAndRoom.Key))
                    {
                        Console.WriteLine("Connected to " + ipAndRoom.Key, "Synchronizer");
                    }
                    else Console.WriteLine("Server says no", "Synchronizer");
                    return;
                }
                Console.WriteLine("Running own server", "Synchronizer");
                server = new NetGameServer(checkpointInterval);
                server.Listen();;

                Thread.Sleep(100);
                Console.WriteLine("Started scanning...", "Synchronizer");
                rooms = world.NetClient.ScanNetwork();
                foreach (var ipAndRoom in rooms)
                {
                    Console.WriteLine(ipAndRoom.Key.ToString() + " "
                        + ipAndRoom.Value.OwnersName, "Synchronizer");
                    if (world.NetClient.TryConnect(ipAndRoom.Key))
                    {
                        Console.WriteLine("Connected to " + ipAndRoom.Key, "Synchronizer");
                    }
                    else Console.WriteLine("Server says no", "Synchronizer");
                }
            });

        }

        public void Update(GameTime elapsed)
        {
            if (!world.NetClient.IsGameStarted) return;

            if(curFrame == checkpointInterval - 1)
            {
                world.NetClient.Acknowlege(checkpoint);
                //сливаю в executionStacks инпуты с буфера класса, работающего с 
                // сетью и протоколом
                for(int i = 0; i < checkpointInterval; i++)
                {
                    //executionStacks[i].Clear();
                    foreach(RemoteActionBase action in world.NetClient.ReceivedActions[i])
                    {
                        executionStacks[i].Add(action);
                    }
                }

                curFrame = 0;
                checkpoint++;
            }
            //тут генерируется инпут
            inputManager.Update(elapsed, curFrame, checkpoint);
            //применяю инпут, который должен быть применен в этом кадре
            foreach (RemoteActionBase actionData in executionStacks[curFrame])
            {
                world.ExecuteAction(actionData);
                //Task.Run(() => Debug.WriteLine("Executing action on frame " + curFrame +
                //    " on CP " + checkpoint + ". ActionData: f: " + actionData.Frame + " cp: " + actionData.Checkpoint));
            }
            executionStacks[curFrame].Clear();

            world.Update(elapsed);
            SyncSimulation.Step();
            curFrame++;
        }
    }
}