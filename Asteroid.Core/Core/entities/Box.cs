using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Box2DX.Common;

using Asteroid.Core.Render;
using Asteroid.Core.Physics.Bodies;


namespace Asteroid.Core.entities
{
    class Box : IEntity, IRenderable
    {
        BoxBody body;
        PolygonRenderer renderer;

        public IBody Body => body;

        public IRenderer Renderer => renderer;

        public Box(Vec2 position, float widthM, float heightM)
        {
            body = new BoxBody(position, widthM, heightM);
        }

        public void Render(GameTime elapsed, GraphicsDevice graphicsDevice)
        {
            if(renderer == null) renderer = new PolygonRenderer(Color.WhiteSmoke, 4, graphicsDevice);
            Renderer.Render(Body, graphicsDevice);
        }
    }
}
