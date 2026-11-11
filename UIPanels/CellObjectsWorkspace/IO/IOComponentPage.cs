
//#if TOOLS
//using Godot;
//using System.Collections.Generic;

//using РобоСим.Global;
//using РобоСим.Global.Components;

//namespace РобоСим.Editor.IO;

//[Tool]
//public partial class IOComponentPage : VBoxContainer
//{
//    private IOComponent _io;
//    private SignalManager _signalManager;

//    private readonly List<CellObject> _equipment = new();
//    private readonly Dictionary<string, ChannelRow> _rows = new();

//    private TabContainer _tabs;

//    private VBoxContainer _diList;
//    private VBoxContainer _doList;
//    private VBoxContainer _aiList;
//    private VBoxContainer _aoList;

//    private OptionButton _sourceEquipment;
//    private OptionButton _sourceChannel;
//    private OptionButton _targetEquipment;
//    private OptionButton _targetChannel;

//    private VBoxContainer _connectionsList;
//    private Label _statusLabel;

//    private bool _built;

//    private sealed class ChannelRow
//    {
//        public IOChannel Channel;
//        public Label ValueLabel;
//        public Button ManualButton;
//    }

//    // =====================================================
//    // SETUP
//    // =====================================================

//    public void Setup(
//        IOComponent io,
//        SignalManager signalManager,
//        IEnumerable<CellObject> equipment)
//    {
//        if (_io != null && GodotObject.IsInstanceValid(_io))
//            _io.ChannelChanged -= OnChannelChanged;

//        _io = io;
//        _signalManager = signalManager;

//        _equipment.Clear();

//        if (equipment != null)
//        {
//            foreach (CellObject item in equipment)
//            {
//                if (item != null && GodotObject.IsInstanceValid(item))
//                    _equipment.Add(item);
//            }
//        }

//        if (IsIOValid())
//        {
//            _io.SynchronizeConfiguration();
//            _io.ChannelChanged += OnChannelChanged;
//        }

//        if (_built)
//            Refresh();
//    }

//    public override void _Ready()
//    {
//        if (_built)
//            return;

//        BuildInterface();
//        _built = true;
//        Refresh();
//    }

//    public override void _ExitTree()
//    {
//        if (IsIOValid())
//            _io.ChannelChanged -= OnChannelChanged;

//        _built = false;
//    }

//    // =====================================================
//    // INTERFACE
//    // =====================================================

//    private void BuildInterface()
//    {
//        var scroll = new ScrollContainer
//        {
//            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
//            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
//            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
//        };

//        AddChild(scroll);

//        var content = new VBoxContainer
//        {
//            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
//        };

//        scroll.AddChild(content);

//        content.AddChild(new Label
//        {
//            Text = "I/O CONFIGURATION"
//        });

//        _tabs = new TabContainer
//        {
//            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
//            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
//            CustomMinimumSize = new Vector2(0, 250)
//        };

//        _diList = CreateChannelTab("DI");
//        _doList = CreateChannelTab("DO");
//        _aiList = CreateChannelTab("AI");
//        _aoList = CreateChannelTab("AO");

//        content.AddChild(_tabs);
//        content.AddChild(new HSeparator());

//        BuildConnectionEditor(content);

//        content.AddChild(new HSeparator());
//        content.AddChild(new Label { Text = "CONNECTIONS" });

//        _connectionsList = new VBoxContainer
//        {
//            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
//        };

//        content.AddChild(_connectionsList);

//        _statusLabel = new Label
//        {
//            Text = "Ready",
//            AutowrapMode = TextServer.AutowrapMode.WordSmart
//        };

//        content.AddChild(new HSeparator());
//        content.AddChild(_statusLabel);
//    }

//    private VBoxContainer CreateChannelTab(string tabName)
//    {
//        var page = new VBoxContainer
//        {
//            Name = tabName,
//            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
//        };

//        var scroll = new ScrollContainer
//        {
//            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
//            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
//            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
//        };

//        var list = new VBoxContainer
//        {
//            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
//        };

//        scroll.AddChild(list);
//        page.AddChild(scroll);

//        var addButton = new Button
//        {
//            Text = $"+ Add {tabName} channel"
//        };

//        addButton.Pressed += () => AddChannel(tabName);
//        page.AddChild(addButton);

//        _tabs.AddChild(page);

//        return list;
//    }

//    // =====================================================
//    // REFRESH
//    // =====================================================

//    public void Refresh()
//    {
//        if (!_built || !IsIOValid())
//            return;

//        _io.SynchronizeConfiguration();

//        _rows.Clear();

//        ClearContainer(_diList);
//        ClearContainer(_doList);
//        ClearContainer(_aiList);
//        ClearContainer(_aoList);

//        // Берём каналы непосредственно из массивов компонента.
//        AddChannelsFromArray(_io.DigitalInputs);
//        AddChannelsFromArray(_io.DigitalOutputs);
//        AddChannelsFromArray(_io.AnalogInputs);
//        AddChannelsFromArray(_io.AnalogOutputs);

//        RefreshEquipmentSelectors();
//        RefreshConnections();
//    }

//    private void AddChannelsFromArray(
//        Godot.Collections.Array<IOChannel> channels)
//    {
//        if (channels == null)
//            return;

//        foreach (IOChannel channel in channels)
//        {
//            if (channel != null &&
//                GodotObject.IsInstanceValid(channel))
//            {
//                AddChannelRow(channel);
//            }
//        }
//    }

//    // =====================================================
//    // CHANNEL ROWS
//    // =====================================================

//    private void AddChannelRow(IOChannel channel)
//    {
//        VBoxContainer list = GetChannelList(channel.ChannelType);

//        if (list == null)
//            return;

//        string channelName = channel.ChannelName;

//        var panel = new PanelContainer
//        {
//            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
//        };

//        var layout = new VBoxContainer();
//        panel.AddChild(layout);

//        var topRow = new HBoxContainer();

//        var nameInput = new LineEdit
//        {
//            Text = channelName,
//            PlaceholderText = "Channel name",
//            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
//        };

//        nameInput.TextSubmitted += value =>
//            RenameChannel(channel, value);

//        nameInput.FocusExited += () =>
//            RenameChannel(channel, nameInput.Text);

//        topRow.AddChild(nameInput);

//        var enabledCheck = new CheckBox
//        {
//            Text = "Enabled",
//            ButtonPressed = channel.Enabled
//        };

//        enabledCheck.Toggled += value =>
//        {
//            channel.Enabled = value;
//            Refresh();
//        };

//        topRow.AddChild(enabledCheck);

//        var deleteButton = new Button
//        {
//            Text = "Delete"
//        };

//        deleteButton.Pressed += () => RemoveChannel(channel);
//        topRow.AddChild(deleteButton);

//        layout.AddChild(topRow);

//        var valueRow = new HBoxContainer();

//        var valueLabel = new Label
//        {
//            Text = "Value: 0",
//            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
//        };

//        valueRow.AddChild(valueLabel);

//        var valueInput = new SpinBox
//        {
//            MinValue = 0,
//            MaxValue = channel.IsDigital ? 1 : 10,
//            Step = channel.IsDigital ? 1 : 0.1,
//            CustomMinimumSize = new Vector2(100, 0)
//        };

//        valueRow.AddChild(valueInput);

//        var manualButton = new Button
//        {
//            Text = "Set",
//            Disabled = true
//        };

//        manualButton.Pressed += () =>
//        {
//            if (!IsIOValid())
//                return;

//            bool success = _io.SetManualValue(
//                channel.ChannelName,
//                valueInput.Value
//            );

//            SetStatus(success
//                ? $"Value set: {channel.ChannelName}"
//                : "Manual control is available only for outputs in Live.");
//        };

//        valueRow.AddChild(manualButton);
//        layout.AddChild(valueRow);

//        list.AddChild(panel);

//        if (!string.IsNullOrEmpty(channelName))
//        {
//            _rows[channelName] = new ChannelRow
//            {
//                Channel = channel,
//                ValueLabel = valueLabel,
//                ManualButton = manualButton
//            };
//        }

//        UpdateChannel(channelName);
//    }

//    private VBoxContainer GetChannelList(IOChannelType type)
//    {
//        return type switch
//        {
//            IOChannelType.DigitalInput => _diList,
//            IOChannelType.DigitalOutput => _doList,
//            IOChannelType.AnalogInput => _aiList,
//            IOChannelType.AnalogOutput => _aoList,
//            _ => null
//        };
//    }

//    // =====================================================
//    // ADD / REMOVE / RENAME
//    // =====================================================

//    private void AddChannel(string tabName)
//    {
//        if (!IsIOValid())
//            return;

//        switch (tabName)
//        {
//            case "DI":
//                _io.DigitalInputCount++;
//                break;

//            case "DO":
//                _io.DigitalOutputCount++;
//                break;

//            case "AI":
//                _io.AnalogInputCount++;
//                break;

//            case "AO":
//                _io.AnalogOutputCount++;
//                break;

//            default:
//                return;
//        }

//        _io.SynchronizeConfiguration();

//        Refresh();
//        SetStatus($"{tabName} channel added.");
//    }

//    private void RemoveChannel(IOChannel channel)
//    {
//        if (!IsIOValid() || channel == null)
//            return;

//        switch (channel.ChannelType)
//        {
//            case IOChannelType.DigitalInput:
//                RemoveFromArray(_io.DigitalInputs, channel);
//                _io.DigitalInputCount = _io.DigitalInputs.Count;
//                break;

//            case IOChannelType.DigitalOutput:
//                RemoveFromArray(_io.DigitalOutputs, channel);
//                _io.DigitalOutputCount = _io.DigitalOutputs.Count;
//                break;

//            case IOChannelType.AnalogInput:
//                RemoveFromArray(_io.AnalogInputs, channel);
//                _io.AnalogInputCount = _io.AnalogInputs.Count;
//                break;

//            case IOChannelType.AnalogOutput:
//                RemoveFromArray(_io.AnalogOutputs, channel);
//                _io.AnalogOutputCount = _io.AnalogOutputs.Count;
//                break;
//        }

//        _io.SynchronizeConfiguration();

//        Refresh();
//        SetStatus("Channel removed.");
//    }

//    private void RemoveFromArray(
//        Godot.Collections.Array<IOChannel> array,
//        IOChannel channel)
//    {
//        int index = array.IndexOf(channel);

//        if (index >= 0)
//            array.RemoveAt(index);
//    }

//    private void RenameChannel(IOChannel channel, string newName)
//    {
//        if (channel == null || !IsIOValid())
//            return;

//        newName = newName.Trim();

//        if (string.IsNullOrWhiteSpace(newName))
//        {
//            Refresh();
//            return;
//        }

//        foreach (IOChannel other in _io.GetAllOutChannels())
//        {
//            if (other != channel &&
//                other.ChannelName == newName)
//            {
//                SetStatus("A channel with this name already exists.");
//                Refresh();
//                return;
//            }
//        }

//        string oldName = channel.ChannelName;

//        if (oldName == newName)
//            return;

//        channel.ChannelName = newName;

//        Refresh();
//        SetStatus($"Channel renamed: {newName}");
//    }

//    // =====================================================
//    // CHANNEL VALUE UPDATE
//    // =====================================================

//    private void OnChannelChanged(
//        string channelName,
//        double value,
//        IOValueSource source)
//    {
//        UpdateChannel(channelName);
//    }

//    private void UpdateChannel(string channelName)
//    {
//        if (string.IsNullOrEmpty(channelName))
//            return;

//        if (!_rows.TryGetValue(channelName, out ChannelRow row))
//            return;

//        if (!IsIOValid())
//            return;

//        IOChannelState state = _io.GetChannelState(channelName);

//        if (state == null)
//            return;

//        row.ValueLabel.Text =
//            $"Value: {state.EffectiveValue:0.###} ({state.Source})";

//        bool isLive =
//            GlobalState.Instance != null &&
//            GlobalState.Instance.SimulationStatus == SimulationStatus.Live;

//        row.ManualButton.Disabled =
//            !isLive ||
//            !row.Channel.IsOutput ||
//            !row.Channel.Enabled ||
//            !_io.AllowManualControl;
//    }

//    // =====================================================
//    // CONNECTION EDITOR
//    // =====================================================

//    private void BuildConnectionEditor(VBoxContainer parent)
//    {
//        parent.AddChild(new Label
//        {
//            Text = "CREATE CONNECTION"
//        });

//        var sourceRow = new HBoxContainer();

//        _sourceEquipment = new OptionButton
//        {
//            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
//        };

//        _sourceEquipment.ItemSelected += _ => RefreshSourceChannels();

//        _sourceChannel = new OptionButton
//        {
//            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
//        };

//        sourceRow.AddChild(_sourceEquipment);
//        sourceRow.AddChild(_sourceChannel);
//        parent.AddChild(sourceRow);

//        var targetRow = new HBoxContainer();

//        _targetEquipment = new OptionButton
//        {
//            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
//        };

//        _targetEquipment.ItemSelected += _ => RefreshTargetChannels();

//        _targetChannel = new OptionButton
//        {
//            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
//        };

//        targetRow.AddChild(_targetEquipment);
//        targetRow.AddChild(_targetChannel);
//        parent.AddChild(targetRow);

//        var connectButton = new Button
//        {
//            Text = "Connect"
//        };

//        connectButton.Pressed += ConnectChannels;
//        parent.AddChild(connectButton);
//    }

//    private void RefreshEquipmentSelectors()
//    {
//        _sourceEquipment.Clear();
//        _targetEquipment.Clear();

//        foreach (CellObject equipment in _equipment)
//        {
//            if (equipment == null ||
//                !GodotObject.IsInstanceValid(equipment))
//                continue;

//            if (FindIOComponent(equipment) == null)
//                continue;

//            _sourceEquipment.AddItem(equipment.Name);
//            _targetEquipment.AddItem(equipment.Name);
//        }

//        if (_sourceEquipment.ItemCount > 0)
//            _sourceEquipment.Select(0);

//        if (_targetEquipment.ItemCount > 0)
//            _targetEquipment.Select(0);

//        RefreshSourceChannels();
//        RefreshTargetChannels();
//    }

//    private IOComponent GetSelectedIO(OptionButton selector)
//    {
//        if (selector == null || selector.Selected < 0)
//            return null;

//        int current = 0;

//        foreach (CellObject equipment in _equipment)
//        {
//            if (equipment == null ||
//                !GodotObject.IsInstanceValid(equipment))
//                continue;

//            IOComponent io = FindIOComponent(equipment);

//            if (io == null)
//                continue;

//            if (current == selector.Selected)
//                return io;

//            current++;
//        }

//        return null;
//    }

//    private void RefreshSourceChannels()
//    {
//        _sourceChannel.Clear();

//        IOComponent io = GetSelectedIO(_sourceEquipment);

//        if (io == null)
//            return;

//        foreach (IOChannel channel in io.GetAllOutChannels())
//        {
//            if (channel != null &&
//                channel.Enabled &&
//                channel.IsOutput)
//            {
//                _sourceChannel.AddItem(channel.ChannelName);
//            }
//        }
//    }

//    private void RefreshTargetChannels()
//    {
//        _targetChannel.Clear();

//        IOComponent io = GetSelectedIO(_targetEquipment);

//        if (io == null)
//            return;

//        foreach (IOChannel channel in io.GetAllOutChannels())
//        {
//            if (channel != null &&
//                channel.Enabled &&
//                channel.IsInput)
//            {
//                _targetChannel.AddItem(channel.ChannelName);
//            }
//        }
//    }

//    private void ConnectChannels()
//    {
//        if (_signalManager == null ||
//            !GodotObject.IsInstanceValid(_signalManager))
//        {
//            SetStatus("SignalManager is not assigned.");
//            return;
//        }

//        IOComponent source = GetSelectedIO(_sourceEquipment);
//        IOComponent target = GetSelectedIO(_targetEquipment);

//        if (source == null || target == null ||
//            _sourceChannel.Selected < 0 ||
//            _targetChannel.Selected < 0)
//        {
//            SetStatus("Select equipment and channels.");
//            return;
//        }

//        string sourceName =
//            _sourceChannel.GetItemText(_sourceChannel.Selected);

//        string targetName =
//            _targetChannel.GetItemText(_targetChannel.Selected);

//        bool success = _signalManager.TryConnect(
//            source,
//            sourceName,
//            target,
//            targetName,
//            out string error
//        );

//        SetStatus(success ? "Connection created." : error);

//        RefreshConnections();
//    }

//    // =====================================================
//    // CONNECTION LIST
//    // =====================================================

//    private void RefreshConnections()
//    {
//        ClearContainer(_connectionsList);

//        if (_signalManager == null ||
//            !GodotObject.IsInstanceValid(_signalManager))
//            return;

//        foreach (SignalManager.SignalConnection connection
//                 in _signalManager.Connections)
//        {
//            if (connection == null)
//                continue;

//            if (connection.Source != _io &&
//                connection.Target != _io)
//                continue;

//            if (!GodotObject.IsInstanceValid(connection.Source) ||
//                !GodotObject.IsInstanceValid(connection.Target))
//                continue;

//            string sourceName =
//                connection.Source.GetParent()?.Name ?? "Unknown";

//            string targetName =
//                connection.Target.GetParent()?.Name ?? "Unknown";

//            var row = new HBoxContainer();

//            row.AddChild(new Label
//            {
//                Text =
//                    $"{sourceName}.{connection.SourceChannel} -> " +
//                    $"{targetName}.{connection.TargetChannel}",
//                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
//            });

//            var removeButton = new Button
//            {
//                Text = "Disconnect"
//            };

//            removeButton.Pressed += () =>
//            {
//                _signalManager.Disconnect(connection);
//                RefreshConnections();
//                SetStatus("Connection removed.");
//            };

//            row.AddChild(removeButton);
//            _connectionsList.AddChild(row);
//        }
//    }

//    // =====================================================
//    // HELPERS
//    // =====================================================

//    private IOComponent FindIOComponent(Node node)
//    {
//        if (node == null || !GodotObject.IsInstanceValid(node))
//            return null;

//        if (node is IOComponent io)
//            return io;

//        foreach (Node child in node.GetChildren())
//        {
//            IOComponent result = FindIOComponent(child);

//            if (result != null)
//                return result;
//        }

//        return null;
//    }

//    private bool IsIOValid()
//    {
//        return _io != null && GodotObject.IsInstanceValid(_io);
//    }

//    private void ClearContainer(Container container)
//    {
//        if (container == null)
//            return;

//        foreach (Node child in container.GetChildren())
//        {
//            container.RemoveChild(child);
//            child.QueueFree();
//        }
//    }

//    private void SetStatus(string message)
//    {
//        if (_statusLabel != null &&
//            GodotObject.IsInstanceValid(_statusLabel))
//        {
//            _statusLabel.Text = message;
//        }
//    }
//}
//#endif
