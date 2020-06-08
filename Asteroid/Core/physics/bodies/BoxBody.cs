using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Box2DX.Dynamics;
using Box2DX.Common;
using Box2DX.Collision;


namespace Asteroid.Core.physics.bodies
{
    class BoxBody : IBody
    {
        BodyDef bodyDef;
        Body body;

        public BodyDef BodyDef => bodyDef;
        public Body RealBody => body;

        public BoxBody(Vec2 position, float width, float height)
        {
            bodyDef = new BodyDef() {
                MassData = new MassData() {
                    Mass = 1,
                    I = 1, //инерция вращения
                    Center = new Vec2(),
                },
                Position = position,
                UserData = new Vec2(width, height),
            };
        }

        public void Initialize(Body body)
        {
            this.body = body;
            PolygonDef shapeDef = new PolygonDef() {
                Friction = 0.3f,
                Density = 0.2f,
                Restitution = 1
            };
            // я положил сюда размеры, чтобы не загрязнять класс
            Vec2 size = (Vec2)bodyDef.UserData;
            // устанавливаю форму тела
            shapeDef.SetAsBox(size.X, size.Y);
            bodyDef.UserData = null;
            body.CreateShape(shapeDef);
            body.SetMassFromShapes(); // высчитывает массу
        }
    }
}
