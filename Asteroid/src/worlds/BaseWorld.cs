using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Asteroid.src.entities;
using Asteroid.src.physics;

namespace Asteroid.src.worlds
{
    abstract class BaseWorld
    {
        //надмножество всех подмножеств по интерфейсам сущностей
        protected List<IEntity> entities = new List<IEntity>();
        //помножества сущностей
        protected List<IRenderable> renderables = new List<IRenderable>();
        protected List<IUpdateable> updateables = new List<IUpdateable>();

        public virtual void Update(TimeSpan elapsed)
        {
            foreach(IUpdateable entity in updateables)
            {
                entity.Update(elapsed);
            }
        }
        public virtual void Render(TimeSpan elapsed, GraphicsDevice graphicsDevice)
        {
            foreach(IRenderable entity in renderables)
            {
                entity.Render(elapsed, graphicsDevice);
            }
        }

        public virtual void AddEntity(IEntity entity)
        {
            entities.Add(entity);
            if(entity is IRenderable)
            {
                renderables.Add((IRenderable)entity);
            }
            if (entity is IUpdateable)
            {
                updateables.Add((IUpdateable)entity);
            }
            SyncSimulation.AddBody(entity.Body);
        }

        public virtual void RemoveEntity(IEntity entity)
        {
            if (entity is IRenderable)
            {
                renderables.Remove((IRenderable)entity);
            }
            if (entity is IUpdateable)
            {
                updateables.Remove((IUpdateable)entity);
            }
            entities.Remove(entity);
            SyncSimulation.DestroyBody(entity.Body);
        }
    }
}
