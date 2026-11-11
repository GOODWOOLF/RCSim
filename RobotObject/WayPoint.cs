#if TOOLS
using Godot;
using RoboSim.RobotObject.RobotComponents;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoboSim.Global;

namespace RoboSim.RobotObject
{
    [Tool]
    [GlobalClass]
    public partial class WayPoint :Node3D
    {
        private MeshInstance3D _mesh;
        private RobotMoveParametersComponent _move;
        public CellObject _parent;
        public void SetParent(CellObject parent) => _parent = parent;

        // =============================================
        //> Хранение данных
        // =============================================

        //> Позиция -> Position (от Node3D)
        //> Вращение -> Quaternion (от Node3D)
        [Export] public float[] Angles { get; set; } = new float[6];
        [Export] public bool HasStoredAngles { get; set; }

        // =============================================
        //> Методы
        // =============================================
        /// <summary>Возвращает сохранённую TCP-позу точки.</summary>
        public RobotPose GetPose()
        {
            return new RobotPose(Position, Quaternion);
        }
        /// <summary>Возвращает копию сохранённых суставных углов точки.</summary>
        public float[] GetCopyAngles()
        {
            return (float[])Angles.Clone();
        }
        /// <summary>Сохраняет TCP-позу и отдельную копию шести суставных углов.</summary>
        public void SetPoint(RobotPose pose, float[] angles)
        {
            Position = pose.Position;
            Quaternion = pose.Rotation;
            Angles = angles == null ? new float[6] : (float[])angles.Clone();
            HasStoredAngles = angles != null && angles.Length == 6;
        }
        /// <summary>Инициализирует точку текущей позой робота и сохраняет суставные углы для PTP.</summary>
        public void Initialize(RobotPose pose, float[] angles, string name)
        {
            Position = pose.Position;
            Quaternion = pose.Rotation;
            SetPoint(pose, angles);
            Name = name;
            _move = _parent.GetComponent<RobotMoveParametersComponent>();


        }
        /// <summary>Обновляет TCP-позу и PTP-углы точки текущим положением робота.</summary>
        public void UpdatePoint()
        {
            Position = _move.currentEndEffectorPoint.Position;
            Quaternion = _move.currentEndEffectorPoint.Quaternion;
            SetPoint(new RobotPose(Position, Quaternion), _move.currentJointAngles);

        }
        public void UpdateVisibility(bool v)
        {
            Visible = v;
        }

    }
}
#endif
