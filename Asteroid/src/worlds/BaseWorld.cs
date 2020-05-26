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
        List<IEntity> entities = new List<IEntity>();

        public virtual void Update(TimeSpan elapsed)
        {
            foreach(IEntity entity in entities)
            {
                entity.Update(elapsed);
            }
        }
        public virtual void Render(TimeSpan elapsed, GraphicsDevice graphicsDevice)
        {
            foreach(IEntity entity in entities)
            {
                entity.Render(elapsed, graphicsDevice);
            }
        }

        public virtual void AddEntity(IEntity entity)
        {
            entities.Add(entity);
            SyncSimulation.AddBody(entity.Body);
        }

        public virtual void RemoveEntity(IEntity entity)
        {
            entities.Remove(entity);
            SyncSimulation.DestroyBody(entity.Body);
        }
    }
}
