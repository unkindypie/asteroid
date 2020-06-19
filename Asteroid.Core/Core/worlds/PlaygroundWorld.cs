using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Asteroid.Core.Entities;
using Asteroid.Core.Utils;
using Asteroid.Core.Network;
using Box2DX.Common;
using Asteroid.Core.Input;

namespace Asteroid.Core.Worlds
{
    class PlaygroundWorld : BaseWorld
    {
        Vector2 virtualSize;
        Vector2 realSize;

        public PlaygroundWorld(Vector2 virtualSize, Vector2 realSize)
        {

            netClient = new NetGameClient((new Random()).Next(100, 999).ToString());
            this.virtualSize = virtualSize;
            this.realSize = realSize;

        }

        public override void Initialize(ActionGeneratorsManager inputManager)
        {
            this.inputManager = inputManager;

            //границы
            AddBorders();
            //обработка ввода и генерация IRemoteAction'ов
            AddInputHandlers();
            //выполнение IRemoteAction'ов
            AddActionExecutors();
        }

        void AddBorders()
        {

            AddEntity(
                 new InvisibleBorder(
                     Translator.VirtualToBox2DWorld(0, virtualSize.Y),
                     Translator.VirtualToBox2DWorld(virtualSize.X, virtualSize.Y)
                     )
                 );
            AddEntity(
                new InvisibleBorder(
                    Translator.VirtualToBox2DWorld(0, 0),
                    Translator.VirtualToBox2DWorld(virtualSize.X, 0)
                    )
                );
            AddEntity(
                new InvisibleBorder(
                    Translator.VirtualToBox2DWorld(0, 0),
                    Translator.VirtualToBox2DWorld(0, virtualSize.Y)
                    )
                );
            AddEntity(
                new InvisibleBorder(
                    Translator.VirtualToBox2DWorld(virtualSize.X, 0),
                    Translator.VirtualToBox2DWorld(virtualSize.X, virtualSize.Y)
                    )
                );
        }
        void AddInputHandlers()
        {
            inputManager.OnMousePress += new MouseClickListener((MouseState state) =>
            {
                var screenMP = Mouse.GetState().Position;
                return new SpawnBoxAction()
                {
                    Position
                    = new AVec2(
                        Translator.realXtoBox2DWorld(screenMP.X),
                        Translator.realYtoBox2DWorld(screenMP.Y)
                        )
                };
            });
        }
        void AddActionExecutors()
        {
            AddExecutor(typeof(SpawnBoxAction), (RemoteActionBase _action) =>
            {
                var action = (SpawnBoxAction)_action;
                AddEntity(
                   new Box(
                       new Vec2(action.Position.X, action.Position.Y),
                       Translator.virtualXtoBox2DWorld(50),
                       Translator.virtualXtoBox2DWorld(50)
                ));
            });
        }


    }
}
