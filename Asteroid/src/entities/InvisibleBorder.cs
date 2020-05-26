using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Asteroid.src.physics.bodies;

namespace Asteroid.src.entities
{
    class InvisibleBorder : IEntity
    {
        public IBody Body => throw new NotImplementedException();
    }
}
