using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using Asteroid.Core.Network;
using Microsoft.Xna.Framework.Input;
using Asteroid.Core.Utils;

namespace Asteroid.Core.Input
{
    class MovementController
    {
        Rectangle controlledArea;
        bool isPressing = false;
        bool useCenter;
        Point pressStart;

        public MovementController(Rectangle controlledArea, bool useCenter)
        {
            this.controlledArea = controlledArea;
            this.useCenter = useCenter;
            if(useCenter)
            {
                pressStart = controlledArea.Center;
            }
        }
        
        public MovementAction PressHandler(MouseState mouseState)
        {
            if (!isPressing)
            {
                isPressing = true;
                if(!useCenter) pressStart = Mouse.GetState().Position;
            }

            var mousePosition = Mouse.GetState().Position;
            return new MovementAction()
            {
                Direction = new AVec2(
                    Translator.realXtoBox2DWorld(mousePosition.X - pressStart.X),
                    Translator.realYtoBox2DWorld(mousePosition.Y - pressStart.Y)
                    )
            };
        }
        public MovementAction ReleaseHandler(MouseState mouseState)
        {
            isPressing = false;

            return null;
        }
    }
}
