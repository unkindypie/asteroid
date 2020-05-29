using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asteroid.src.entities
{
    interface INeedsUpdate
    {
        void Update(GameTime elapsed);
    }
}
