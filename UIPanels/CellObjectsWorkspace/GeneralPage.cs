
#if TOOLS
using Godot;

using RoboSim.Global;

namespace RoboSim.Editor.Pages;

[Tool]
public partial class GeneralEquipmentPage : VBoxContainer
{
    private CellObject _equipment;

    private LineEdit _nameInput;

    private SpinBox _positionX;
    private SpinBox _positionY;
    private SpinBox _positionZ;

    private bool _updating;

    public void Setup(CellObject equipment)
    {
        _equipment = equipment;

        if (IsNodeReady())
            Refresh();
    }

    public override void _Ready()
    {
        BuildInterface();
        Refresh();
    }

    private void BuildInterface()
    {
        var content = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };

        AddChild(content);

        // NAME

        content.AddChild(new Label
        {
            Text = "NAME"
        });

        _nameInput = new LineEdit
        {
            PlaceholderText = "Equipment name",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };

        _nameInput.TextSubmitted += OnNameSubmitted;
        _nameInput.FocusExited += ApplyName;

        content.AddChild(_nameInput);

        content.AddChild(new HSeparator());

        // POSITION

        content.AddChild(new Label
        {
            Text = "POSITION"
        });

        var positionRow = new HBoxContainer();

        _positionX = CreatePositionInput("X");
        _positionY = CreatePositionInput("Y");
        _positionZ = CreatePositionInput("Z");

        positionRow.AddChild(CreateAxisContainer("X", _positionX));
        positionRow.AddChild(CreateAxisContainer("Y", _positionY));
        positionRow.AddChild(CreateAxisContainer("Z", _positionZ));

        content.AddChild(positionRow);

        _positionX.ValueChanged += _ => ApplyPosition();
        _positionY.ValueChanged += _ => ApplyPosition();
        _positionZ.ValueChanged += _ => ApplyPosition();
    }

    private SpinBox CreatePositionInput(string placeholder)
    {
        return new SpinBox
        {
            MinValue = -100000,
            MaxValue = 100000,
            Step = 0.01,
            Editable = true,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
    }

    private Control CreateAxisContainer(string axis, SpinBox input)
    {
        var container = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };

        container.AddChild(new Label
        {
            Text = axis
        });

        container.AddChild(input);

        return container;
    }

    // =====================================================
    // REFRESH
    // =====================================================

    public void Refresh()
    {
        if (_equipment == null ||
            !GodotObject.IsInstanceValid(_equipment))
            return;

        _updating = true;

        _nameInput.Text = _equipment.Name;

        _positionX.Value = _equipment.Position.X;
        _positionY.Value = _equipment.Position.Y;
        _positionZ.Value = _equipment.Position.Z;

        _updating = false;
    }

    // =====================================================
    // NAME
    // =====================================================

    private void OnNameSubmitted(string value)
    {
        ApplyName();
    }

    private void ApplyName()
    {
        if (_updating || !IsEquipmentValid())
            return;

        string newName = _nameInput.Text.Trim();

        if (string.IsNullOrWhiteSpace(newName))
        {
            Refresh();
            return;
        }

        _equipment.Name = newName;
    }

    // =====================================================
    // POSITION
    // =====================================================

    private void ApplyPosition()
    {
        if (_updating || !IsEquipmentValid())
            return;

        _equipment.Position = new Vector3(
            (float)_positionX.Value,
            (float)_positionY.Value,
            (float)_positionZ.Value
        );
    }

    // =====================================================
    // HELPERS
    // =====================================================

    private bool IsEquipmentValid()
    {
        return _equipment != null &&
               GodotObject.IsInstanceValid(_equipment);
    }
}
#endif
