using System;

namespace RoboSim.RobotObject;

[Flags]
public enum ProgramCommandScope
{
    Loop = 1,
    Initialization = 2,
    Branch = 4
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ProgramCommandAttribute : Attribute
{
    public string Title { get; }
    public ProgramCommandScope Scopes { get; }

    public ProgramCommandAttribute(string title, ProgramCommandScope scopes = ProgramCommandScope.Loop | ProgramCommandScope.Branch)
    { Title = title; Scopes = scopes; }
}
