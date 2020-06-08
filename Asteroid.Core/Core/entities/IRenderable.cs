using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Asteroid.Core.Render;
using Microsoft.Xna.Framework;

namespace Asteroid.Core.entities
{
    interface IRenderable
    {
        IRenderer Renderer { get; }
        void Render(GameTime elapsed, GraphicsDevice graphicsDevice);
    }
}
