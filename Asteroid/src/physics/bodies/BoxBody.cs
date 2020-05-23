using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Box2DX.Dynamics;


namespace Asteroid.src.physics.bodies
{
    class BoxBody : IBody
    {
        BodyDef bodyDef;
        Body body;

        public Body Body { get { return body; } }
        public BodyDef BodyDef { get { return bodyDef; } }

        public BoxBody()
        {

        }

        public void Initialize(Body body)
        {
            this.body = body;

        }
    }
}
