using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Box2DX.Common;
using Microsoft.Xna.Framework;

namespace Asteroid.src.utils
{
    class Translator
    {
        static public float Scalar { get; set; } = 0.02f;
        static public float sHeight { get; set; } = 720;

        static public float xToWorld(float x){
            return x * Scalar;
        }

        static public float xToScreen(float x) {
            return x / Scalar;
        }

        static public float yToWorld(float y){
            return (sHeight - y) * Scalar;
        }

        static public float yToScreen(float y){
            return sHeight - y / Scalar;
        }

        static public Vec2 ToWorld(float x, float y) {
            return new Vec2(xToWorld(x), yToWorld(y));
        }

        static public Vector2 ToScreen(Vec2 vec2) {
            return new Vector2((int)xToScreen(vec2.X), (int)yToScreen(vec2.Y));
        }
    }
}
