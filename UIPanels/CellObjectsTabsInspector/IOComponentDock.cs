
#if TOOLS
using Godot;
using RoboSim.Global.Components.IOComponents;

namespace RoboSim.EditorPluginTools.Docks
{
    [Tool]
    public partial class IOComponentDock : VBoxContainer
    {
        private readonly EditorPlugin _plugin;
        private IOComponent _component;
        private VBoxContainer _inputsContainer;
        private VBoxContainer _outputsContainer;

        public IOComponentDock(EditorPlugin plugin, IOComponent component)
        {
            _plugin = plugin;
            _component = component;

            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            CustomMinimumSize = new Vector2(280, 0);

            Build();
        }

        private void Build()
        {
            var scroll = new ScrollContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
            };

            var content = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };

            content.AddChild(CreateSectionTitle("ЦИФРОВЫЕ ВХОДЫ"));

            _inputsContainer = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };
            content.AddChild(_inputsContainer);

            var addInput = new Button { Text = "+ Добавить вход" };
            addInput.Pressed += AddInput;
            content.AddChild(addInput);

            content.AddChild(new HSeparator());
            content.AddChild(CreateSectionTitle("ЦИФРОВЫЕ ВЫХОДЫ"));

            _outputsContainer = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };
            content.AddChild(_outputsContainer);

            var addOutput = new Button { Text = "+ Добавить выход" };
            addOutput.Pressed += AddOutput;
            content.AddChild(addOutput);

            scroll.AddChild(content);
            AddChild(scroll);

            Refresh();
        }

        private Label CreateSectionTitle(string text)
        {
            return new Label
            {
                Text = text,
                ThemeTypeVariation = "HeaderSmall"
            };
        }

        private void Refresh()
        {
            ClearContainer(_inputsContainer);
            ClearContainer(_outputsContainer);

            for (int i = 0; i < _component.DigitalInputs.Count; i++)
            {
                int index = i;
                CreateChannelRow(
                    _inputsContainer,
                    _component.DigitalInputs[index],
                    () => RemoveInput(index));
            }

            for (int i = 0; i < _component.DigitalOutputs.Count; i++)
            {
                int index = i;
                CreateChannelRow(
                    _outputsContainer,
                    _component.DigitalOutputs[index],
                    () => RemoveOutput(index));
            }

            if (_component.DigitalInputs.Count == 0)
                _inputsContainer.AddChild(CreateEmptyLabel("Нет цифровых входов"));

            if (_component.DigitalOutputs.Count == 0)
                _outputsContainer.AddChild(CreateEmptyLabel("Нет цифровых выходов"));
        }

        private static void ClearContainer(Container container)
        {
            while (container.GetChildCount() > 0)
            {
                Node child = container.GetChild(0);
                container.RemoveChild(child);
                child.QueueFree();
            }
        }

        private void CreateChannelRow(
            VBoxContainer parent,
            IOChannel channel,
            System.Action removeAction)
        {
            var row = new HBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };

            var nameField = new LineEdit
            {
                Text = channel.ChannelName,
                PlaceholderText = "Имя канала",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                CustomMinimumSize = new Vector2(100, 0)
            };

            nameField.TextChanged += value =>
            {
                if (channel.ChannelName == value)
                    return;

                channel.ChannelName = value;
                MarkChanged();
            };

            var valueToggle = new CheckBox
            {
                Text = "1",
                ButtonPressed = channel.Value,
                TooltipText = "Текущее значение канала"
            };

            valueToggle.Toggled += value =>
            {
                channel.Value = value;
                MarkChanged();
            };

            var removeButton = new Button
            {
                Text = "×",
                TooltipText = "Удалить канал"
            };

            removeButton.Pressed += () => removeAction();

            row.AddChild(nameField);
            row.AddChild(valueToggle);
            row.AddChild(removeButton);

            parent.AddChild(row);
        }

        private Label CreateEmptyLabel(string text)
        {
            return new Label
            {
                Text = text,
                Modulate = new Color(0.6f, 0.6f, 0.6f)
            };
        }

        private void AddInput()
        {
            string name = $"DI_{_component.DigitalInputs.Count + 1}";
            var channel = new IOChannel(
                name,
                IOChannelType.DigitalInput,
                _component);

            _component.DigitalInputs.Add(channel);

            MarkChanged();
            Refresh();
        }

        private void AddOutput()
        {
            string name = $"DO_{_component.DigitalOutputs.Count + 1}";
            var channel = new IOChannel(
                name,
                IOChannelType.DigitalOutput,
                _component);

            _component.DigitalOutputs.Add(channel);

            MarkChanged();
            Refresh();
        }

        private void RemoveInput(int index)
        {
            if (index < 0 || index >= _component.DigitalInputs.Count)
                return;

            IOChannel channel = _component.DigitalInputs[index];
            channel.Disconnect(out string error);

            _component.DigitalInputs.RemoveAt(index);

            MarkChanged();
            Refresh();
        }

        private void RemoveOutput(int index)
        {
            if (index < 0 || index >= _component.DigitalOutputs.Count)
                return;

            IOChannel channel = _component.DigitalOutputs[index];
            channel.Disconnect(out string error);

            _component.DigitalOutputs.RemoveAt(index);

            MarkChanged();
            Refresh();
        }

        private void MarkChanged()
        {
            _component.NotifyPropertyListChanged();
            EditorInterface.Singleton.MarkSceneAsUnsaved();
        }
    }
}
#endif
