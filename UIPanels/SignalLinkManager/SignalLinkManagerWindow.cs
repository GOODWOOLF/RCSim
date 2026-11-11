#if TOOLS
using Godot;
using RoboSim.Global;
using RoboSim.Global.Components.IOComponents;
using RoboSim.Global.IO;
using System.Collections.Generic;

namespace РобоСим.Editor;

/// <summary>Окно редактора для создания и удаления связей выходных и входных каналов.</summary>
[Tool]
public partial class SignalLinkManagerWindow : Window
{
    private readonly List<IOComponent> _sourceComponents = new();
    private readonly List<IOComponent> _targetComponents = new();
    private readonly List<IOChannel> _sourceChannels = new();
    private readonly List<IOChannel> _targetChannels = new();

    private OptionButton _sourceObject;
    private OptionButton _sourceChannel;
    private OptionButton _targetObject;
    private OptionButton _targetChannel;
    private VBoxContainer _connections;
    private Label _status;

    /// <summary>Создаёт окно выбора объектов и каналов текущей сцены.</summary>
    public SignalLinkManagerWindow()
    {
        Title = "Менеджер связей сигналов";
        Size = new Vector2I(780, 560);
        MinSize = new Vector2I(620, 420);
        Unresizable = false;
        Transient = true;
        AlwaysOnTop = true;
        Visible = false;
    }

    /// <summary>Строит интерфейс и подписывается на изменения реестра сцены.</summary>
    public override void _Ready()
    {
        BuildInterface();
        SceneObjectRegistry.Changed += RefreshObjects;
        CloseRequested += Hide;
        RefreshObjects();
        Hide();
    }

    /// <summary>Отписывается от реестра при закрытии окна вместе с редактором.</summary>
    public override void _ExitTree()
    {
        SceneObjectRegistry.Changed -= RefreshObjects;
    }

    /// <summary>Обновляет реестр активной сцены и открывает окно связей.</summary>
    public void Open()
    {
        if (!IsInsideTree())
        {
            GD.PushError("[SignalLinkManager] Нельзя открыть окно: оно не добавлено в дерево редактора.");
            return;
        }

        Callable.From(ShowWindow).CallDeferred();
    }

    /// <summary>Показывает окно после завершения обработки нажатия кнопки редактора.</summary>
    private void ShowWindow()
    {
        Show();
        MoveToCenter();
        GrabFocus();
        RefreshSceneRegistry();
    }

    /// <summary>Обновляет реестр и списки после показа окна, не задерживая реакцию на клик.</summary>
    private void RefreshSceneRegistry()
    {
        SceneObjectRegistry.Refresh(EditorInterface.Singleton.GetEditedSceneRoot());
        RefreshObjects();
    }

    /// <summary>Создаёт поля выбора источника и приёмника, кнопку соединения и список активных связей.</summary>
    private void BuildInterface()
    {
        var root = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.AddThemeConstantOverride("separation", 8);
        AddChild(root);

        var sourceRow = new HBoxContainer();
        sourceRow.AddChild(new Label { Text = "Источник", CustomMinimumSize = new Vector2(95, 0), VerticalAlignment = VerticalAlignment.Center });
        _sourceObject = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _sourceObject.ItemSelected += _ => RefreshSourceChannels();
        _sourceChannel = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        sourceRow.AddChild(_sourceObject);
        sourceRow.AddChild(_sourceChannel);
        root.AddChild(sourceRow);

        var targetRow = new HBoxContainer();
        targetRow.AddChild(new Label { Text = "Приёмник", CustomMinimumSize = new Vector2(95, 0), VerticalAlignment = VerticalAlignment.Center });
        _targetObject = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _targetObject.ItemSelected += _ => RefreshTargetChannels();
        _targetChannel = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        targetRow.AddChild(_targetObject);
        targetRow.AddChild(_targetChannel);
        root.AddChild(targetRow);

        var connect = new Button { Text = "Создать связь: выход → вход" };
        connect.Pressed += ConnectSelectedChannels;
        root.AddChild(connect);
        _status = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        root.AddChild(_status);
        root.AddChild(new HSeparator());
        root.AddChild(new Label { Text = "Созданные связи", ThemeTypeVariation = "HeaderSmall" });

        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        _connections = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        scroll.AddChild(_connections);
        root.AddChild(scroll);

        var registryRefresh = new Timer { WaitTime = 0.5, Autostart = true };
        registryRefresh.Timeout += () =>
        {
            if (Visible)
                SceneObjectRegistry.Refresh(EditorInterface.Singleton.GetEditedSceneRoot());
        };
        AddChild(registryRefresh);
    }

    /// <summary>Заполняет списки объектами активной сцены, у которых есть IOComponent.</summary>
    private void RefreshObjects()
    {
        if (_sourceObject == null || !GodotObject.IsInstanceValid(_sourceObject)) return;

        _sourceComponents.Clear();
        _targetComponents.Clear();
        _sourceObject.Clear();
        _targetObject.Clear();
        _sourceObject.AddItem("— объект-источник —");
        _targetObject.AddItem("— объект-приёмник —");

        foreach (CellObject cell in SceneObjectRegistry.Objects)
        {
            if (!GodotObject.IsInstanceValid(cell)) continue;
            IOComponent io = cell.GetComponent<IOComponent>();
            if (!GodotObject.IsInstanceValid(io)) continue;

            _sourceComponents.Add(io);
            _targetComponents.Add(io);
            string label = $"{cell.Name} ({cell.GetType().Name})";
            _sourceObject.AddItem(label);
            _targetObject.AddItem(label);
        }

        _sourceObject.Select(0);
        _targetObject.Select(0);
        RefreshSourceChannels();
        RefreshTargetChannels();
        RefreshConnections();
    }

    /// <summary>Перестраивает список доступных цифровых выходов выбранного объекта.</summary>
    private void RefreshSourceChannels()
    {
        if (_sourceChannel == null) return;
        _sourceChannels.Clear();
        _sourceChannel.Clear();
        _sourceChannel.AddItem("— выход —");
        IOComponent io = GetSelectedComponent(_sourceObject, _sourceComponents);
        if (GodotObject.IsInstanceValid(io))
        {
            foreach (IOChannel channel in io.GetAllOutChannels())
            {
                _sourceChannels.Add(channel);
                _sourceChannel.AddItem(channel.ChannelName);
            }
        }
        _sourceChannel.Select(0);
    }

    /// <summary>Перестраивает список доступных цифровых входов выбранного объекта.</summary>
    private void RefreshTargetChannels()
    {
        if (_targetChannel == null) return;
        _targetChannels.Clear();
        _targetChannel.Clear();
        _targetChannel.AddItem("— вход —");
        IOComponent io = GetSelectedComponent(_targetObject, _targetComponents);
        if (GodotObject.IsInstanceValid(io))
        {
            foreach (IOChannel channel in io.GetAllInpChannels())
            {
                _targetChannels.Add(channel);
                _targetChannel.AddItem(channel.ChannelName);
            }
        }
        _targetChannel.Select(0);
    }

    /// <summary>Создаёт связь между выбранным выходом и входом через SignalLinkManager.</summary>
    private void ConnectSelectedChannels()
    {
        IOComponent source = GetSelectedComponent(_sourceObject, _sourceComponents);
        IOComponent target = GetSelectedComponent(_targetObject, _targetComponents);
        int sourceIndex = _sourceChannel.Selected - 1;
        int targetIndex = _targetChannel.Selected - 1;
        if (!GodotObject.IsInstanceValid(source) || !GodotObject.IsInstanceValid(target) ||
            sourceIndex < 0 || sourceIndex >= _sourceChannels.Count || targetIndex < 0 || targetIndex >= _targetChannels.Count)
        {
            _status.Text = "Выбери объект и канал с каждой стороны.";
            return;
        }

        if (SignalLinkManager.Instance.TryConnect(source, _sourceChannels[sourceIndex], target, _targetChannels[targetIndex], out string error))
        {
            _status.Text = "Связь создана.";
            RefreshConnections();
        }
        else
        {
            _status.Text = error;
        }
    }

    /// <summary>Обновляет строки уже созданных связей и кнопки отключения.</summary>
    private void RefreshConnections()
    {
        if (_connections == null) return;
        foreach (Node child in _connections.GetChildren())
        {
            _connections.RemoveChild(child);
            child.QueueFree();
        }

        foreach (SignalConnection connection in SignalLinkManager.Instance._connections)
        {
            if (!GodotObject.IsInstanceValid(connection) || !GodotObject.IsInstanceValid(connection.Source) ||
                !GodotObject.IsInstanceValid(connection.Target) || !GodotObject.IsInstanceValid(connection.SourceChannel) ||
                !GodotObject.IsInstanceValid(connection.TargetChannel)) continue;

            var row = new HBoxContainer();
            row.AddChild(new Label
            {
                Text = $"{connection.Source.Name}.{connection.SourceChannel.ChannelName}  →  " +
                       $"{connection.Target.Name}.{connection.TargetChannel.ChannelName}",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                VerticalAlignment = VerticalAlignment.Center
            });
            var remove = new Button { Text = "Отключить" };
            remove.Pressed += () =>
            {
                SignalLinkManager.Instance.DisconnectLink(connection.SourceChannel, connection.TargetChannel, out string error);
                _status.Text = string.IsNullOrEmpty(error) ? "Связь отключена." : error;
                RefreshConnections();
            };
            row.AddChild(remove);
            _connections.AddChild(row);
        }

        if (_connections.GetChildCount() == 0)
            _connections.AddChild(new Label { Text = "Связей пока нет." });
    }

    /// <summary>Возвращает IOComponent для выбранного индекса или null для строки-заполнителя.</summary>
    private static IOComponent GetSelectedComponent(OptionButton option, List<IOComponent> components)
    {
        if (option == null) return null;
        int index = option.Selected - 1;
        return index >= 0 && index < components.Count ? components[index] : null;
    }
}
#endif
