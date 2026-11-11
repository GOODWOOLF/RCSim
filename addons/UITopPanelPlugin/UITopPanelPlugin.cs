#if TOOLS
using Godot;
using System;
using System.Collections.Generic;
using РобоСим.Editor;
using RoboSim.Global;
/// <summary>
/// Плагин интерфейса главного
/// </summary>
[Tool]
public partial class UITopPanelPlugin : EditorPlugin
{
    private TopToolbar _toolbar;
    private SignalLinkManagerWindow _signalLinkWindow;

    // Сцена оборудования, выбранная в библиотеке.
    public override void _EnterTree()
    {
        GD.Print("Плагин загружен");

        // Toolbar
        _toolbar = new TopToolbar();
        _toolbar.SignalLinkManagerRequested += OpenSignalLinkManager;

        AddControlToContainer(
            CustomControlContainer.SpatialEditorMenu,
            _toolbar
        );


        _signalLinkWindow = new SignalLinkManagerWindow();
        EditorInterface.Singleton.GetBaseControl().AddChild(_signalLinkWindow);

    }

    public override void _ExitTree()
    {
        GD.Print("Плагин выгружен");

        // Удаляем Toolbar
        if (_toolbar != null)
        {
            _toolbar.SignalLinkManagerRequested -= OpenSignalLinkManager;
            RemoveControlFromContainer(
                CustomControlContainer.SpatialEditorMenu,
                _toolbar
            );

            _toolbar.QueueFree();
            _toolbar = null;
        }


        if (_signalLinkWindow != null && GodotObject.IsInstanceValid(_signalLinkWindow))
        {
            if (_signalLinkWindow.GetParent() != null)
                _signalLinkWindow.GetParent().RemoveChild(_signalLinkWindow);
            _signalLinkWindow.QueueFree();
            _signalLinkWindow = null;
        }
    }

    /// <summary>Открывает менеджер связей каналов текущей активной сцены.</summary>
    private void OpenSignalLinkManager()
    {
        GD.Print("[RoboSim Toolbar] Нажата кнопка «Связи сигналов».");
        if (_signalLinkWindow == null || !GodotObject.IsInstanceValid(_signalLinkWindow))
        {
            GD.PushError("[RoboSim Toolbar] Окно менеджера связей не создано.");
            return;
        }
        _signalLinkWindow.Open();
    }
}

        //}
        //private const string SpatialEditorGroup = "_spatial_editor_group";

        //private HBoxContainer _nativeToolbar;
        //private HBoxContainer _customToolbar;

        //private readonly Dictionary<string, Button> _nativeButtons = new();
        //private readonly Dictionary<Button, Button> _buttonLinks = new();

        //private static readonly Dictionary<string, string> NativeButtonNames = new()
        //{
        //    ["Transform"] = "Режим трансформации",
        //    ["Move"] = "Режим перемещения",
        //    ["Rotate"] = "Режим вращения",
        //    ["Scale"] = "Режим масштабирования",
        //    ["Select"] = "Режим выделения",
        //    ["Ruler"] = "Режим измерения",
        //    ["Local"] = "Использовать локальное пространство",
        //    ["Snap"] = "Использовать привязки",
        //    ["Trackball"] = "Использовать трекбол",
        //    ["PreserveChildren"] = "Сохранить положение дочерних узлов"
        //};

        //public override void _EnterTree()
        //{
        //    CallDeferred(nameof(InitializeToolbar));
        //}

        //public override void _ExitTree()
        //{
        //    // Удаляем собственную панель.
        //    if (_customToolbar != null)
        //    {
        //        RemoveControlFromContainer(
        //            CustomControlContainer.SpatialEditorMenu,
        //            _customToolbar
        //        );

        //        _customToolbar.QueueFree();
        //        _customToolbar = null;
        //    }

        //    // Восстанавливаем только скрытые штатные кнопки.
        //    foreach (Button button in _nativeButtons.Values)
        //    {
        //        if (GodotObject.IsInstanceValid(button))
        //            button.Visible = true;
        //    }

        //    _nativeButtons.Clear();
        //    _buttonLinks.Clear();
        //    _nativeToolbar = null;
        //}

        //private void InitializeToolbar()
        //{
        //    FindNativeButtons();

        //    if (_nativeButtons.Count == 0)
        //    {
        //        GD.PushError(
        //            "[RoboSim Toolbar] Не найдены штатные кнопки 3D-редактора."
        //        );
        //        return;
        //    }

        //    FindNativeToolbar();

        //    if (_nativeToolbar == null)
        //    {
        //        GD.PushError(
        //            "[RoboSim Toolbar] Не найден общий HBoxContainer штатных кнопок."
        //        );
        //        return;
        //    }

        //    CreateCustomToolbar();

        //    // Скрываем только кнопки, для которых есть замены.
        //    HideReplacedNativeButtons();

        //    GD.Print(
        //        $"[RoboSim Toolbar] Инициализация завершена. Найдено кнопок: {_nativeButtons.Count}"
        //    );
        //}

        //private void FindNativeButtons()
        //{
        //    _nativeButtons.Clear();

        //    var spatialEditors = GetTree().GetNodesInGroup(
        //        new StringName(SpatialEditorGroup)
        //    );

        //    GD.Print(
        //        $"[RoboSim Toolbar] Найдено 3D-редакторов: {spatialEditors.Count}"
        //    );

        //    foreach (Node spatialEditor in spatialEditors)
        //    {
        //        FindButtonsRecursive(spatialEditor);
        //    }

        //    foreach (var pair in _nativeButtons)
        //    {
        //        GD.Print(
        //            $"[RoboSim Toolbar] {pair.Key}: {pair.Value.GetPath()}"
        //        );
        //    }

        //    GD.Print(
        //        $"[RoboSim Toolbar] Всего найдено кнопок: {_nativeButtons.Count}"
        //    );
        //}

        //private void FindButtonsRecursive(Node node)
        //{
        //    if (node is Button button)
        //    {
        //        string accessibilityName = button.GetAccessibilityName();

        //        foreach (var pair in NativeButtonNames)
        //        {
        //            if (accessibilityName == pair.Value)
        //            {
        //                _nativeButtons[pair.Key] = button;
        //                break;
        //            }
        //        }
        //    }

        //    foreach (Node child in node.GetChildren(true))
        //    {
        //        FindButtonsRecursive(child);
        //    }
        //}

        ///// <summary>
        ///// Находит общий контейнер штатных кнопок.
        ///// </summary>
        //private void FindNativeToolbar()
        //{
        //    _nativeToolbar = null;

        //    if (!_nativeButtons.TryGetValue("Move", out Button moveButton))
        //    {
        //        GD.PushError(
        //            "[RoboSim Toolbar] Не найдена кнопка Move."
        //        );
        //        return;
        //    }

        //    if (moveButton.GetParent() is not HBoxContainer parent)
        //    {
        //        GD.PushError(
        //            "[RoboSim Toolbar] Родитель кнопки Move не является HBoxContainer."
        //        );
        //        return;
        //    }

        //    _nativeToolbar = parent;

        //    GD.Print(
        //        $"[RoboSim Toolbar] Найден штатный ряд: {_nativeToolbar.GetPath()}"
        //    );
        //}

        ///// <summary>
        ///// Скрывает только те штатные кнопки,
        ///// для которых создаются собственные замены.
        ///// </summary>
        //private void HideReplacedNativeButtons()
        //{
        //    foreach (var pair in _nativeButtons)
        //    {
        //        if (GodotObject.IsInstanceValid(pair.Value))
        //        {
        //            pair.Value.Visible = false;
        //        }
        //    }
        //}

        //private void CreateCustomToolbar()
        //{
        //    _customToolbar = new HBoxContainer
        //    {
        //        Name = "RoboSimCustomToolbar",
        //        SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
        //        SizeFlagsVertical = Control.SizeFlags.ShrinkCenter
        //    };

        //    _customToolbar.AddThemeConstantOverride("separation", 4);

        //    AddToolButton("Transform", "Transform");

        //    AddSeparator();

        //    AddToolButton("Move", "Move");
        //    AddToolButton("Rotate", "Rotate");
        //    AddToolButton("Scale", "Scale");
        //    AddToolButton("Select", "Select");
        //    AddToolButton("Ruler", "Ruler");

        //    AddSeparator();

        //    AddToggleButton("Local", "Local");
        //    AddToggleButton("Snap", "Snap");
        //    AddToggleButton("Trackball", "Trackball");
        //    AddToggleButton("PreserveChildren", "Preserve Children");

        //    AddControlToContainer(
        //        CustomControlContainer.SpatialEditorMenu,
        //        _customToolbar
        //    );

        //    GD.Print("[RoboSim Toolbar] Собственная панель добавлена.");
        //}

        //private void AddToolButton(string nativeKey, string text)
        //{
        //    if (!_nativeButtons.TryGetValue(nativeKey, out Button nativeButton))
        //        return;

        //    var customButton = new Button
        //    {
        //        Text = text,
        //        ToggleMode = nativeButton.ToggleMode,
        //        ButtonPressed = nativeButton.ButtonPressed,
        //        TooltipText = nativeButton.TooltipText
        //    };

        //    customButton.Pressed += () =>
        //    {
        //        InvokeNativeButton(nativeButton);
        //        UpdateButtonStates();
        //    };

        //    _buttonLinks[customButton] = nativeButton;
        //    _customToolbar.AddChild(customButton);
        //}

        //private void AddToggleButton(string nativeKey, string text)
        //{
        //    if (!_nativeButtons.TryGetValue(nativeKey, out Button nativeButton))
        //        return;

        //    var customButton = new Button
        //    {
        //        Text = text,
        //        ToggleMode = true,
        //        ButtonPressed = nativeButton.ButtonPressed,
        //        TooltipText = nativeButton.TooltipText
        //    };

        //    customButton.Pressed += () =>
        //    {
        //        InvokeNativeButton(nativeButton);
        //        UpdateButtonStates();
        //    };

        //    _buttonLinks[customButton] = nativeButton;
        //    _customToolbar.AddChild(customButton);
        //}

        ///// <summary>
        ///// Передаёт нажатие штатной кнопке Godot.
        ///// </summary>
        //private void InvokeNativeButton(Button nativeButton)
        //{
        //    if (!GodotObject.IsInstanceValid(nativeButton))
        //        return;

        //    if (nativeButton.ToggleMode)
        //    {
        //        nativeButton.ButtonPressed = !nativeButton.ButtonPressed;
        //    }

        //    nativeButton.EmitSignal(Button.SignalName.Pressed);
        //}

        //private void UpdateButtonStates()
        //{
        //    foreach (var pair in _buttonLinks)
        //    {
        //        Button customButton = pair.Key;
        //        Button nativeButton = pair.Value;

        //        if (!GodotObject.IsInstanceValid(customButton))
        //            continue;

        //        if (!GodotObject.IsInstanceValid(nativeButton))
        //            continue;

        //        customButton.ButtonPressed = nativeButton.ButtonPressed;
        //    }
        //}

        //private void AddSeparator()
        //{
        //    _customToolbar.AddChild(new VSeparator());
        //}
    

#endif
