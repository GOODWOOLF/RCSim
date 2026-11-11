#if TOOLS
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboSim.Global.Components
{
    /// <summary>
    /// Базовый компонент
    /// </summary>
    [Tool]
    public abstract partial class CellComponent : Node3D
    {
        protected CellObject _parent;
        public void SetParent(CellObject parent) => _parent = parent;
        public abstract void Initialize();

    }

}
#endif