
#if TOOLS
using Godot;
using System;

[Tool]
public partial class PluginManager : EditorPlugin
{
    private const string OwnPlugin = "plugin_manager";

    private VBoxContainer _panel;
    private HBoxContainer _header;
    private VBoxContainer _list;
    private ScrollContainer _scroll;
    private Timer _timer;

    private string _lastSnapshot = "";

    private const float EnabledWidth = 100;
    private const float PluginWidth = 200;
    private const float DescriptionWidth = 350;

    public override void _EnterTree()
    {
        CreatePanel();

        AddControlToBottomPanel(_panel, "Plugin Manager");

        _timer = new Timer
        {
            WaitTime = 1.0,
            Autostart = true
        };

        AddChild(_timer);
        _timer.Timeout += RefreshPlugins;

        RefreshPlugins();
    }

    public override void _ExitTree()
    {
        if (_timer != null)
        {
            _timer.Timeout -= RefreshPlugins;
            _timer.Stop();
            _timer.QueueFree();
        }

        if (_panel != null)
        {
            RemoveControlFromBottomPanel(_panel);
            _panel.QueueFree();
        }
    }

    // PANEL

    private void CreatePanel()
    {
        _panel = new VBoxContainer
        {
            Name = "PluginManagerPanel",
            CustomMinimumSize = new Vector2(0, 180)
        };

        var title = new Label
        {
            Text = "PLUGIN MANAGER",
            HorizontalAlignment = HorizontalAlignment.Center
        };

        _panel.AddChild(title);
        _panel.AddChild(new HSeparator());

        _header = new HBoxContainer();
        _panel.AddChild(_header);

        _scroll = new ScrollContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };

        _list = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };

        _scroll.AddChild(_list);
        _panel.AddChild(_scroll);

        BuildHeader();
    }

    // HEADER

    private void BuildHeader()
    {
        AddHeaderColumn("Enabled", EnabledWidth);
        AddHeaderColumn("Plugin", PluginWidth);
        AddHeaderColumn("Description", DescriptionWidth);
    }

    private void AddHeaderColumn(string text, float width)
    {
        var label = new Label
        {
            Text = text,
            CustomMinimumSize = new Vector2(width, 28),
            VerticalAlignment = VerticalAlignment.Center
        };

        _header.AddChild(label);
    }

    // AUTO UPDATE

    private void RefreshPlugins()
    {
        string snapshot = CreateSnapshot();

        if (snapshot == _lastSnapshot)
            return;

        _lastSnapshot = snapshot;

        RebuildRows();
    }

    private string CreateSnapshot()
    {
        var addons = DirAccess.Open("res://addons");

        if (addons == null)
            return "NO_ADDONS";

        string[] directories = addons.GetDirectories();
        Array.Sort(directories);

        string result = "";

        foreach (string directory in directories)
        {
            if (directory == OwnPlugin)
                continue;

            string path =
                $"res://addons/{directory}/plugin.cfg";

            if (!FileAccess.FileExists(path))
                continue;

            var config = new ConfigFile();

            if (config.Load(path) != Error.Ok)
                continue;

            string name = config.GetValue(
                "plugin", "name", directory
            ).AsString();

            string description = config.GetValue(
                "plugin", "description", ""
            ).AsString();

            bool enabled =
                EditorInterface.Singleton.IsPluginEnabled(directory);

            result += $"{directory}|{name}|{description}|{enabled}\n";
        }

        return result;
    }

    // PLUGIN LIST

    private void RebuildRows()
    {
        if (_list == null)
            return;

        foreach (Node child in _list.GetChildren())
        {
            _list.RemoveChild(child);
            child.QueueFree();
        }

        var addons = DirAccess.Open("res://addons");

        if (addons == null)
            return;

        string[] directories = addons.GetDirectories();
        Array.Sort(directories);

        foreach (string directory in directories)
        {
            if (directory == OwnPlugin)
                continue;

            string path =
                $"res://addons/{directory}/plugin.cfg";

            if (!FileAccess.FileExists(path))
                continue;

            var config = new ConfigFile();

            if (config.Load(path) != Error.Ok)
                continue;

            string name = config.GetValue(
                "plugin", "name", directory
            ).AsString();

            string description = config.GetValue(
                "plugin", "description", ""
            ).AsString();

            bool enabled =
                EditorInterface.Singleton.IsPluginEnabled(directory);

            CreatePluginRow(
                directory,
                name,
                description,
                enabled
            );
        }
    }

    // PLUGIN ROW

    private void CreatePluginRow(
        string directory,
        string name,
        string description,
        bool enabled)
    {
        var row = new HBoxContainer();

        row.SizeFlagsHorizontal =
            Control.SizeFlags.ExpandFill;

        // ENABLED

        var toggle = CreateToggle(
            directory,
            enabled,
            name
        );

        toggle.CustomMinimumSize =
            new Vector2(EnabledWidth, 28);

        row.AddChild(toggle);

        // NAME

        var nameLabel = new Label
        {
            Text = name,
            CustomMinimumSize = new Vector2(PluginWidth, 28),
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = true,
            TooltipText = name
        };

        row.AddChild(nameLabel);

        // DESCRIPTION

        var descriptionLabel = new Label
        {
            Text = description,
            CustomMinimumSize = new Vector2(DescriptionWidth, 28),
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = true,
            TooltipText = description,
            MouseFilter = Control.MouseFilterEnum.Stop
        };

        row.AddChild(descriptionLabel);

        _list.AddChild(row);
    }

    // ENABLE / DISABLE

    private Control CreateToggle(
        string directory,
        bool enabled,
        string pluginName)
    {
        var checkbox = new CheckButton
        {
            ButtonPressed = enabled,
            Text = enabled ? "ON" : "OFF",
            TooltipText = "Enable / Disable plugin"
        };

        checkbox.Toggled += (value) =>
        {
            if (directory == OwnPlugin)
            {
                checkbox.SetPressedNoSignal(true);
                return;
            }

            EditorInterface.Singleton.SetPluginEnabled(
                directory,
                value
            );

            checkbox.Text = value ? "ON" : "OFF";

            GD.Print(
                $"Plugin {pluginName}: {(value ? "ON" : "OFF")}"
            );

            _lastSnapshot = "";
        };

        return checkbox;
    }
}
#endif
