using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

using Asteroid.src.physics.bodies;


namespace Asteroid.src.render
{
    interface IRenderer
    {
        void Render(IBody body, SpriteBatch spriteBatch);
    }
}
