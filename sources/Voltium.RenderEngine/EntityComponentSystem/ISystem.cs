using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.RenderEngine.EntityComponentSystem
{
    interface ISystem
    {
        void Update(float delta);
    }
}
