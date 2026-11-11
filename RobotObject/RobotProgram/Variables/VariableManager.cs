using Godot;


/// <summary>Persistent collection of variable resources used by program instructions.</summary>
[GlobalClass]
public partial class VariableManager : Resource
{
    [Export] public Godot.Collections.Array<Variable> Variables { get; set; } = new();

    public BoolVariable AddBool(string name, bool initialValue = false)
    {
        var variable = new BoolVariable { Name = name, Value = initialValue };
        Variables.Add(variable);
        return variable;
    }
    public NumberVariable AddNumber(string name, float initialValue = 0f)
    {
        var variable = new NumberVariable { Name = name, Value = initialValue };
        Variables.Add(variable);
        return variable;
    }
    public Godot.Collections.Array<Variable> GetListVariable() => Variables;
    public void DeleteVariable(Variable variable) { if (variable != null) Variables.Remove(variable); }
    public Variable FindByName(string name)
    {
        foreach (var variable in Variables) if (variable != null && variable.Name == name) return variable;
        return null;
    }
}

public abstract partial class Variable : Resource
{
    [Export] public string Name { get; set; } = "";
}

public partial class BoolVariable : Variable
{
    [Export] public bool Value { get; set; }
}

public partial class NumberVariable : Variable
{
    [Export] public float Value { get; set; }
}
