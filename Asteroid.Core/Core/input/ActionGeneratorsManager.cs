using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Asteroid.Core.Network;
using Asteroid.Core.Worlds;
using System.Diagnostics;

namespace Asteroid.Core.Input
{
    //каждый обработчик возвращает данные действия
    delegate RemoteActionBase MouseClickListener(MouseState mouseState);
    delegate RemoteActionBase KeyboardEventListener(KeyboardState keyboardState, MouseState mouseState);
    //Класс занимается вызовом обработчиков ивентов ввода, которые будут
    // генерирвоать IRemoteAction'ы для дальнейшей отправки владельцу комнаты
    class ActionGeneratorsManager
    {
        TimeSpan lastClickUpd = new TimeSpan(0);
        BaseWorld world;

        public ActionGeneratorsManager(byte checkpointInterval, BaseWorld world)
        {
            this.world = world;
        }

        public event MouseClickListener OnMousePress;
        public event MouseClickListener OnMouseRelease;
        public event KeyboardEventListener OnKeyPress;


        public void Update(GameTime gameTime, byte frame, ulong checkpoint)
        {
            if ((Mouse.GetState().LeftButton == ButtonState.Pressed ||
                Mouse.GetState().RightButton == ButtonState.Pressed) &&
               (gameTime.TotalGameTime - lastClickUpd) > new TimeSpan(0, 0, 0, 0, 200))
            {
                if(OnMousePress != null)
                {
                    foreach (MouseClickListener listener in OnMousePress.GetInvocationList())
                    {
                        var result = listener(Mouse.GetState());
                        result.Frame = frame;
                        result.Checkpoint = checkpoint;
                        if (result != null)
                        {

                            world.NetClient.SendAction(result);
                        }
                    }
                }

                if(OnMouseRelease != null)
                {
                    foreach (MouseClickListener listener in OnMouseRelease.GetInvocationList())
                    {
                        var result = listener(Mouse.GetState());
                        result.Frame = frame;
                        result.Checkpoint = checkpoint;
                        if (result != null)
                        {

                            world.NetClient.SendAction(result);
                        }
                    }
                }
              
                lastClickUpd = gameTime.TotalGameTime;
            }
        }
    }
}
