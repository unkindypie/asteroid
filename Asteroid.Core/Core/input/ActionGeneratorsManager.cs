using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Asteroid.Core.network;
using Asteroid.Core.worlds;
using System.Diagnostics;

namespace Asteroid.Core.input
{
    //каждый обработчик возвращает данные действия
    delegate RemoteActionBase MouseClickListener(MouseState mouseState);
    //Класс занимается хранением и вызовом обработчиков ивентов ввода, которые будут
    // генерирвоать IRemoteAction'ы для дальнейшей их отправки владельцу комнаты и выполнения
    class ActionGeneratorsManager
    {
        List<MouseClickListener> mouseClickEventListeners = new List<MouseClickListener>();
        TimeSpan lastClickUpd = new TimeSpan(0);
        BaseWorld world;

        public ActionGeneratorsManager(byte checkpointInterval, BaseWorld world)
        {
            this.world = world;
        }

        public void AddMouseClickListener(MouseClickListener listener)
        {
            mouseClickEventListeners.Add(listener);
        }

        public void Update(GameTime gameTime, byte frame, ulong checkpoint)
        {
            if ((Mouse.GetState().LeftButton == ButtonState.Pressed ||
                Mouse.GetState().RightButton == ButtonState.Pressed) &&
               (gameTime.TotalGameTime - lastClickUpd) > new TimeSpan(0, 0, 0, 0, 200))
            {
                foreach(var listener in mouseClickEventListeners)
                {
                    var result = listener(Mouse.GetState());
                    result.Frame = frame;
                    result.Checkpoint = checkpoint;
                    if (result != null) {
                        
                        world.NetClient.SendAction(result);
                        //Task.Run(() => Debug.WriteLine($"Sent action being on {checkpoint}", "client-input"));
                    }
                }

                lastClickUpd = gameTime.TotalGameTime;
            }
        }
    }
}
