using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Box2DX.Collision;
using Box2DX.Common;
using Box2DX.Dynamics;

namespace Asteroid.Core.physics.bodies
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
                //средняя позиция между векторами
                Position = new Vec2((from.X + to.X) /2, (from.Y + to.Y) / 2),
                //угол между векторами
                Angle = (float)System.Math.Atan2(to.Y - from.Y, to.X - from.X),
                //расстояние между точками загоняю в UserData, чтоб потом использовать в Initialize
                UserData = (float)System.Math.Sqrt(
                (from.X - to.X) * (from.X - to.X)
                + (from.Y - to.Y) * (from.Y - to.Y))
            };
        }

        public void Initialize(Body body)
        {
            this.body = body;
            float len = (float)bodyDef.UserData;
            bodyDef.UserData = null;
            PolygonDef shapeDef = new PolygonDef();
            //указываю вершины относительно центра тела
            shapeDef.VertexCount = 2;
            shapeDef.Vertices[0] = new Vec2(-len / 2f, 0);
            shapeDef.Vertices[1] = new Vec2(len / 2f, 0);
            
            body.CreateShape(shapeDef);
        }
    }
}
