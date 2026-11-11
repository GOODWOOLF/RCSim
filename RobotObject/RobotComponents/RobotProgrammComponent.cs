#if TOOLS
using Godot;
using RoboSim.Global.Components;
using RoboSim.Global.Components.IOComponents;
using RoboSim.RobotObject;
using RoboSim.RobotObject.RobotComponents;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RoboSim.RobotObject.RobotComponents;

[Tool]
public partial class RobotProgrammComponent : CellComponent
{
    [Export] public Programm Program { get; set; }
    [Export] public VariableManager Variables { get; set; } = new();
    [Export] public IOComponent IO { get; set; }
    private IRobotMotion _motion;
    private RobotProgrammContext _rpc;
    private Task _runTask = Task.CompletedTask;
    private Task<NodeStatus> _pointMoveTask = Task.FromResult(NodeStatus.Success);
    public RobotProgrammContext Context => _rpc;
    public bool IsProgramRunning => !_runTask.IsCompleted;
    public bool IsStandaloneCommandRunning => !_pointMoveTask.IsCompleted;
    public RobotToolsComponent RobotTools => _parent?.GetComponent<RobotToolsComponent>();
    public RobotControllerComponent MotionController => _motion as RobotControllerComponent;
    public string ProgramStatusText { get; private set; } = "Остановлена";
    public Godot.Collections.Array<WayPoint> GetAvailablePoints()
    {
        var points = new Godot.Collections.Array<WayPoint>();
        var manager = _parent?.GetComponent<RobotPointsManagerComponent>();
        if (manager != null) foreach (var point in manager._wayPoints) if (GodotObject.IsInstanceValid(point)) points.Add(point);
        return points;
    }

    public WayPoint CreateWaypointAtCurrentPosition(string name)
        => _parent?.GetComponent<RobotPointsManagerComponent>()?.CreatePoint(name);

    [ExportToolButton("Запустить программу")]
    public Callable RunButton => Callable.From(StartProgram);
    [ExportToolButton("Остановить программу")]
    public Callable StopButton => Callable.From(StopProgram);

    public override void Initialize()
    {
        Variables ??= new VariableManager();
        _motion = _parent?.GetComponent<RobotControllerComponent>();
        if (IO == null) IO = _parent?.GetComponent<IOComponent>();
        _rpc = new RobotProgrammContext(this, Variables, IO);
    }

    /// <summary>Запускает программу целиком и включает проверку конфигураций последовательных LIN-команд.</summary>
    public void StartProgram()
    {
        if (_runTask is { IsCompleted: false }) return;
        if (!_pointMoveTask.IsCompleted) { GD.PushError("Сначала завершите отдельную команду робота."); return; }
        if (_motion == null) { GD.PushError("RobotProgrammComponent: контроллер робота не найден."); return; }
        if (Program == null) { GD.PushError("RobotProgrammComponent: программа не назначена."); return; }
        _rpc.PrepareRun(enforceLinearConfigurationSequence: true);
        ProgramStatusText = "Выполняется";
        _runTask = RunProgramAsync();
    }

    /// <summary>Выполняет выбранный узел программы отдельно, не запуская корневой цикл.</summary>
    public async Task<NodeStatus> ExecuteCommandAsync(NodeBase node)
    {
        if (node == null) return _rpc.Fail("Команда не выбрана.");
        if (_motion == null) return _rpc.Fail("RobotProgrammComponent: контроллер робота не найден.");
        if (!_runTask.IsCompleted) return _rpc.Fail("Сначала остановите программу робота.");
        if (!_pointMoveTask.IsCompleted) return _rpc.Fail("Уже выполняется отдельная команда робота.");

        _rpc.PrepareRun();
        _rpc.CurrentExecutingNode = node;
        _rpc.CurrentStatusText = string.IsNullOrWhiteSpace(node.DisplayName) ? node.GetType().Name : node.DisplayName;
        ProgramStatusText = $"Выполняется: {_rpc.CurrentStatusText}";
        _pointMoveTask = ExecuteSingleCommandAsync(node);
        return await _pointMoveTask;
    }

    /// <summary>Запускает тело одной команды и сохраняет её результат в статусе компонента.</summary>
    private async Task<NodeStatus> ExecuteSingleCommandAsync(NodeBase node)
    {
        try
        {
            NodeStatus result = await node.Execute(_rpc, _motion);
            ProgramStatusText = result == NodeStatus.Success ? "Команда завершена" : "Команда остановлена";
            return result;
        }
        catch (OperationCanceledException)
        {
            ProgramStatusText = "Остановлена";
            return NodeStatus.Failure;
        }
        catch (Exception error)
        {
            ProgramStatusText = "Ошибка";
            return _rpc.Fail($"Ошибка команды: {error.Message}");
        }
    }

    private async Task RunProgramAsync()
    {
        try
        {
            NodeStatus result = await Program.Execute(_rpc, _motion);
            ProgramStatusText = result == NodeStatus.Success ? "Завершена" : "Остановлена";
        }
        catch (OperationCanceledException) { ProgramStatusText = "Остановлена"; }
        catch (Exception e) { ProgramStatusText = "Ошибка"; _rpc.AddLog($"Ошибка выполнения: {e.Message}"); GD.PushError(e.ToString()); }
    }

    /// <summary>
    /// Движение к точек из поинт менеджера
    /// </summary>
    /// <param name="point"></param>
    /// <param name="linear"></param>
    /// <returns></returns>
    public async Task<NodeStatus> MoveToPointAsync(WayPoint point, bool linear)
    {
        if (point == null) return _rpc.Fail("Точка движения не задана.");
        if (_motion == null) return _rpc.Fail("RobotProgrammComponent: контроллер робота не найден.");
        if (!_runTask.IsCompleted) return _rpc.Fail("Выполняется программа робота.");
        if (!_pointMoveTask.IsCompleted) return _rpc.Fail("Уже выполняется отдельная команда робота.");

        _rpc.PrepareRun();
        ProgramStatusText = "Выполняется переход к точке";
        _pointMoveTask = RunPointMoveAsync(point, linear);
        return await _pointMoveTask;
    }

    /// <summary>Выполняет выбранную точку менеджера через тот же интерфейс, что и команда программы.</summary>
    private async Task<NodeStatus> RunPointMoveAsync(WayPoint point, bool linear)
    {
        try
        {
            return linear
                ? await _motion.ExecuteMotionAsync(point, RobotMotionType.LIN, 0.5f, -1, _motion.CurrentIkConfiguration, _rpc)
                : await _motion.ExecuteMotionAsync(point, RobotMotionType.PTP, 100f, -1, -1, _rpc);
        }
        catch (OperationCanceledException) { return NodeStatus.Failure; }
        catch (Exception e) { return _rpc.Fail($"Ошибка перехода к точке: {e.Message}"); }
    }

    public void StopProgram()
    {
        if (_rpc == null) return;
        ProgramStatusText = "Остановка…";
        _rpc.Stop();
    }
}
#endif
