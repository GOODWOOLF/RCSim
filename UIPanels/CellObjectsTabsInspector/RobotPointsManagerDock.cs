
#if TOOLS
using Godot;
using System;
using System.Linq;
using RoboSim.RobotObject;
using RoboSim.RobotObject.RobotComponents;

public partial class RobotPointsManagerDock : VBoxContainer
{
    private readonly EditorPlugin _plugin;
    private readonly RobotPointsManagerComponent _component;

    private VBoxContainer _pointsContainer;
    private LineEdit _pointNameInput;
    private OptionButton _ikConfigurationSelector;

    public RobotPointsManagerDock(
        EditorPlugin plugin,
        RobotPointsManagerComponent component)
    {
        _plugin = plugin;
        _component = component;

        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;

        Build();
        Refresh();
    }

    private void Build()
    {
        var title = new Label
        {
            Text = "Точки робота",
            HorizontalAlignment = HorizontalAlignment.Center
        };

        AddChild(title);
        AddChild(new HSeparator());

        AddChild(new Label { Text = "Текущая конфигурация IK" });
        _ikConfigurationSelector = new OptionButton();
        for (int configuration = 0; configuration < 8; configuration++)
            _ikConfigurationSelector.AddItem($"Конфигурация {configuration + 1}", configuration);
        _ikConfigurationSelector.Selected = Math.Clamp(_component.CurrentIkConfiguration, 0, 7);
        _ikConfigurationSelector.ItemSelected += index =>
        {
            if (!_component.TrySetCurrentIkConfiguration((int)index, out string error))
            {
                GD.PushError(error);
                _ikConfigurationSelector.Selected = Math.Clamp(_component.CurrentIkConfiguration, 0, 7);
                return;
            }
            MarkChanged();
        };
        AddChild(_ikConfigurationSelector);
        AddChild(new HSeparator());

        // Создание точки
        var createSection = new VBoxContainer();

        createSection.AddChild(new Label
        {
            Text = "Создание точки"
        });

        _pointNameInput = new LineEdit
        {
            PlaceholderText = "Название точки",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };

        createSection.AddChild(_pointNameInput);

        var createButton = new Button
        {
            Text = "Сохранить текущую позицию"
        };

        createButton.Pressed += CreatePoint;
        createSection.AddChild(createButton);

        AddChild(createSection);
        AddChild(new HSeparator());

        // Заголовок списка
        AddChild(new Label
        {
            Text = "Сохранённые точки"
        });

        // Прокручиваемый список
        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };

        _pointsContainer = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };

        scroll.AddChild(_pointsContainer);
        AddChild(scroll);
    }

    private void CreatePoint()
    {
        string pointName = _pointNameInput.Text.Trim();

        if (string.IsNullOrEmpty(pointName))
            pointName = $"Точка {_component._wayPoints.Count + 1}";

        _component.CreatePoint(pointName);

        _pointNameInput.Clear();

        MarkChanged();
        Refresh();
    }

    private void DeletePoint(WayPoint point)
    {
        _component.DeleteWayPoint(point);

        MarkChanged();
        Refresh();
    }

    private void GoToPoint(WayPoint point)
    {
        _component.GoToWayPointPTP(point);
    }

    private void Refresh()
    {
        ClearContainer(_pointsContainer);

        foreach (WayPoint point in _component._wayPoints)
        {
            CreatePointEntry(point);
        }
    }

    private void CreatePointEntry(WayPoint point)
    {
        var pointContainer = new VBoxContainer();

        var header = new HBoxContainer();

        var foldoutButton = new Button
        {
            Text = "▶",
            CustomMinimumSize = new Vector2(24, 0),
            Flat = true
        };

        var nameLabel = new Label
        {
            Text = point.Name,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Stop
        };

        var linearButton = new Button { Text = "Линейное" };
        var ptpButton = new Button { Text = "P2P" };
        var visibilityButton = new Button
        {
            Icon = EditorInterface.Singleton.GetEditorTheme().GetIcon(
            point.Visible ? "GuiVisibilityVisible" : "GuiVisibilityHidden",
            "EditorIcons"
        ),
            TooltipText = point.Visible ? "Скрыть точку" : "Показать точку",
            Flat = true,
            CustomMinimumSize = new Vector2(28, 28)
        };
        var updateButton = new Button
        {
            Text = "Обновить",
            TooltipText = "Обновить точку по текущему положению робота"
        };

        var deleteButton = new Button { Text = "Удалить" };

        header.AddChild(foldoutButton);
        header.AddChild(nameLabel);
        header.AddChild(visibilityButton);
        header.AddChild(linearButton);
        header.AddChild(ptpButton);
        header.AddChild(updateButton);
        header.AddChild(deleteButton);

        var details = new VBoxContainer
        {
            Visible = false
        };

        details.AddThemeConstantOverride("separation", 5);

        var detailsContent = new VBoxContainer();
        detailsContent.AddThemeConstantOverride("separation", 5);

        details.AddChild(detailsContent);

        var positionTitle = new Label { Text = "Позиция" };
        detailsContent.AddChild(positionTitle);

        var positionRow = new HBoxContainer();
        positionRow.AddThemeConstantOverride("separation", 5);

        positionRow.AddChild(CreateValueCard("X", $"{point.Position.X:F3}"));
        positionRow.AddChild(CreateValueCard("Y", $"{point.Position.Y:F3}"));
        positionRow.AddChild(CreateValueCard("Z", $"{point.Position.Z:F3}"));

        detailsContent.AddChild(positionRow);

        var rotationTitle = new Label { Text = "Вращение" };
        detailsContent.AddChild(rotationTitle);

        var rotationRow = new HBoxContainer();
        rotationRow.AddThemeConstantOverride("separation", 5);

        Vector3 rotation = point.RotationDegrees;

        rotationRow.AddChild(CreateValueCard("X", $"{rotation.X:F2}°"));
        rotationRow.AddChild(CreateValueCard("Y", $"{rotation.Y:F2}°"));
        rotationRow.AddChild(CreateValueCard("Z", $"{rotation.Z:F2}°"));

        detailsContent.AddChild(rotationRow);
        var anglesTitle = new Label { Text = "Углы суставов" };
        detailsContent.AddChild(anglesTitle);

        var anglesGrid = new GridContainer { Columns = 3 };
        anglesGrid.AddThemeConstantOverride("h_separation", 5);
        anglesGrid.AddThemeConstantOverride("v_separation", 5);

        for (int i = 0; i < point.Angles.Length; i++)
        {
            anglesGrid.AddChild(
                CreateValueCard($"J{i + 1}", $"{point.Angles[i]:F2}°")
            );
        }

        detailsContent.AddChild(anglesGrid);

        void ToggleDetails()
        {
            details.Visible = !details.Visible;
            foldoutButton.Text = details.Visible ? "▼" : "▶";
        }

        foldoutButton.Pressed += ToggleDetails;

        nameLabel.GuiInput += @event =>
        {
            if (@event is InputEventMouseButton mouseEvent &&
                mouseEvent.Pressed &&
                mouseEvent.ButtonIndex == MouseButton.Left)
            {
                ToggleDetails();
            }
        };

        linearButton.Pressed += () => _component.GoToWayPointLinear(point);
        ptpButton.Pressed += () => _component.GoToWayPointPTP(point);
        deleteButton.Pressed += () => DeletePoint(point);
        visibilityButton.Pressed += () =>
        {
            bool visible = !point.Visible;

            point.UpdateVisibility(visible);

            visibilityButton.Icon = EditorInterface.Singleton.GetEditorTheme().GetIcon(
                visible ? "GuiVisibilityVisible" : "GuiVisibilityHidden",
                "EditorIcons"
            );

            visibilityButton.TooltipText = visible
                ? "Скрыть точку"
                : "Показать точку";

            MarkChanged();
        };
        updateButton.Pressed += () =>
        {
            point.UpdatePoint();
            MarkChanged();
            Refresh();
        };

  

        pointContainer.AddChild(header);
        pointContainer.AddChild(details);
        if (_pointsContainer.GetChildCount() > 0)
        {
            var separator = new HSeparator();
            separator.AddThemeConstantOverride("separation", 4);
            separator.Modulate = new Color(0.35f, 0.35f, 0.35f);
            _pointsContainer.AddChild(separator);
        }

        _pointsContainer.AddChild(pointContainer);
    }

    private Control CreateValueCard(string title, string value)
    {
        var card = new PanelContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.11f, 0.11f, 0.11f),
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            ContentMarginLeft = 5,
            ContentMarginRight = 5,
            ContentMarginTop = 3,
            ContentMarginBottom = 3
        };

        card.AddThemeStyleboxOverride("panel", style);

        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 1);

        var titleLabel = new Label
        {
            Text = title,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        titleLabel.AddThemeFontSizeOverride("font_size", 13);
        titleLabel.AddThemeColorOverride(
            "font_color",
            new Color(0.7f, 0.7f, 0.7f)
        );

        var valueLabel = new Label
        {
            Text = value,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        valueLabel.AddThemeFontSizeOverride("font_size", 12);

        content.AddChild(titleLabel);
        content.AddChild(valueLabel);

        card.AddChild(content);

        return card;
    }
    private void CreatePointRow(WayPoint point, int index)
    {
        var card = new VBoxContainer();

        card.AddThemeConstantOverride("separation", 4);

        // Название точки
        card.AddChild(new Label
        {
            Text = $"{index + 1}. {point.Name}"
        });

        // Положение
        Vector3 position = point.Position;

        card.AddChild(new Label
        {
            Text = $"X: {position.X:F3}    Y: {position.Y:F3}    Z: {position.Z:F3}"
        });

        // Углы осей
        string anglesText = point.Angles == null
            ? "Углы не заданы"
            : string.Join("   ",
                point.Angles.Select((angle, i) => $"J{i + 1}: {angle:F2}°"));

        card.AddChild(new Label
        {
            Text = anglesText
        });

        // Кнопки действий
        var buttons = new HBoxContainer();

        var goButton = new Button
        {
            Text = "Перейти к точке",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };

        goButton.Pressed += () => GoToPoint(point);

        var deleteButton = new Button
        {
            Text = "Удалить"
        };

        deleteButton.Pressed += () => DeletePoint(point);

        buttons.AddChild(goButton);
        buttons.AddChild(deleteButton);

        card.AddChild(buttons);

        _pointsContainer.AddChild(card);
        _pointsContainer.AddChild(new HSeparator());
    }

    private void MarkChanged()
    {
        _plugin.GetEditorInterface().MarkSceneAsUnsaved();
    }

    private static void ClearContainer(Container container)
    {
        foreach (Node child in container.GetChildren())
        {
            container.RemoveChild(child);
            child.QueueFree();
        }
    }
}
#endif
