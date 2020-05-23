using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Asteroid.src.render;
using Asteroid.src.physics.bodies;

namespace Asteroid.src.entities
{
    class Box : IEntity
    {
        BoxBody body;
        BoxRenderer renderer;

        public IBody Body => body;

        public IRenderer Renderer => renderer;

        public Box(float xM, float yM, float widthM, float heightM)
        {

        }
    }
}
