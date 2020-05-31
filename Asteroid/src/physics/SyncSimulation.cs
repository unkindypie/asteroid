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
        static World box2dWorld;
        static public readonly float Delta = 0.01666f;
        static readonly int velocityIterations = 8;
        static readonly int positionIterations = 3;
        static bool isInitialized = false;

        public static void Initialize()
        {
            if (isInitialized) throw new Exception("SyncSimulation is already initialized!");

            AABB worldAABB = new AABB();
            worldAABB.LowerBound.Set(-100.0f, -100.0f);
            worldAABB.UpperBound.Set(100.0f, 100.0f);
            box2dWorld = new World(worldAABB, new Vec2(0, -1), true);

            isInitialized = true;
        }

        public static void Step()
        {
            box2dWorld.Step(Delta, velocityIterations, positionIterations);
        }

        static public void AddBody(IBody body)
        {
            body.Initialize(box2dWorld.CreateBody(body.BodyDef));
        }

        static public void DestroyBody(IBody body)
        {
            box2dWorld.DestroyBody(body.RealBody);
        }

    }
}
