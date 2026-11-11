using System.Threading.Tasks;

namespace RoboSim.RobotObject;

public enum RobotMotionType { PTP, LIN }

/// <summary>Motion commands that a program can request from a robot controller.</summary>
public interface IRobotMotion
{
    /// <summary>Возвращает текущую конфигурацию IK контроллера.</summary>
    int CurrentIkConfiguration { get; }
    /// <summary>Выполняет указанную точку выбранным режимом движения и настройками команды.</summary>
    Task<NodeStatus> ExecuteMotionAsync(WayPoint point, RobotMotionType motionType, float speed, int toolIndex, int ikConfiguration, RobotProgrammContext context);
}
