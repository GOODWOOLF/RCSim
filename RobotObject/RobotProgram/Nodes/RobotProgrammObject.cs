using Godot;
using RoboSim.Global.Components.IOComponents;
using RoboSim.RobotObject.RobotComponents;
using System;
using System.Threading.Tasks;

namespace RoboSim.RobotObject;

public enum NodeStatus { Success, Failure }

/// <summary>Base instruction in a robot program. Instructions are Resources so the editor can compose them.</summary>
public abstract partial class NodeBase : Resource
{
    [Export] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Export] public string DisplayName { get; set; } = "";
    public abstract Task<NodeStatus> Execute(RobotProgrammContext context, IRobotMotion robotMotion);
}

public partial class MainNode : NodeBase
{
    [Export] public Godot.Collections.Array<NodeBase> Children { get; set; } = new();
    public override async Task<NodeStatus> Execute(RobotProgrammContext context, IRobotMotion motion)
    {
        while (context.IsRunning())
        {
            foreach (var child in Children)
            {
                if (!context.IsRunning()) return NodeStatus.Failure;
                if (child == null) return context.Fail("В программе есть пустая команда.");
                context.CurrentExecutingNode = child;
                context.CurrentStatusText = string.IsNullOrWhiteSpace(child.DisplayName) ? child.GetType().Name : child.DisplayName;
                context.AddLog($"Выполнение: {context.CurrentStatusText}");
                var result = await child.Execute(context, motion);
                if (result != NodeStatus.Success) return result;
            }
            if (Children.Count == 0) return NodeStatus.Success;
            await context.NextFrameAsync();
        }
        return NodeStatus.Failure;
    }
}

public partial class InitializeList : Resource
{
    [Export] public Godot.Collections.Array<VariableAssignmentNode> InitList { get; set; } = new();
    public async Task<NodeStatus> Execute(RobotProgrammContext context, IRobotMotion motion)
    {
        foreach (var node in InitList)
        {
            if (!context.IsRunning()) return NodeStatus.Failure;
            if (node == null) return context.Fail("Список инициализации содержит пустую команду.");
            context.CurrentExecutingNode = node;
            context.CurrentStatusText = string.IsNullOrWhiteSpace(node.DisplayName) ? node.GetType().Name : node.DisplayName;
            context.AddLog($"Инициализация: {context.CurrentStatusText}");
            var result = await node.Execute(context, motion);
            if (result != NodeStatus.Success) return result;
        }
        return NodeStatus.Success;
    }
}

public partial class Programm : Resource
{
    [Export] public InitializeList Initialization { get; set; } = new();
    [Export] public MainNode MainNode { get; set; } = new();
    public async Task<NodeStatus> Execute(RobotProgrammContext context, IRobotMotion motion)
    {
        if (MainNode == null) return context.Fail("Не назначен корневой узел программы.");
        var init = Initialization == null ? NodeStatus.Success : await Initialization.Execute(context, motion);
        return init == NodeStatus.Success ? await MainNode.Execute(context, motion) : init;
    }
}

[ProgramCommand("Движение PTP")]
public partial class PTPNode : NodeBase
{
    [Export] public NodePath PointPath { get; set; } = new();
    [Export(PropertyHint.Range, "0,100,1")] public float SpeedPercent { get; set; } = 100;
    [Export] public int ToolIndex { get; set; } = -1;
    public override Task<NodeStatus> Execute(RobotProgrammContext c, IRobotMotion m)
    { var point = c.ResolveNode<WayPoint>(PointPath); if (point == null) return Task.FromResult(c.Fail($"PTP: точка не найдена по пути '{PointPath}'.")); c.CurrentStatusText = $"PTP → {point.Name}"; return m.ExecuteMotionAsync(point, RobotMotionType.PTP, SpeedPercent, ToolIndex, -1, c); }
}
[ProgramCommand("Движение LIN")]
public partial class LINNode : NodeBase
{
    [Export] public NodePath PointPath { get; set; } = new();
    [Export(PropertyHint.Range, "0.001,10,0.01,or_greater,suffix:m/s")] public float SpeedMetersPerSecond { get; set; } = 0.5f;
    [Export] public int ToolIndex { get; set; } = -1;
    [Export] public int IKConfiguration { get; set; } = -1;
    public override Task<NodeStatus> Execute(RobotProgrammContext c, IRobotMotion m)
    { var point = c.ResolveNode<WayPoint>(PointPath); if (point == null) return Task.FromResult(c.Fail($"LIN: точка не найдена по пути '{PointPath}'.")); c.CurrentStatusText = $"LIN → {point.Name} ({SpeedMetersPerSecond:0.###} м/с)"; return m.ExecuteMotionAsync(point, RobotMotionType.LIN, SpeedMetersPerSecond, ToolIndex, IKConfiguration, c); }
}
[ProgramCommand("Записать цифровой выход")]
public partial class SetDigitalOutputNode : NodeBase
{
    [Export] public IOChannel Channel { get; set; }
    [Export] public bool Value { get; set; }
    public override Task<NodeStatus> Execute(RobotProgrammContext c, IRobotMotion m)
    { if (Channel == null || !Channel.isOutput) return Task.FromResult(c.Fail("Не назначен выходной канал.")); Channel.Value = Value; c.AddLog($"Выход {Channel.ChannelName} = {Value}"); return Task.FromResult(NodeStatus.Success); }
}

public enum TypeOperation { VariableToVariable, VariableToValue, Increment, Decrement }
public abstract partial class VariableAssignmentNode : NodeBase { }

[ProgramCommand("Задать числовую переменную", ProgramCommandScope.Loop | ProgramCommandScope.Initialization | ProgramCommandScope.Branch)]
public partial class SetVariableNumber : VariableAssignmentNode
{
    [Export] public NumberVariable Variable { get; set; }
    [Export] public TypeOperation Operation { get; set; }
    [Export] public float Value { get; set; }
    [Export] public NumberVariable Source { get; set; }
    public override Task<NodeStatus> Execute(RobotProgrammContext c, IRobotMotion m)
    {
        if (Variable == null) return Task.FromResult(c.Fail("Не назначена целевая числовая переменная."));
        if (Operation == TypeOperation.VariableToVariable && Source == null) return Task.FromResult(c.Fail("Не назначена переменная-источник."));
        Variable.Value = Operation switch { TypeOperation.VariableToVariable => Source.Value, TypeOperation.VariableToValue => Value, TypeOperation.Increment => Variable.Value + Value, TypeOperation.Decrement => Variable.Value - Value, _ => Variable.Value };
        c.AddLog($"{Variable.Name} = {Variable.Value}"); return Task.FromResult(NodeStatus.Success);
    }
}
public enum TypeOperationBool { VariableToVariable, VariableToValue, Toggle, AndWithValue, OrWithValue, XorWithValue, AndWithVariable, OrWithVariable, XorWithVariable }
[ProgramCommand("Задать логическую переменную", ProgramCommandScope.Loop | ProgramCommandScope.Initialization | ProgramCommandScope.Branch)]
public partial class SetVariableBool : VariableAssignmentNode
{
    [Export] public BoolVariable Variable { get; set; }
    [Export] public TypeOperationBool Operation { get; set; }
    [Export] public bool Value { get; set; }
    [Export] public BoolVariable Source { get; set; }
    public override Task<NodeStatus> Execute(RobotProgrammContext c, IRobotMotion m)
    {
        if (Variable == null) return Task.FromResult(c.Fail("Не назначена целевая логическая переменная."));
        if ((Operation == TypeOperationBool.VariableToVariable || Operation.ToString().EndsWith("WithVariable")) && Source == null) return Task.FromResult(c.Fail("Не назначена переменная-источник."));
        bool rhs = Source?.Value ?? Value;
        Variable.Value = Operation switch { TypeOperationBool.VariableToVariable => rhs, TypeOperationBool.VariableToValue => Value, TypeOperationBool.Toggle => !Variable.Value, TypeOperationBool.AndWithValue or TypeOperationBool.AndWithVariable => Variable.Value && rhs, TypeOperationBool.OrWithValue or TypeOperationBool.OrWithVariable => Variable.Value || rhs, TypeOperationBool.XorWithValue or TypeOperationBool.XorWithVariable => Variable.Value ^ rhs, _ => Variable.Value };
        c.AddLog($"{Variable.Name} = {Variable.Value}"); return Task.FromResult(NodeStatus.Success);
    }
}
[ProgramCommand("Ожидать цифровой вход")]
public partial class WaitDigitalInputNode : NodeBase
{
    [Export] public IOChannel Channel { get; set; }
    [Export] public bool ExpectedValue { get; set; } = true;
    public override async Task<NodeStatus> Execute(RobotProgrammContext c, IRobotMotion m)
    {
        if (Channel == null || !Channel.isInput) return c.Fail("Не назначен входной канал.");
        c.AddLog($"Ожидание {Channel.ChannelName} = {ExpectedValue}");
        while (c.IsRunning()) { if (Channel.Value == ExpectedValue) return NodeStatus.Success; await c.NextFrameAsync(); }
        return NodeStatus.Failure;
    }
}

public partial class ConditionBranch : Resource
{
    [Export] public string BranchName { get; set; } = "Если";
    [Export(PropertyHint.MultilineText)] public string Expression { get; set; } = "";
    [Export] public Godot.Collections.Array<NodeBase> Actions { get; set; } = new();
}
[ProgramCommand("Условие: если / иначе")]
public partial class IfElseNode : NodeBase
{
    [Export] public Godot.Collections.Array<ConditionBranch> Branches { get; set; } = new();
    [Export] public Godot.Collections.Array<NodeBase> ElseNodes { get; set; } = new();
    public override async Task<NodeStatus> Execute(RobotProgrammContext c, IRobotMotion m)
    {
        foreach (var branch in Branches)
        {
            if (branch == null || string.IsNullOrWhiteSpace(branch.Expression)) return c.Fail("Не задано условие ветки.");
            bool matches;
            try { matches = ConditionExpressionParser.Evaluate(branch.Expression, c); }
            catch (Exception error) { return c.Fail($"Ошибка условия «{branch.BranchName}»: {error.Message}"); }
            if (matches) return await RunList(branch.Actions, c, m);
        }
        return await RunList(ElseNodes, c, m);
    }
    private static async Task<NodeStatus> RunList(Godot.Collections.Array<NodeBase> nodes, RobotProgrammContext c, IRobotMotion m)
    { foreach (var node in nodes) { if (!c.IsRunning()) return NodeStatus.Failure; if (node == null) return c.Fail("Ветка содержит пустую команду."); c.CurrentExecutingNode = node; c.CurrentStatusText = string.IsNullOrWhiteSpace(node.DisplayName) ? node.GetType().Name : node.DisplayName; c.AddLog($"Выполнение: {c.CurrentStatusText}"); var result = await node.Execute(c, m); if (result != NodeStatus.Success) return result; } return NodeStatus.Success; }
}
