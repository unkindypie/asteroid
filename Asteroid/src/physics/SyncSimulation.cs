using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Box2DX.Common;
using Box2DX.Dynamics;
using Box2DX.Collision;

using Asteroid.src.physics.bodies;

namespace Asteroid.src.physics
{
    static class SyncSimulation
    {
        static World world;
        static readonly float delta = 0.001666f;
        static readonly int velocityIterations = 10;
        static readonly int positionIterations = 8;
        static bool isInitialized = false;

        public static void Initialize()
        {
            if (isInitialized) throw new Exception("SyncSimulation is already initialized!");
            var aabb = new AABB();
            world = new World(aabb, new Vec2(0, -1), true);

            isInitialized = true;
        }

        public static void Step()
        {
            world.Step(delta, velocityIterations, positionIterations);
        }

        static public void AddBody(IBody body)
        {
            body.Initialize(world.CreateBody(body.BodyDef));
        }

    }
}
