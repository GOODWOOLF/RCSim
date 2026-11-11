#if TOOLS
using System.Collections.Generic;
using RoboSim.Global.Components;
using Godot;

namespace RoboSim.RobotObject.RobotComponents
{
    [Tool]
    public partial class RobotPointsManagerComponent : CellComponent
    {
        // Интерфейс: удалить точку, добавить точку(аргумент), добавить точку по текущей позиции, двигаться к точке, (удаление и движение - кнопки на каждой точке)
        // Интерфейс:
        private PackedScene _wayPointScene =
            GD.Load<PackedScene>("res://RobotObject//WayPoint.tscn");
        private RobotControllerComponent _controller;
        private RobotProgrammComponent _programm;
        // =============================================
        //> Хранение данных
        // =============================================
        public List<WayPoint> _wayPoints = new();
        public int CurrentIkConfiguration => _controller?.CurrentIkConfiguration ?? 0;

        // =============================================
        //> Методы
        // =============================================

        /// <summary>
        /// Добавить точку
        /// </summary>
        /// <param name="point"></param>
        public void AddWayPoint(WayPoint point)
        {
            _wayPoints.Add(point);
        }
        /// <summary>
        /// Удалить точку
        /// </summary>
        /// <param name="point"></param>
        public void DeleteWayPoint(WayPoint point)
        {
            _wayPoints.Remove(point);
            point.QueueFree();
        }
        public WayPoint CreatePoint(string name)
        {
            if (_controller?.EndEffectorPoint == null) return null;
            WayPoint point = _wayPointScene.Instantiate<WayPoint>();

            point.SetParent(_parent);
            point.Initialize(
                new RobotPose(
                    _controller.EndEffectorPoint.Position,
                    _controller.EndEffectorPoint.Quaternion
                ),
                _controller.currentJointAngles,
                name
            );

            AddChild(point);
            point.Owner = GetTree().EditedSceneRoot;
            _wayPoints.Add(point);
            return point;
        }
        public void GoToWayPointPTP(WayPoint point)
        {
            if (_programm == null) { GD.PushError("RobotProgrammComponent не найден."); return; }
            _ = _programm.MoveToPointAsync(point, false);
        }
        public void GoToWayPointLinear(WayPoint point)
        {
            if (_programm == null) { GD.PushError("RobotProgrammComponent не найден."); return; }
            _ = _programm.MoveToPointAsync(point, true);
        }

        /// <summary>Переключает рабочую конфигурацию IK и пересчитывает суставы для текущей TCP-позы.</summary>
        public bool TrySetCurrentIkConfiguration(int configuration, out string error)
        {
            if (_controller == null)
            {
                error = "Контроллер робота не найден.";
                return false;
            }
            return _controller.TrySetCurrentIkConfiguration(configuration, out error);
        }
        public override void Initialize()
        {
            _controller = _parent.GetComponent<RobotControllerComponent>();
            _programm = _parent.GetComponent<RobotProgrammComponent>();
            foreach (Node child in GetChildren())
                if (child is WayPoint point && !_wayPoints.Contains(point))
                {
                    point.SetParent(_parent);
                    _wayPoints.Add(point);
                }
        }
        

    }
}
#endif
