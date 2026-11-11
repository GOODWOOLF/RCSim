
#if TOOLS
using Godot;
using RoboSim.Global.Components;

namespace RoboSim.EditorPluginTools.Docks
{
    [Tool]
    public partial class RobotControllerDock : VBoxContainer
    {
        private readonly EditorPlugin _plugin;
        private RobotControllerComponent _component;

        private VBoxContainer _jointsContainer;
        private readonly Label[] _actualJointAngleLabels = new Label[6];
        private readonly HSlider[] _jointSliders = new HSlider[6];
        private readonly SpinBox[] _jointTargetFields = new SpinBox[6];
        private OptionButton _ikConfigurationSelector;
        private bool _updatingJointControls;

        public RobotControllerDock(
            EditorPlugin plugin,
            RobotControllerComponent component)
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

            content.AddChild(CreateSectionTitle("ПАРАМЕТРЫ КОНТРОЛЛЕРА"));
            content.AddChild(new HSeparator());
            content.AddChild(CreateTargetJointAnglesSection());

            content.AddChild(CreateNodeReferenceRow(
                "End Effector Point",
                () => _component.EndEffectorPoint,
                node => _component.EndEffectorPoint = node));

            content.AddChild(CreateNodeReferenceRow(
                "Flange",
                () => _component.Flange,
                node => _component.Flange = node));

            content.AddChild(new HSeparator());
            content.AddChild(CreateSectionTitle("СУСТАВЫ"));

            _jointsContainer = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };

            content.AddChild(_jointsContainer);

            var addJointsButton = new Button
            {
                Text = "+ Добавить выбранные узлы"
            };

            addJointsButton.Pressed += AddSelectedJoints;
            content.AddChild(addJointsButton);

            content.AddChild(new HSeparator());
            content.AddChild(CreateSectionTitle("КИНЕМАТИКА"));

            var limitsToggle = new Button
            {
                ToggleMode = true,
                ButtonPressed = !_component.EnforceJointLimits,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                TooltipText = "Отключает проверку пределов осей для движений робота"
            };
            void UpdateLimitsButton(bool limitsDisabled)
            {
                _component.EnforceJointLimits = !limitsDisabled;
                limitsToggle.Text = limitsDisabled
                    ? "Лимиты осей: отключены"
                    : "Лимиты осей: включены";
            }
            UpdateLimitsButton(limitsToggle.ButtonPressed);
            limitsToggle.Toggled += disabled =>
            {
                UpdateLimitsButton(disabled);
                MarkChanged();
            };
            content.AddChild(limitsToggle);

            var numIkRow = new HBoxContainer();
            numIkRow.AddChild(new Label
            {
                Text = "Текущая конфигурация IK",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            });

            _ikConfigurationSelector = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            for (int configuration = 0; configuration < 8; configuration++)
                _ikConfigurationSelector.AddItem($"Конфигурация {configuration + 1}", configuration);
            _ikConfigurationSelector.Selected = System.Math.Clamp(_component.NumIK, 0, 7);
            _ikConfigurationSelector.ItemSelected += index =>
            {
                if (!_component.TrySetCurrentIkConfiguration((int)index, out string error))
                {
                    GD.PushError(error);
                    _ikConfigurationSelector.Selected = System.Math.Clamp(_component.NumIK, 0, 7);
                    return;
                }
                MarkChanged();
            };

            numIkRow.AddChild(_ikConfigurationSelector);
            content.AddChild(numIkRow);

            scroll.AddChild(content);
            AddChild(scroll);

            var controlRefresh = new Timer { WaitTime = 0.1, Autostart = true };
            controlRefresh.Timeout += UpdateJointActualReadouts;
            AddChild(controlRefresh);

            RefreshJoints();
            UpdateJointActualReadouts();
        }

        private Label CreateSectionTitle(string text)
        {
            return new Label
            {
                Text = text,
                ThemeTypeVariation = "HeaderSmall"
            };
        }

        private Control CreateNodeReferenceRow(
            string title,
            System.Func<Node3D> getter,
            System.Action<Node3D> setter)
        {
            var container = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };

            container.AddChild(new Label { Text = title });

            var row = new HBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };

            var nodeLabel = new Label
            {
                Text = GetNodeDisplayName(getter()),
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                ClipText = true
            };

            var setButton = new Button
            {
                Text = "Назначить",
                TooltipText = "Назначить выбранный в дереве сцены Node3D"
            };

            setButton.Pressed += () =>
            {
                Node3D selected = GetFirstSelectedNode3D();

                if (selected == null)
                {
                    GD.PushWarning("Выбери Node3D в дереве сцены.");
                    return;
                }

                setter(selected);
                nodeLabel.Text = GetNodeDisplayName(selected);
                MarkChanged();
            };

            var clearButton = new Button
            {
                Text = "×",
                TooltipText = "Очистить ссылку"
            };

            clearButton.Pressed += () =>
            {
                setter(null);
                nodeLabel.Text = "Не назначен";
                MarkChanged();
            };

            row.AddChild(nodeLabel);
            row.AddChild(setButton);
            row.AddChild(clearButton);

            container.AddChild(row);
            return container;
        }

        private string GetNodeDisplayName(Node3D node)
        {
            if (node == null)
                return "Не назначен";

            return $"{node.Name} ({node.GetPath()})";
        }

        private Node3D GetFirstSelectedNode3D()
        {
            var selection = EditorInterface.Singleton.GetSelection();

            foreach (Node node in selection.GetSelectedNodes())
            {
                if (node is Node3D node3D && node3D != _component)
                    return node3D;
            }

            return null;
        }

        private void AddSelectedJoints()
        {
            var selection = EditorInterface.Singleton.GetSelection();
            var joints = _component.jointsTransform ?? System.Array.Empty<Node3D>();

            var result = new System.Collections.Generic.List<Node3D>(joints);

            foreach (Node node in selection.GetSelectedNodes())
            {
                if (node is not Node3D node3D || node3D == _component)
                    continue;

                if (result.Contains(node3D))
                    continue;

                result.Add(node3D);
            }

            _component.jointsTransform = result.ToArray();

            MarkChanged();
            RefreshJoints();
        }

        private void RemoveJoint(int index)
        {
            var joints = _component.jointsTransform;

            if (joints == null || index < 0 || index >= joints.Length)
                return;

            var result = new System.Collections.Generic.List<Node3D>(joints);
            result.RemoveAt(index);

            _component.jointsTransform = result.ToArray();

            MarkChanged();
            RefreshJoints();
        }
        private Control CreateTargetJointAnglesSection()
        {
            var section = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };

            section.AddChild(CreateSectionTitle("ЦЕЛЕВЫЕ УГЛЫ"));

            float[] angles = _component.targetJointAngles;

            if (angles == null || angles.Length != 6)
            {
                _component.targetJointAngles = new float[6];
                angles = _component.targetJointAngles;
            }

            for (int i = 0; i < 6; i++)
            {
                int index = i;

                var row = new VBoxContainer
                {
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
                };

                var header = new HBoxContainer();

                var nameLabel = new Label
                {
                    Text = $"J{i + 1}",
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
                };

                (float lowerLimit, float upperLimit) = _component.GetJointAngleLimits(index);
                var actualAngle = new Label
                {
                    Text = "Факт: —",
                    TooltipText = "Фактический угол оси робота",
                    HorizontalAlignment = HorizontalAlignment.Right,
                    CustomMinimumSize = new Vector2(105, 0)
                };
                _actualJointAngleLabels[index] = actualAngle;

                var valueField = new SpinBox
                {
                    MinValue = lowerLimit,
                    MaxValue = upperLimit,
                    Step = 0.1,
                    Value = Mathf.Clamp(angles[i], lowerLimit, upperLimit),
                    TooltipText = "Целевой угол оси",
                    CustomMinimumSize = new Vector2(85, 0)
                };
                _jointTargetFields[index] = valueField;

                var slider = new HSlider
                {
                    MinValue = lowerLimit,
                    MaxValue = upperLimit,
                    Step = 0.1,
                    Value = Mathf.Clamp(angles[i], lowerLimit, upperLimit),
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
                };
                _jointSliders[index] = slider;

                slider.ValueChanged += value =>
                {
                    if (_updatingJointControls) return;
                    _component.targetJointAngles[index] = (float)value;
                    valueField.Value = value;
                };

                valueField.ValueChanged += value =>
                {
                    if (_updatingJointControls) return;
                    _component.targetJointAngles[index] = (float)value;
                    slider.Value = value;
                };

                header.AddChild(nameLabel);
                header.AddChild(actualAngle);
                header.AddChild(valueField);

                row.AddChild(header);
                row.AddChild(slider);

                section.AddChild(row);
            }

            return section;
        }

        /// <summary>Обновляет фактические показания углов и окрашивает ось при выходе за заводские пределы.</summary>
        private void UpdateJointActualReadouts()
        {
            if (!GodotObject.IsInstanceValid(_component)) return;
            _updatingJointControls = true;
            try
            {
                for (int joint = 0; joint < 6; joint++)
                {
                    (float lower, float upper) = _component.GetJointAngleLimits(joint);
                    float actual = _component.currentJointAngles[joint];
                    float target = _component.targetJointAngles[joint];
                    _actualJointAngleLabels[joint].Text = $"Факт: {actual:0.0}°";
                    if (actual < lower || actual > upper)
                        _actualJointAngleLabels[joint].AddThemeColorOverride("font_color", Colors.Red);
                    else
                        _actualJointAngleLabels[joint].RemoveThemeColorOverride("font_color");

                    _jointSliders[joint].MinValue = lower;
                    _jointSliders[joint].MaxValue = upper;
                    _jointTargetFields[joint].MinValue = lower;
                    _jointTargetFields[joint].MaxValue = upper;
                    float clampedTarget = Mathf.Clamp(target, lower, upper);
                    _jointSliders[joint].Value = clampedTarget;
                    _jointTargetFields[joint].Value = clampedTarget;
                }

                if (_ikConfigurationSelector.Selected != _component.CurrentIkConfiguration)
                    _ikConfigurationSelector.Select(System.Math.Clamp(_component.CurrentIkConfiguration, 0, 7));
            }
            finally
            {
                _updatingJointControls = false;
            }
        }

        private void RefreshJoints()
        {
            ClearContainer(_jointsContainer);

            var joints = _component.jointsTransform;

            if (joints == null || joints.Length == 0)
            {
                _jointsContainer.AddChild(new Label
                {
                    Text = "Суставы не назначены",
                    Modulate = new Color(0.6f, 0.6f, 0.6f)
                });

                return;
            }

            for (int i = 0; i < joints.Length; i++)
            {
                int index = i;
                Node3D joint = joints[i];

                var row = new HBoxContainer
                {
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
                };

                row.AddChild(new Label
                {
                    Text = $"{i + 1}. {GetNodeDisplayName(joint)}",
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                    ClipText = true
                });

                var removeButton = new Button
                {
                    Text = "×",
                    TooltipText = "Удалить сустав"
                };

                removeButton.Pressed += () => RemoveJoint(index);

                row.AddChild(removeButton);
                _jointsContainer.AddChild(row);
            }
        }

        private void MarkChanged()
        {
            _component.NotifyPropertyListChanged();
            EditorInterface.Singleton.MarkSceneAsUnsaved();
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
    }
}
#endif
