using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RoboSim.RobotObject;

public sealed record ProgramCommandDescriptor(Type Type, string Title, ProgramCommandScope Scopes)
{
    public NodeBase Create() => (NodeBase)Activator.CreateInstance(Type);
}

public static class ProgramCommandRegistry
{
    // Команды подключаются атрибутом: меню собирается автоматически по месту вставки.
    private static readonly IReadOnlyList<ProgramCommandDescriptor> Commands =
        typeof(NodeBase).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(NodeBase).IsAssignableFrom(type))
            .Select(type => (type, attribute: type.GetCustomAttribute<ProgramCommandAttribute>()))
            .Where(item => item.attribute != null)
            .Select(item => new ProgramCommandDescriptor(item.type, item.attribute.Title, item.attribute.Scopes))
            .OrderBy(item => item.Title, StringComparer.CurrentCulture)
            .ToArray();

    public static IReadOnlyList<ProgramCommandDescriptor> Get(ProgramCommandScope scope)
        => Commands.Where(command => (command.Scopes & scope) != 0).ToArray();
}
