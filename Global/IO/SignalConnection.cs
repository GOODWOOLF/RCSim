#if TOOLS
using Godot;
using RoboSim.Global.Components.IOComponents;

namespace RoboSim.Global.IO
{
    [Tool]
    public partial class SignalConnection : Node3D
    {

         public IOComponent Source;
        [Export] public IOChannel SourceChannel;

         public IOComponent Target;
        [Export] public IOChannel TargetChannel;

        //обработчик обновления сигнала
        public void HandleUpdateSignal(bool value)
        {
            TargetChannel.Value = value;
        }
    }
}
#endif