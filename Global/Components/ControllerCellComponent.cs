#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboSim.Global.Components
{
    public partial class ControllerCellComponent : CellComponent
    {
        
        public virtual void StartSimulation()
        {
            throw new System.NotImplementedException("Не назначено действие");
        }

        public virtual void StopSimulation()
        {
            throw new System.NotImplementedException("Не назначено действие");
        }

        public virtual void Live()
        {
            throw new System.NotImplementedException("Не назначено действие");
        }

        public override void Initialize()
        {
            throw new NotImplementedException();
        }
    }
}
#endif