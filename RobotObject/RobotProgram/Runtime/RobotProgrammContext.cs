using Godot;
using RoboSim.Global.Components.IOComponents;
using RoboSim.RobotObject.RobotComponents;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RoboSim.RobotObject;

/// <summary>Runtime state shared by all instructions during one program run.</summary>
public sealed class RobotProgrammContext
{
    private readonly List<string> _log = new();
    public IReadOnlyList<string> Log => _log;
    public CancellationTokenSource Cancellation { get; private set; } = new();
    public VariableManager VariableManager { get; }
    public IOComponent IOManager { get; }
    public Node Owner { get; }
    public NodeBase CurrentExecutingNode { get; set; }
    public string CurrentStatusText { get; set; } = "";
    public bool EnforceLinearConfigurationSequence { get; private set; }
    public int? PreviousLinearConfiguration { get; private set; }
    public string PreviousLinearPointName { get; private set; }

    public RobotProgrammContext(Node owner, VariableManager variableManager, IOComponent ioManager)
    { Owner = owner; VariableManager = variableManager; IOManager = ioManager; }

    public void AddLog(string message) => _log.Add(message);
    public bool IsRunning() => !Cancellation.IsCancellationRequested;
    public void Stop() { if (!Cancellation.IsCancellationRequested) Cancellation.Cancel(); }
    /// <summary>Начинает новый запуск и очищает состояние проверки цепочки LIN-точек.</summary>
    public void PrepareRun(bool enforceLinearConfigurationSequence = false)
    {
        Cancellation.Dispose();
        Cancellation = new CancellationTokenSource();
        _log.Clear();
        CurrentExecutingNode = null;
        CurrentStatusText = "";
        EnforceLinearConfigurationSequence = enforceLinearConfigurationSequence;
        PreviousLinearConfiguration = null;
        PreviousLinearPointName = null;
    }

    /// <summary>Проверяет и запоминает конфигурацию следующей последовательной LIN-команды.</summary>
    public string ValidateLinearConfiguration(int configuration, string pointName)
    {
        if (!EnforceLinearConfigurationSequence) return null;
        if (configuration < 0) return $"Для LIN-точки '{pointName}' не задана конфигурация IK.";
        if (configuration >= 8) return $"Для LIN-точки '{pointName}' задана неверная конфигурация IK {configuration + 1}; допустимы значения от 1 до 8.";
        if (PreviousLinearConfiguration is int previous && previous != configuration)
            return $"Несовпадение конфигурации IK у точки '{pointName}': " +
                   $"предыдущая LIN-точка '{PreviousLinearPointName}' использует конфигурацию {previous + 1}, " +
                   $"а текущая — {configuration + 1}.";
        PreviousLinearConfiguration = configuration;
        PreviousLinearPointName = pointName;
        return null;
    }

    /// <summary>Начинает новую цепочку LIN после выполнения PTP-команды.</summary>
    public void ResetLinearConfigurationSequence()
    {
        PreviousLinearConfiguration = null;
        PreviousLinearPointName = null;
    }

    public NodeStatus Fail(string message) { AddLog($"Ошибка: {message}"); GD.PushError(message); return NodeStatus.Failure; }
    public T ResolveNode<T>(NodePath path) where T : Node
    {
        if (path.ToString().Length == 0) return null;
        return Owner?.GetNodeOrNull<T>(path);
    }
    public async Task NextFrameAsync()
    {
        Cancellation.Token.ThrowIfCancellationRequested();
        if (Owner?.GetTree() is SceneTree tree) await Owner.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        Cancellation.Token.ThrowIfCancellationRequested();
    }
}
