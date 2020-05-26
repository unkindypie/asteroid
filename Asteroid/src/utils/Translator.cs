using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Box2DX.Common;
using Microsoft.Xna.Framework;

using Asteroid.src.render;

namespace Asteroid.src.utils
{
    static class Translator
    {
        // размер Box2D метра в ВИРТУАЛЬНЫХ пикселах
        // т.е. 1 метр - 50 виртуальных пикселов
        static public float PhysScalar { get; set; } = 0.02f;

        // реальная ширина экрана / на виртуальную
        static public float ScaleX { get; set; } = 1;
        // реальная высота экрана / на виртуальную
        static public float ScaleY { get; set; } = 1;

        static public float virtualXtoWorld(float x)
        {
            return x * PhysScalar;
        }
        static public float virtualYtoWorld(float y)
        {
            return (-y) * PhysScalar;
        }

        static public float xToWorld(float x){
            return x / ScaleX * PhysScalar;
        }

        static public float xToScreen(float x) {
            return x / PhysScalar;
        }

        static public float yToWorld(float y){
            return (-y) / ScaleY * PhysScalar;
        }

        static public float yToScreen(float y){
            return (-y) / PhysScalar;
        }

        static public Vec2 ToWorld(float x, float y) {
            return new Vec2(xToWorld(x), yToWorld(y));
        }

        static public Vector2 ToScreen(Vec2 vec2) {
            return new Vector2((int)xToScreen(vec2.X), (int)yToScreen(vec2.Y));
        }

        static public Vector2 ScreenToWorldSpace(Vector2 point)
        {
            Matrix invertedMatrix = Matrix.Invert(Camera.ViewMatrix * Camera.ProjectionMatrix);
            return Vector2.Transform(new Vector2(point.X, point.Y), invertedMatrix);
        }

        static public Vec2 ToBox2dVec2(this Vector2 vec)
        {
            return new Vec2(vec.X, vec.Y);
        }
        static public Vector2 ToXNAVector2(this Vec2 vec)
        {
            return new Vector2(vec.X, vec.Y);
        }
    }
}
