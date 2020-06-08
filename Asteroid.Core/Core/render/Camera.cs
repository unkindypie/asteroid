using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Asteroid.Core.render
{
    static class Camera
    {
        public static Matrix ViewMatrix { get; set; }
        public static Matrix ProjectionMatrix { get; set; }
        public static Matrix DefaultWorldMatrix { get; set; }
        public static BasicEffect CurrentEffect { get; set; }
    }
}
