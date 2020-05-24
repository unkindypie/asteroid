using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Box2DX.Common;

using Asteroid.src.render;
using Asteroid.src.physics.bodies;


namespace Asteroid.src.entities
{
    class Box : IEntity
    {
        BoxBody body;
        PolygonRenderer renderer;

        public IBody Body => body;

        public IRenderer Renderer => renderer;

        public Box(Vec2 position, float widthM, float heightM)
        {
            body = new BoxBody(position, widthM, heightM);
        }

        public void Update(TimeSpan elapsed)
        {
           
        }

        public void Render(TimeSpan elapsed, SpriteBatch spriteBatch)
        {
            if(renderer == null) renderer = new PolygonRenderer(Color.WhiteSmoke, 4, spriteBatch.GraphicsDevice);
            Renderer.Render(Body, spriteBatch);
        }
    }
}
