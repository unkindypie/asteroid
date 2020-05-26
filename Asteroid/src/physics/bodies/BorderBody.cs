using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Box2DX.Collision;
using Box2DX.Common;
using Box2DX.Dynamics;

namespace Asteroid.src.physics.bodies
{
    class BorderBody : IBody
    {
        BodyDef bodyDef;
        Body body;

        public BodyDef BodyDef => bodyDef;
        public Body RealBody => body;

        public BorderBody(Vec2 from, Vec2 to)
        {
            bodyDef = new BodyDef()
            {
                Position = new Vec2((from.X + to.X) /2, (from.Y + to.Y) / 2)
            };
            float distance = (float)System.Math.Sqrt(
                (from.X - to.X) * (from.X - to.X)
                + (from.Y - to.Y) * (from.Y - to.Y));

        }

        public void Initialize(Body body)
        {
            
        }
    }
}
