#if TOOLS
using Godot;
using System.Text.Json.Nodes;
using RoboSim.Global;

namespace Robosim.RobotObj
{
    [Tool]
    public partial class RobotObject : CellObject
    {
        private RobotControllerComponent _controller;
        public override void _Ready()
        {
            base._Ready();
            _controller = GetComponent<RobotControllerComponent>();

        }
        public override Node GetEditorSelectionTarget()
        {
            if (GlobalState.Instance.JogMode && IsInstanceValid(_controller.EndEffectorPoint))
                return _controller.EndEffectorPoint;

            return this;
        }

    }
}
#endif