#if TOOLS
using Godot;
using RoboSim.Global;
using System;

/// <summary>
/// Верхняя панель интерфейса
/// </summary>
[Tool]
public partial class TopToolbar : HBoxContainer
{
    public event Action SignalLinkManagerRequested;
    private Button _localB;
    private Button _globalB;
    private Button _jogB;

    private readonly Color _normal = new Color("#303944");
    private readonly Color _hover = new Color("#465363");
    private readonly Color _green = new Color("#238B55");
    private readonly Color _red = new Color("#B83D42");
    private readonly Color _blue = new Color("#2878B9");
    private readonly Color _orange = new Color("#C47B27");

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 8);

        var play = CreateButton("▶ Play", _green);
        play.Pressed += PlayPressed;
        AddChild(play);

        var stop = CreateButton("■ Stop", _red);
        stop.Pressed += StopPressed;
        AddChild(stop);

        AddChild(new VSeparator());

        var signalLinks = CreateButton("Связи сигналов", _blue);
        signalLinks.CustomMinimumSize = new Vector2(135, 34);
        signalLinks.Pressed += () => SignalLinkManagerRequested?.Invoke();
        AddChild(signalLinks);

        AddChild(new VSeparator());

        var rotateGroup = new ButtonGroup();

        _localB = CreateButton("Local", _blue, true);
        _localB.ButtonGroup = rotateGroup;
        _localB.Toggled += LocalToggled;
        AddChild(_localB);

        _globalB = CreateButton("Global", _blue, true);
        _globalB.ButtonGroup = rotateGroup;
        _globalB.Toggled += GlobalToggled;
        AddChild(_globalB);

        _jogB = CreateButton("Jog", _orange, true);
        _jogB.Toggled += JogToggled;
        AddChild(_jogB);

        // Начальные состояния
        _localB.SetPressedNoSignal(
            GlobalState.Instance.RotateMode == RotateModeEnum.Local
        );

        _globalB.SetPressedNoSignal(
            GlobalState.Instance.RotateMode == RotateModeEnum.Global
        );

        _jogB.SetPressedNoSignal(
            GlobalState.Instance.JogMode
        );
    }

    private Button CreateButton(
        string text,
        Color accent,
        bool toggle = false)
    {
        var button = new Button
        {
            Text = text,
            ToggleMode = toggle,
            CustomMinimumSize = new Vector2(85, 34),
            FocusMode = FocusModeEnum.None
        };

        button.AddThemeFontSizeOverride("font_size", 13);

        button.AddThemeColorOverride(
            "font_color", new Color("#E2E8F0"));

        button.AddThemeColorOverride(
            "font_hover_color", Colors.White);

        button.AddThemeColorOverride(
            "font_pressed_color", Colors.White);

        button.AddThemeStyleboxOverride(
            "normal", CreateStyle(_normal));

        button.AddThemeStyleboxOverride(
            "hover", CreateStyle(_hover));

        button.AddThemeStyleboxOverride(
            "pressed", CreateStyle(accent));

        button.AddThemeStyleboxOverride(
            "focus", new StyleBoxEmpty());

        return button;
    }

    private StyleBoxFlat CreateStyle(Color color)
    {
        var style = new StyleBoxFlat
        {
            BgColor = color,
            CornerRadiusTopLeft = 5,
            CornerRadiusTopRight = 5,
            CornerRadiusBottomLeft = 5,
            CornerRadiusBottomRight = 5,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 5,
            ContentMarginBottom = 5
        };

        return style;
    }

    private void PlayPressed()
    {
        GD.Print("Play");
        GlobalState.Instance.SimulationStatus = SimulationStatus.Play;
    }

    private void StopPressed()
    {
        GD.Print("Stop");
        GlobalState.Instance.SimulationStatus = SimulationStatus.Live;
    }

    private void LocalToggled(bool pressed)
    {
        if (!pressed) return;

        GlobalState.Instance.RotateMode = RotateModeEnum.Local;
    }

    private void GlobalToggled(bool pressed)
    {
        if (!pressed) return;

        GlobalState.Instance.RotateMode = RotateModeEnum.Global;
    }

    private void JogToggled(bool pressed)
    {
        GlobalState.Instance.JogMode = pressed;
    }
}
#endif
