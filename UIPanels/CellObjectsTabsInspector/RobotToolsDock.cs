#if TOOLS
using Godot;
using RoboSim.RobotObject.RobotComponents;
using System;

namespace RoboSim.EditorPluginTools.Docks;

[Tool]
public partial class RobotToolsDock : VBoxContainer
{
    private readonly EditorPlugin _plugin;
    private readonly RobotToolsComponent _component;
    private VBoxContainer _list;
    private VBoxContainer _details;

    public RobotToolsDock(EditorPlugin plugin, RobotToolsComponent component)
    {
        _plugin = plugin;
        _component = component;
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;
        CustomMinimumSize = new Vector2(320, 0);
        Build();
        Refresh();
    }

    private void Build()
    {
        var header = new HBoxContainer();
        header.AddChild(new Label { Text = "Инструменты захвата", ThemeTypeVariation = "HeaderSmall", SizeFlagsHorizontal = SizeFlags.ExpandFill });
        var add = new Button { Text = "+ Добавить инструмент" };
        add.Pressed += () => { _component.AddTool(); _component.NotifyPropertyListChanged(); MarkChanged(); Refresh(); };
        header.AddChild(add);
        AddChild(header);
        AddChild(new HSeparator());

        var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        var content = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        content.AddChild(new Label { Text = "Список инструментов" });
        _list = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        content.AddChild(_list);
        content.AddChild(new HSeparator());
        content.AddChild(new Label { Text = "Параметры выбранного инструмента" });
        _details = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        content.AddChild(_details);
        scroll.AddChild(content);
        AddChild(scroll);
    }

    private void Refresh()
    {
        Clear(_list);
        Clear(_details);
        for (int i = 0; i < _component.EndEffectorOffset.Count; i++)
        {
            int index = i;
            RobotPose tool = _component.EndEffectorOffset[i];
            var row = new HBoxContainer();
            var select = new Button
            {
                Text = $"{(index == _component.SelectedToolIndex ? "●" : "○")}  {tool.ToolName}",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                Alignment = HorizontalAlignment.Left
            };
            select.Pressed += () => { _component.SelectTool(index); MarkChanged(); Refresh(); };
            row.AddChild(select);
            var delete = new Button { Text = "Удалить", Disabled = _component.EndEffectorOffset.Count <= 1 };
            delete.Pressed += () =>
            {
                if (_component.DeleteTool(index)) { _component.NotifyPropertyListChanged(); MarkChanged(); Refresh(); }
            };
            row.AddChild(delete);
            _list.AddChild(row);
        }

        if (_component.EndEffectorOffset.Count == 0) return;
        _component.SelectTool(_component.SelectedToolIndex);
        RobotPose selected = _component.CurrentTool;
        var name = new LineEdit { Text = selected.ToolName, PlaceholderText = "Название инструмента" };
        name.TextChanged += value =>
        {
            selected.ToolName = value;
            MarkChanged();
            // Синхронно обновляем строку списка, не пересоздавая поле ввода.
            var selectedButton = _list.GetChild(_component.SelectedToolIndex).GetChild(0) as Button;
            if (selectedButton != null) selectedButton.Text = $"●  {value}";
        };
        _details.AddChild(name);

        AddAxisRow("Позиция, м", selected.Position, value =>
        {
            selected.Position = value;
            _component.NotifyActiveToolPoseChanged(selected);
        }, " м", -1000, 1000, 0.001);
        Vector3 eulerDegrees = new(Mathf.RadToDeg(selected.Rotation.GetEuler().X), Mathf.RadToDeg(selected.Rotation.GetEuler().Y), Mathf.RadToDeg(selected.Rotation.GetEuler().Z));
        AddAxisRow("Вращение, °", eulerDegrees, value =>
        {
            selected.Rotation = Quaternion.FromEuler(new Vector3(Mathf.DegToRad(value.X), Mathf.DegToRad(value.Y), Mathf.DegToRad(value.Z)));
            _component.NotifyActiveToolPoseChanged(selected);
        }, "°", -360, 360, 0.5);
    }

    private void AddAxisRow(string title, Vector3 initial, Action<Vector3> update, string suffix, double min, double max, double step)
    {
        _details.AddChild(new Label { Text = title });
        var row = new HBoxContainer();
        Vector3 current = initial;
        float[] axes = { current.X, current.Y, current.Z };
        string[] names = { "X", "Y", "Z" };
        for (int axis = 0; axis < 3; axis++)
        {
            int axisIndex = axis;
            var field = new SpinBox { MinValue = min, MaxValue = max, Step = step, Value = axes[axis], Suffix = suffix, Prefix = names[axis] + " ", SizeFlagsHorizontal = SizeFlags.ExpandFill };
            field.ValueChanged += value =>
            {
                Vector3 result = axisIndex switch
                {
                    0 => new Vector3((float)value, current.Y, current.Z),
                    1 => new Vector3(current.X, (float)value, current.Z),
                    _ => new Vector3(current.X, current.Y, (float)value)
                };
                current = result;
                update(result);
                MarkChanged();
            };
            row.AddChild(field);
        }
        _details.AddChild(row);
    }

    private void MarkChanged() => EditorInterface.Singleton.MarkSceneAsUnsaved();

    private static void Clear(Container container)
    {
        foreach (Node child in container.GetChildren()) { container.RemoveChild(child); child.QueueFree(); }
    }
}
#endif
