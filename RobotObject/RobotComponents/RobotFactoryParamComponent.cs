#if TOOLS
using Godot;
using RoboSim.Global.Components;

namespace RoboSim.RobotObject.RobotComponents
{
    [Tool]
    public partial class RobotFactoryParamComponent : CellComponent
    {
        [Export] public float[] downLimit = { -175, -70, -170, -360, -100, -360 };
        [Export] public float[] upLimit = { 175, 50, 60, 360, 100, 360 };

        /// <summary>
        /// Ось вращения осей использовать только в робот контроллере
        /// </summary>
        public Vector3[] jointsDirect { get { return _jointsDirect; } }
        /// <summary></summary>
        private Vector3[] _jointsDirect = new Vector3[] { new Vector3(0, 1, 0), new Vector3(1, 0, 0),
            new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0) };

        /// <summary>
        /// размеры звеньев робота
        /// </summary>
        public JointsSize jointsSize { get { return _jointsSize; } }
        /// <summary></summary>
        private JointsSize _jointsSize = new JointsSize(a1: -450, a2: 350, b: 0, c1: 447, c2: 1150, c3: 1350, c4: 259);

        public override void Initialize()
        {
            //throw new System.NotImplementedException();
        }
    }
}
#endif