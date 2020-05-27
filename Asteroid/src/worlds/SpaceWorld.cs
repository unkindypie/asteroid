using Asteroid.src.entities;
using Asteroid.src.utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asteroid.src.worlds
{
    class SpaceWorld : BaseWorld
    {
        public SpaceWorld(Vector2 virtualSize, Vector2 realSize)
        {
            //world.AddEntity(
            //    new Box(
            //        new Vec2(0, 0),
            //        Translator.virtualXtoBox2DWorld(50),
            //        Translator.virtualXtoBox2DWorld(50)
            //        ));

            //границы
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
    }
}
