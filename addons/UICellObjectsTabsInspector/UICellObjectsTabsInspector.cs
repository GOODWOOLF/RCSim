
#if TOOLS
using Godot;
using RoboSim.RobotObject.RobotComponents;
using System.Collections.Generic;
using RoboSim.Global;
using RoboSim.Global.Components;
using RoboSim.Global.Components.IOComponents;
using RoboSim.EditorPluginTools.Docks;

[Tool]
public partial class UICellObjectsTabsInspector : EditorPlugin
{
    private EditorSelection _selection;

    private readonly List<EditorDock> _componentDocks = new();

    public override void _EnterTree()
    {
        _selection = EditorInterface.Singleton.GetSelection();
        _selection.SelectionChanged += OnSelectionChanged;

        OnSelectionChanged();
    }

    public override void _ExitTree()
    {
        _selection.SelectionChanged -= OnSelectionChanged;
        ClearTabs();
    }

    private void OnSelectionChanged()
    {
        ClearTabs();

        var selectedNodes = _selection.GetSelectedNodes();

        if (selectedNodes.Count == 0)
            return;

        CellObject cellObject = FindCellObject(selectedNodes[0]);

        if (cellObject == null)
            return;

        foreach (CellComponent component in cellObject.Components)
        {
            if (component == null)
                continue;

            CreateComponentTab(component);
        }
    }

    private CellObject FindCellObject(Node node)
    {
        while (node != null)
        {
            if (node is CellObject cellObject)
                return cellObject;

            node = node.GetParent();
        }

        return null;
    }

    private void CreateComponentTab(CellComponent component)
    {
        string title = component switch
        {
            IOComponent => "Входы/Выходы",
            RobotControllerComponent => "Контроллер робота",
            RobotToolsComponent => "Инструменты захвата",
            RobotPointsManagerComponent => "Точки робота",
            RobotProgrammComponent => "Программа робота",
            _ => component.GetType().Name
        };

        Control content = component switch
        {
            IOComponent io =>
                new IOComponentDock(this, io),

            RobotControllerComponent robot =>
                new RobotControllerDock(this, robot),

            RobotToolsComponent tools =>
                new RobotToolsDock(this, tools),

            RobotPointsManagerComponent points =>
                new RobotPointsManagerDock(this, points),

            RobotProgrammComponent program =>
                new RobotProgrammDock(this, program),

            _ => new VBoxContainer()
        };

        var dock = new EditorDock
        {
            Title = title,
            DefaultSlot = EditorDock.DockSlot.RightUl,
            AvailableLayouts =
                EditorDock.DockLayout.Vertical |
                EditorDock.DockLayout.Floating,
            Transient = true
        };

        dock.AddChild(content);

        AddDock(dock);
        _componentDocks.Add(dock);
    }

    private string GetComponentTabName(CellComponent component)
    {
        return component switch
        {
            RobotControllerComponent => "Контроллер робота",
            IOComponent => "Входы/Выходы",
            _ => component.GetType().Name
        };
    }

    private void ClearTabs()
    {
        foreach (EditorDock dock in _componentDocks)
        {
            if (!GodotObject.IsInstanceValid(dock))
                continue;

            dock.Close();
            RemoveDock(dock);
            dock.QueueFree();
        }

        _componentDocks.Clear();
    }
}
#endif
