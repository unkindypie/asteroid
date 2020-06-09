using Asteroid.Core.Network;
using System;
using System.Collections.Generic;
using System.Text;

namespace Asteroid.Core.Network
{
    [Serializable]
    class MovementAction
    {
        public AVec2 Direction { get; set; }
    }
}
