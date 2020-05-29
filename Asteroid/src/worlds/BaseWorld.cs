using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

using Asteroid.src.entities;
using Asteroid.src.physics;
using Asteroid.src.input;
using Asteroid.src.network;


namespace Asteroid.src.worlds
{

    // Базовый класс для всех игровых миров(уровней.)
    // инкапуслирует в себе обработку инпута, вызов рендера и обновления сущностей
    // должен быть передан в Synchronizer

    //todo: как-то автоматизировать добавление новых интерфейсов сущностей и их
    // коллекции
    abstract class BaseWorld
    {
        //надмножество всех подмножеств по интерфейсам сущностей
        protected List<IEntity> entities = new List<IEntity>();
        //подмножества сущностей
        protected List<IRenderable> renderables = new List<IRenderable>();
        protected List<IUpdateable> updateables = new List<IUpdateable>();
        //хранит обработчики ввода пользователя, которые генерируют IRemoteAction'ы
        protected ActionGeneratorsManager inputManager;
        //хранит функции, которые выполняют ивенты из IRemoteAction'ов
        protected Dictionary<Type, Action<IRemoteAction>> executors
            = new Dictionary<Type, Action<IRemoteAction>>();

        // вызывается из Synchronizer
        public abstract void Initialize(ActionGeneratorsManager inputManager);

        public virtual void Update(GameTime elapsed)
        {
            foreach(IUpdateable entity in updateables)
            {
                entity.Update(elapsed);
            }
        }

        // Отображает сущности, реализующие IRenderable используя их собственный IRenderer
        public virtual void Render(GameTime elapsed, GraphicsDevice graphicsDevice)
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

        public void ExecuteAction(IRemoteAction remoteAction)
        {
            //вызываю обработчик
            executors[remoteAction.GetType()](remoteAction);
        }

        public void AddExecutor(Type actionType, Action<IRemoteAction> executor)
        {
            executors.Add(actionType, executor);
        }
    }
}
