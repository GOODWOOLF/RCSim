#if TOOLS
using RoboSim.Global.Components;
using Godot;
namespace RoboSim.RobotObject.RobotComponents
{
    [Tool]
    public partial class RobotMoveParametersComponent : CellComponent
    {
        RobotControllerComponent _controller;
        /// <summary>
        /// Максимальная скорость по осям
        /// </summary>
        [Export]public float[] jointsSpeedLimit { get; set; } = new float[] { 100, 200, 300, 400, 500, 600 };

        /// <summary>
        /// ускорение по осям
        /// </summary>
        [Export]public float[] jointsAccelerLimit { get; set; } = new float[] { 10, 200, 300, 400, 500, 600 };

        /// <summary>
        /// Торможение по осям
        /// </summary>
        [Export] public float[] jointsBrakeLimit { get; set; } = new float[] { 10, 200, 300, 400, 500, 600 };
        [Export] public float angularSpeedDefault = 50f;// градусы скорость вращения захвата робота вокруг целевой точки, если при линейном движении текущая и целевая координаты равны
        [Export] public float angularAccelerDefault = 50f;// градусы скорость вращения захвата робота вокруг целевой точки, если при линейном движении текущая и целевая координаты равны
        [Export] public float angularBrakeDefault = 50f;// градусы скорость вращения захвата робота вокруг целевой точки, если при линейном движении текущая и целевая координаты равны

        [Export] public float linearSpeedDefault = 0.5f;
        [Export] public float linearAccelerDefault = 0.5f;
        [Export] public float linearBrakeDefault = 0.5f;

        /// <summary>
        /// Текущая позиция осей (угловая) МЕНЯТЬ НЕЛЬЗЯ
        /// </summary>
        public float[] currentJointAngles { get => _controller.currentJointAngles; }
        public Node3D currentEndEffectorPoint { get => _controller.EndEffectorPoint; }


        public override void Initialize()
        {
            _controller =  _parent.GetComponent<RobotControllerComponent>();
        }
    }
}
#endif