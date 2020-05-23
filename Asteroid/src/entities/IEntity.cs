using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Asteroid.src.physics.bodies;
using Asteroid.src.render;

namespace Asteroid.src.entities
{
    interface IEntity
    {
        IBody Body { get; }
        IRenderer Renderer { get; }
    }
}
