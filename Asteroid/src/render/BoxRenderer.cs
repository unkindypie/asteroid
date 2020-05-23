using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Asteroid.src.physics.bodies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Asteroid.src.render
{
    class BoxRenderer : IRenderer
    {
        Color Color { get; set; }

        public BoxRenderer(Color color)
        {
            Color = color;
        }

        public void Render(IBody body, GraphicsDevice graphicsDevice)
        {
            var boxBody = body as BoxBody;

            //TODO: Render...
        }
    }
}
