using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

using Asteroid.Core.Physics.Bodies;
using Asteroid.Core.Render;


namespace Asteroid.Core.entities
{
    interface IEntity
    {
        IBody Body { get; }
    }
}
