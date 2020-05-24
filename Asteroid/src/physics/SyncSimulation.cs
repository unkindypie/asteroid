﻿using System;
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
        static readonly float delta = 0.001666f;
        static readonly int velocityIterations = 10;
        static readonly int positionIterations = 8;
        static bool isInitialized = false;

        public static void Initialize()
        {
            if (isInitialized) throw new Exception("SyncSimulation is already initialized!");

            AABB worldAABB = new AABB();
            worldAABB.LowerBound.Set(-100.0f, -100.0f);
            worldAABB.UpperBound.Set(100.0f, 100.0f);
            box2dWorld = new World(worldAABB, new Vec2(0, -10), true);

            isInitialized = true;
        }

        public static void Step()
        {
            box2dWorld.Step(delta, velocityIterations, positionIterations);
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
