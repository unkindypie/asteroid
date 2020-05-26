using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Asteroid.src.render;
namespace Asteroid.src.entities
{
    interface IRenderable
    {
        IRenderer Renderer { get; }
        void Render(TimeSpan elapsed, GraphicsDevice graphicsDevice);
    }
}
