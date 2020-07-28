using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltium.RenderEngine.EntityComponentSystem;

namespace Voltium.CubeGame
{
    public struct Renderable
    {

    }

    public sealed class WorldRenderer
    {
        private EntityContainer _worldEntities;



        public void Render()
        {
            foreach (var entity in _worldEntities.ViewOf<Renderable>())
            {

            }
        }
    }
}
