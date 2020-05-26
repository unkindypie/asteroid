using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asteroid.src.entities
{
    interface IUpdateable
    {
        void Update(TimeSpan elapsed);
    }
}
