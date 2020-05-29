using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Asteroid.src.input
{
    class SyncInputManager
    {
        List<Action<MouseState>> mouseClickEventListeners = new List<Action<MouseState>>();
        TimeSpan lastClickUpd = new TimeSpan(0);
        CurrentPlayer player;

        public void AddMouseClickListener(Action<MouseState> listener)
        {
            mouseClickEventListeners.Add(listener);
        }

        public void Update(GameTime gameTime)
        {
            if ((Mouse.GetState().LeftButton == ButtonState.Pressed ||
                Mouse.GetState().RightButton == ButtonState.Pressed) &&
               (gameTime.TotalGameTime - lastClickUpd) > new TimeSpan(0, 0, 0, 0, 200))
            {
                foreach(var listener in mouseClickEventListeners)
                {
                    listener(Mouse.GetState());
                }

                lastClickUpd = gameTime.TotalGameTime;
            }
        }
    }
}
