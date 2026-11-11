using Godot;

[GlobalClass]
public partial class RobotPose : Resource
{
    [Export] public string ToolName { get; set; } = "Инструмент";
    [Export] public Vector3 Position { get; set; } = Vector3.Zero;
    [Export] public Quaternion Rotation { get; set; } = Quaternion.Identity;

    public RobotPose() { }
    public RobotPose(Vector3 position, Quaternion rotation)
    { Position = position; Rotation = rotation; }
}
