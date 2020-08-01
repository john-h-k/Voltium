using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltium.RenderEngine.EntityComponentSystem;

namespace Voltium.CubeGame
{
    internal struct Renderable
    {

    }

    internal sealed class WorldRenderer
    {
        private EntityContainer _worldEntities = new();



        public void Render()
        {
            foreach (var entity in _worldEntities.ViewOf<Renderable>())
            {

            }
        }
    }
}
