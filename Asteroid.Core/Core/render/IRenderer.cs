using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

using Asteroid.Core.Physics.Bodies;


namespace Asteroid.Core.Render
{
    interface IRenderer
    {
        void Render(IBody body, GraphicsDevice graphicsDevice);
    }
}
