#if TOOLS
using Godot;
using RoboSim.Global.Components;
using RoboSim.RobotObject;

namespace RoboSim.RobotObject.RobotComponents;

[Tool]
public partial class RobotToolsComponent : CellComponent
{
    public event System.Action ActiveToolChanged;

    /// <summary>Настроенные смещения TCP каждого инструмента.</summary>
    [Export] public Godot.Collections.Array<RobotPose> EndEffectorOffset { get; set; } = new();

    /// <summary>Индекс инструмента, который сейчас использует контроллер.</summary>
    private int _selectedToolIndex;
    [Export]
    public int SelectedToolIndex
    {
        get => _selectedToolIndex;
        set
        {
            if (_selectedToolIndex == value) return;
            _selectedToolIndex = value;
            ActiveToolChanged?.Invoke();
        }
    }

    public RobotPose CurrentTool
    {
        get
        {
            EnsureAtLeastOneTool();
            SelectedToolIndex = Mathf.Clamp(SelectedToolIndex, 0, EndEffectorOffset.Count - 1);
            return EndEffectorOffset[SelectedToolIndex];
        }
    }

    public override void Initialize() => EnsureAtLeastOneTool();

    public override void _EnterTree()
    {
        base._EnterTree();
        EnsureAtLeastOneTool();
    }

    public int AddTool(string name = null)
    {
        EnsureArray();
        var tool = new RobotPose
        {
            ToolName = string.IsNullOrWhiteSpace(name) ? $"Инструмент {EndEffectorOffset.Count + 1}" : name,
            Position = new Vector3(0, 0.3f, 0),
            Rotation = Quaternion.Identity
        };
        EndEffectorOffset.Add(tool);
        SelectedToolIndex = EndEffectorOffset.Count - 1;
        return SelectedToolIndex;
    }

    public bool DeleteTool(int index)
    {
        if (EndEffectorOffset == null || index < 0 || index >= EndEffectorOffset.Count || EndEffectorOffset.Count <= 1)
            return false;
        EndEffectorOffset.RemoveAt(index);
        SelectedToolIndex = Mathf.Clamp(SelectedToolIndex > index ? SelectedToolIndex - 1 : SelectedToolIndex, 0, EndEffectorOffset.Count - 1);
        return true;
    }

    public void SelectTool(int index)
    {
        if (EndEffectorOffset == null || EndEffectorOffset.Count == 0) { SelectedToolIndex = 0; return; }
        SelectedToolIndex = Mathf.Clamp(index, 0, EndEffectorOffset.Count - 1);
    }

    /// <summary>Сообщает контроллеру, что изменилось смещение активного инструмента.</summary>
    public void NotifyActiveToolPoseChanged(RobotPose tool)
    {
        if (tool != null && ReferenceEquals(tool, CurrentTool)) ActiveToolChanged?.Invoke();
    }

    private void EnsureArray() => EndEffectorOffset ??= new Godot.Collections.Array<RobotPose>();

    private void EnsureAtLeastOneTool()
    {
        EnsureArray();
        if (EndEffectorOffset.Count == 0)
            AddTool("Стандартный инструмент");
        SelectedToolIndex = Mathf.Clamp(SelectedToolIndex, 0, EndEffectorOffset.Count - 1);
    }
}
#endif
