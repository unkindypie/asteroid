﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Box2DX.Dynamics;

namespace Asteroid.Core.physics.bodies
{
    interface IBody
    {
        BodyDef BodyDef { get; }
        Body RealBody { get; }
        void Initialize(Body body);
    }
}
