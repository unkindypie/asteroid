using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

using Asteroid.Core.physics.bodies;
using Asteroid.Core.render;


namespace Asteroid.Core.entities
{
    interface IEntity
    {
        IBody Body { get; }
    }
}
