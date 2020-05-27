using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Box2DX.Common;

using Asteroid.src.physics.bodies;


namespace Asteroid.src.entities
{
    class InvisibleBorder : IEntity
    {
        BorderBody body;
        public IBody Body => body;

        public InvisibleBorder(Vec2 from, Vec2 to)
        {
            body = new BorderBody(from, to);
        }
    }
}
