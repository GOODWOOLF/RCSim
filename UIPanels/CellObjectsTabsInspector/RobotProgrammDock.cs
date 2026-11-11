#if TOOLS
using Godot;
using RoboSim.Global.Components.IOComponents;
using RoboSim.RobotObject;
using RoboSim.RobotObject.RobotComponents;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace RoboSim.EditorPluginTools.Docks;

[Tool]
public partial class RobotProgrammDock : VBoxContainer
{
    private readonly EditorPlugin _plugin;
    private readonly RobotProgrammComponent _component;
    private LineEdit _newVariableName;
    private OptionButton _newVariableType;
    private VBoxContainer _variablesList;
    private VBoxContainer _layoutRoot;
    private ProgramTree _tree;
    private VBoxContainer _properties;
    private RichTextLabel _log;
    private Label _status;
    private Label _currentNode;
    private PopupMenu _addMenu;
    private Button _createProgramButton;
    private NodeBase _selectedNode;
    private int _selectedBranchIndex;
    private string _selectedScope = "main";
    private IReadOnlyList<ProgramCommandDescriptor> _loopCommands;
    private IReadOnlyList<ProgramCommandDescriptor> _initializationCommands;

    public RobotProgrammDock(EditorPlugin plugin, RobotProgrammComponent component)
    {
        _plugin = plugin;
        _component = component;
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;
        CustomMinimumSize = new Vector2(360, 0);
        Build();
        RefreshVariables();
        RefreshTree();
        RefreshRuntime();
    }

    private void Build()
    {
        var pageScroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto
        };
        _layoutRoot = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.Fill
        };
        pageScroll.AddChild(_layoutRoot);
        AddChild(pageScroll);

        var runRow = new HBoxContainer();
        var start = new Button { Text = "▶ Старт" };
        start.Pressed += _component.StartProgram;
        var stop = new Button { Text = "■ Стоп" };
        stop.Pressed += _component.StopProgram;
        _status = new Label { Text = "Статус: остановлена", SizeFlagsHorizontal = SizeFlags.ExpandFill, VerticalAlignment = VerticalAlignment.Center };
        runRow.AddChild(start);
        runRow.AddChild(stop);
        runRow.AddChild(_status);
        _layoutRoot.AddChild(runRow);

        _createProgramButton = new Button { Text = "+ Создать программу" };
        _createProgramButton.Pressed += CreateProgram;
        _createProgramButton.Visible = _component.Program == null;
        _layoutRoot.AddChild(_createProgramButton);

        _currentNode = new Label { Text = "Текущая команда: —", AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _layoutRoot.AddChild(_currentNode);
        _layoutRoot.AddChild(new HSeparator());

        _layoutRoot.AddChild(new Label { Text = "Переменные", ThemeTypeVariation = "HeaderSmall" });
        var addVariableRow = new HBoxContainer();
        _newVariableName = new LineEdit { PlaceholderText = "Имя новой переменной", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _newVariableType = new OptionButton();
        _newVariableType.AddItem("Число", 0);
        _newVariableType.AddItem("Логическая", 1);
        var addVariable = new Button { Text = "+ Добавить" };
        addVariable.Pressed += AddVariable;
        addVariableRow.AddChild(_newVariableName);
        addVariableRow.AddChild(_newVariableType);
        addVariableRow.AddChild(addVariable);
        _layoutRoot.AddChild(addVariableRow);

        var variableScroll = new ScrollContainer { CustomMinimumSize = new Vector2(0, 145), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _variablesList = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        variableScroll.AddChild(_variablesList);
        _layoutRoot.AddChild(variableScroll);
        _layoutRoot.AddChild(new HSeparator());

        _layoutRoot.AddChild(new Label { Text = "Дерево программы", ThemeTypeVariation = "HeaderSmall" });
        var treeButtons = new HBoxContainer();
        var addLoopButton = new MenuButton { Text = "+ Команда сюда" };
        _addMenu = addLoopButton.GetPopup();
        _loopCommands = ProgramCommandRegistry.Get(ProgramCommandScope.Loop);
        for (int i = 0; i < _loopCommands.Count; i++) _addMenu.AddItem(_loopCommands[i].Title, i);
        _addMenu.IdPressed += AddProgramNode;
        var addInitButton = new MenuButton { Text = "+ Команда при запуске" };
        var addInitMenu = addInitButton.GetPopup();
        _initializationCommands = ProgramCommandRegistry.Get(ProgramCommandScope.Initialization);
        for (int i = 0; i < _initializationCommands.Count; i++) addInitMenu.AddItem(_initializationCommands[i].Title, i);
        addInitMenu.IdPressed += AddInitializationNode;
        var delete = new Button { Text = "Удалить выбранное" };
        delete.Pressed += DeleteSelected;
        treeButtons.AddChild(addLoopButton);
        treeButtons.AddChild(addInitButton);
        treeButtons.AddChild(delete);
        _layoutRoot.AddChild(treeButtons);

        _tree = new ProgramTree { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 180) };
        _tree.ReorderRequested += ReorderNode;
        _tree.ItemSelected += OnTreeSelectionChanged;
        _layoutRoot.AddChild(_tree);
        _properties = new VBoxContainer();
        _layoutRoot.AddChild(_properties);
        _layoutRoot.AddChild(new HSeparator());
        _layoutRoot.AddChild(new Label { Text = "Журнал выполнения", ThemeTypeVariation = "HeaderSmall" });
        _log = new RichTextLabel { CustomMinimumSize = new Vector2(0, 90), ScrollFollowing = true, FitContent = false };
        _layoutRoot.AddChild(_log);

        var timer = new Timer { WaitTime = 0.25, Autostart = true };
        timer.Timeout += RefreshRuntime;
        _layoutRoot.AddChild(timer);
    }

    private void AddVariable()
    {
        string name = _newVariableName.Text.Trim();
        if (string.IsNullOrEmpty(name)) name = $"Переменная {_component.Variables.Variables.Count + 1}";
        if (_newVariableType.Selected == 0) _component.Variables.AddNumber(name);
        else _component.Variables.AddBool(name);
        _newVariableName.Clear();
        RefreshVariables();
        MarkChanged();
    }

    private void CreateProgram()
    {
        if (_component.Program != null) return;
        _component.Program = new Programm();
        _component.Program.Initialization = new InitializeList();
        _component.Program.MainNode = new MainNode();
        _createProgramButton.Visible = false;
        RefreshTree();
        MarkChanged();
    }

    private void RefreshVariables()
    {
        Clear(_variablesList);
        if (_component.Variables == null) return;
        for (int i = 0; i < _component.Variables.Variables.Count; i++)
        {
            int index = i;
            var variable = _component.Variables.Variables[i];
            if (variable == null) continue;
            var row = new HBoxContainer();
            var name = new LineEdit { Text = variable.Name, CustomMinimumSize = new Vector2(95, 0), SizeFlagsHorizontal = SizeFlags.ExpandFill };
            name.TextChanged += value => { variable.Name = value; MarkChanged(); };
            row.AddChild(name);
            row.AddChild(new Label { Text = variable is NumberVariable ? "Число" : "Логическая", CustomMinimumSize = new Vector2(65, 0) });
            if (variable is NumberVariable number)
            {
                var value = new SpinBox { MinValue = -1000000, MaxValue = 1000000, Step = 0.1, Value = number.Value, SizeFlagsHorizontal = SizeFlags.ExpandFill };
                value.ValueChanged += v => { number.Value = (float)v; MarkChanged(); };
                row.AddChild(value);
            }
            else if (variable is BoolVariable boolean)
            {
                var value = new CheckBox { ButtonPressed = boolean.Value, Text = boolean.Value ? "true" : "false" };
                value.Toggled += v => { boolean.Value = v; value.Text = v ? "true" : "false"; MarkChanged(); };
                row.AddChild(value);
            }
            var remove = new Button { Text = "×", TooltipText = "Удалить переменную" };
            remove.Pressed += () => { _component.Variables.Variables.RemoveAt(index); RefreshVariables(); MarkChanged(); };
            row.AddChild(remove);
            _variablesList.AddChild(row);
        }
    }

    private void AddInitializationNode(long id)
    {
        EnsureProgram();
        if (id < 0 || id >= _initializationCommands.Count) return;
        if (_initializationCommands[(int)id].Create() is not VariableAssignmentNode node) return;
        _component.Program.Initialization.InitList.Add(node);
        RefreshTree(); MarkChanged();
    }

    private void AddProgramNode(long id)
    {
        EnsureProgram();
        if (id < 0 || id >= _loopCommands.Count) return;
        if (!AddNodeToScope(_selectedScope, _loopCommands[(int)id].Create()))
        { _selectedScope = "main"; AddNodeToScope(_selectedScope, _loopCommands[(int)id].Create()); }
        RefreshTree(); MarkChanged();
    }

    private void EnsureProgram()
    {
        _component.Program ??= new Programm();
        _component.Program.Initialization ??= new InitializeList();
        _component.Program.MainNode ??= new MainNode();
        if (_createProgramButton != null) _createProgramButton.Visible = false;
    }

    private void RefreshTree()
    {
        if (_tree == null) return;
        _tree.Clear();
        var root = _tree.CreateItem();
        var initRoot = _tree.CreateItem(root);
        initRoot.SetText(0, "При запуске");
        initRoot.SetMetadata(0, Variant.From(ProgramTree.MakeScopeKey("init")));
        if (_component.Program != null && _component.Program.Initialization != null)
            for (int i = 0; i < _component.Program.Initialization.InitList.Count; i++)
            {
                var node = _component.Program.Initialization.InitList[i];
                var item = _tree.CreateItem(initRoot);
                item.SetText(0, Describe(node));
                item.SetMetadata(0, Variant.From(ProgramTree.MakeKey("init", i)));
            }
        var loopRoot = _tree.CreateItem(root);
        loopRoot.SetText(0, "Цикл программы");
        loopRoot.SetMetadata(0, Variant.From(ProgramTree.MakeScopeKey("main")));
        if (_component.Program?.MainNode != null) AddTreeItems(loopRoot, _component.Program.MainNode.Children, "main");
        initRoot.Collapsed = false;
        loopRoot.Collapsed = false;
    }

    private void AddTreeItems(TreeItem parent, Godot.Collections.Array<NodeBase> nodes, string scope)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            var item = _tree.CreateItem(parent);
            item.SetText(0, Describe(node));
            item.SetMetadata(0, Variant.From(ProgramTree.MakeKey(scope, i)));
            item.Collapsed = false;
            if (node is not IfElseNode conditional) continue;

            for (int branchIndex = 0; branchIndex < conditional.Branches.Count; branchIndex++)
            {
                int branchNumber = branchIndex + 1;
                var branch = conditional.Branches[branchIndex];
                string actionScope = $"{scope}/{i}/branch/{branchIndex}/action";
                var branchItem = _tree.CreateItem(item);
                string branchLabel = string.IsNullOrWhiteSpace(branch?.BranchName) ? $"Ветка {branchNumber}" : branch.BranchName;
                branchItem.SetText(0, $"{branchLabel}: {branch?.Expression ?? "условие не задано"}");
                branchItem.SetMetadata(0, Variant.From(ProgramTree.MakeScopeKey(actionScope)));
                branchItem.Collapsed = false;
                if (branch != null) AddTreeItems(branchItem, branch.Actions, actionScope);
            }

            string elseScope = $"{scope}/{i}/else";
            var elseItem = _tree.CreateItem(item);
            elseItem.SetText(0, "ИНАЧЕ");
            elseItem.SetMetadata(0, Variant.From(ProgramTree.MakeScopeKey(elseScope)));
            elseItem.Collapsed = false;
            AddTreeItems(elseItem, conditional.ElseNodes, elseScope);
        }
    }

    private static string Describe(NodeBase node)
    {
        if (node == null) return "<пустая команда>";
        if (!string.IsNullOrWhiteSpace(node.DisplayName)) return node.DisplayName;
        string title = node.GetType().GetCustomAttribute<ProgramCommandAttribute>()?.Title;
        if (!string.IsNullOrEmpty(title)) return title;
        return node switch
        {
            PTPNode => "PTP",
            LINNode lin => $"LIN ({lin.SpeedMetersPerSecond:0.###} м/с)",
            SetVariableNumber n => $"Задать число: {n.Variable?.Name ?? "?"}",
            SetVariableBool b => $"Задать bool: {b.Variable?.Name ?? "?"}",
            SetDigitalOutputNode => "Записать выход",
            WaitDigitalInputNode => "Ждать вход",
            IfElseNode => "Если / иначе",
            _ => node.GetType().Name
        };
    }

    private void DeleteSelected()
    {
        var item = _tree.GetSelected();
        if (item == null || _component.Program == null || !ProgramTree.TryParseKey(item.GetMetadata(0).AsString(), out string scope, out int index)) return;
        RemoveNodeAtScope(scope, index);
        RefreshTree(); Clear(_properties); MarkChanged();
    }

    private void ReorderNode(string sourceScope, int from, string targetScope, int to)
    {
        if (sourceScope == "init")
        {
            int initCount = ScopeCount("init");
            if (targetScope != "init" || from < 0 || from >= initCount || to < -1 || to > initCount) return;
            var initNode = GetNodeAtScope("init", from);
            _component.Program.Initialization.InitList.RemoveAt(from);
            int initInsertAt = to < 0 ? _component.Program.Initialization.InitList.Count : to;
            if (from < initInsertAt) initInsertAt--;
            _component.Program.Initialization.InitList.Insert(Math.Clamp(initInsertAt, 0, _component.Program.Initialization.InitList.Count), (VariableAssignmentNode)initNode);
            RefreshTree(); MarkChanged();
            return;
        }
        var sourceList = GetNodeList(sourceScope);
        var targetList = GetNodeList(targetScope);
        if (sourceList == null || targetList == null || from < 0 || from >= sourceList.Count) return;
        if (targetScope == "init") return;
        if (to < -1 || to > targetList.Count) return;
        var node = sourceList[from];
        if (node is IfElseNode && targetScope.StartsWith($"{sourceScope}/{from}/", StringComparison.Ordinal)) return;
        int insertAt = to < 0 ? targetList.Count : to;
        sourceList.RemoveAt(from);
        if (ReferenceEquals(sourceList, targetList) && from < insertAt) insertAt--;
        insertAt = Math.Clamp(insertAt, 0, targetList.Count);
        targetList.Insert(insertAt, node);
        RefreshTree(); MarkChanged();
    }

    private void OnTreeSelectionChanged()
    {
        Clear(_properties);
        var item = _tree.GetSelected();
        if (item == null || _component.Program == null) { _selectedNode = null; _selectedScope = "main"; return; }
        string metadata = item.GetMetadata(0).AsString();
        if (metadata.StartsWith("scope|", StringComparison.Ordinal))
        {
            _selectedScope = metadata.Substring("scope|".Length);
            _selectedNode = null;
            return;
        }
        if (!ProgramTree.TryParseKey(metadata, out string scope, out int index)) { _selectedNode = null; return; }
        _selectedScope = scope;
        _selectedNode = GetNodeAtScope(scope, index);
        if (_selectedNode != null) BuildProperties(_selectedNode);
    }

    private bool AddNodeToScope(string scope, NodeBase node)
    {
        if (scope == "init")
        {
            if (node is not VariableAssignmentNode assignment) return false;
            _component.Program.Initialization.InitList.Add(assignment);
            return true;
        }
        var list = GetNodeList(scope);
        if (list == null) return false;
        list.Add(node);
        return true;
    }

    private Godot.Collections.Array<NodeBase> GetNodeList(string scope)
    {
        if (_component.Program == null) return null;
        if (scope == "main") return _component.Program.MainNode?.Children;
        if (!scope.StartsWith("main/", StringComparison.Ordinal)) return null;
        string[] parts = scope.Split('/');
        var list = _component.Program.MainNode?.Children;
        int cursor = 1;
        while (list != null && cursor < parts.Length)
        {
            if (!int.TryParse(parts[cursor++], out int commandIndex) || commandIndex < 0 || commandIndex >= list.Count || list[commandIndex] is not IfElseNode conditional)
                return null;
            if (cursor >= parts.Length) return null;
            string section = parts[cursor++];
            if (section == "else")
            {
                list = conditional.ElseNodes;
                continue;
            }
            if (section != "branch" || cursor >= parts.Length || !int.TryParse(parts[cursor++], out int branchIndex) || branchIndex < 0 || branchIndex >= conditional.Branches.Count || cursor >= parts.Length || parts[cursor++] != "action")
                return null;
            list = conditional.Branches[branchIndex]?.Actions;
        }
        return cursor == parts.Length ? list : null;
    }

    private int ScopeCount(string scope)
        => scope == "init" ? _component.Program?.Initialization?.InitList.Count ?? 0 : GetNodeList(scope)?.Count ?? 0;
    private NodeBase GetNodeAtScope(string scope, int index)
        => scope == "init" ? _component.Program?.Initialization?.InitList[index] : GetNodeList(scope)?[index];
    private void RemoveNodeAtScope(string scope, int index)
    {
        if (scope == "init") _component.Program.Initialization.InitList.RemoveAt(index);
        else GetNodeList(scope)?.RemoveAt(index);
    }
    private void InsertNodeAtScope(string scope, int index, NodeBase node)
    {
        if (scope == "init" && node is VariableAssignmentNode assignment) _component.Program.Initialization.InitList.Insert(index, assignment);
        else GetNodeList(scope)?.Insert(index, node);
    }

    private void BuildProperties(NodeBase node)
    {
        _properties.AddChild(new HSeparator());
        _properties.AddChild(new Label { Text = $"Параметры: {Describe(node)}", ThemeTypeVariation = "HeaderSmall" });
        var executeCommand = new Button { Text = "▶ Выполнить эту команду" };
        executeCommand.Pressed += async () =>
        {
            await _component.ExecuteCommandAsync(node);
            RefreshRuntime();
        };
        _properties.AddChild(executeCommand);
        var displayName = new LineEdit { PlaceholderText = "Подпись команды", Text = node.DisplayName };
        displayName.TextChanged += value => { node.DisplayName = value; MarkChanged(); RefreshTree(); };
        _properties.AddChild(displayName);
        // Параметры команды строятся по экспортированным свойствам, без switch по типу.
        foreach (var property in node.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            if (property.GetCustomAttribute<ExportAttribute>() != null && property.Name is not ("Id" or "DisplayName" or "Branches" or "ElseNodes"))
                AddPropertyEditor(node, property);
        if (node is PTPNode or LINNode) AddCurrentPositionPointButton(node);
        if (node is IfElseNode ifElse) BuildIfElseProperties(ifElse);
    }

    private void AddPropertyEditor(NodeBase node, PropertyInfo property)
    {
        Type type = property.PropertyType;
        object current = property.GetValue(node);
        string label = PropertyLabel(property.Name);
        _properties.AddChild(new Label { Text = label });

        if (type == typeof(NodePath) && property.Name == "PointPath")
        {
            AddPointSelector((NodePath)current, path => SetProperty(node, property, path));
            return;
        }
        if (type == typeof(NumberVariable))
        {
            AddVariableSelector<NumberVariable>(current as NumberVariable, value => SetProperty(node, property, value));
            return;
        }
        if (type == typeof(BoolVariable))
        {
            AddVariableSelector<BoolVariable>(current as BoolVariable, value => SetProperty(node, property, value));
            return;
        }
        if (type == typeof(IOChannel))
        {
            bool input = node is WaitDigitalInputNode;
            AddChannelSelector(current as IOChannel, input, value => SetProperty(node, property, value));
            return;
        }
        if (property.Name == "ToolIndex" && type == typeof(int))
        {
            AddToolSelector((int)current, value => { SetProperty(node, property, value); MarkChanged(); });
            return;
        }
        if (property.Name == "IKConfiguration" && type == typeof(int))
        {
            AddIKConfigurationSelector((int)current, value => { SetProperty(node, property, value); MarkChanged(); });
            return;
        }
        if (type == typeof(bool))
        {
            var control = new CheckBox { ButtonPressed = current is true, Text = label };
            control.Toggled += value => { SetProperty(node, property, value); MarkChanged(); };
            _properties.AddChild(control);
            return;
        }
        if (type.IsEnum)
        {
            var control = new OptionButton();
            foreach (object value in Enum.GetValues(type)) control.AddItem(EnumLabel(value.ToString()), Convert.ToInt32(value));
            control.Select(Convert.ToInt32(current));
            control.ItemSelected += index => { SetProperty(node, property, Enum.ToObject(type, index)); MarkChanged(); RefreshTree(); };
            _properties.AddChild(control);
            return;
        }
        if (type == typeof(string))
        {
            var control = new LineEdit { Text = current as string ?? "" };
            control.TextChanged += value => { SetProperty(node, property, value); MarkChanged(); };
            _properties.AddChild(control);
            return;
        }
        if (type == typeof(float) || type == typeof(double) || type == typeof(int))
        {
            double value = Convert.ToDouble(current ?? 0);
            var control = new SpinBox { MinValue = -1000000, MaxValue = 1000000, Step = property.Name.Contains("Speed", StringComparison.OrdinalIgnoreCase) ? 0.01 : 0.1, Value = value };
            if (property.Name == "SpeedMetersPerSecond") control.Suffix = " м/с";
            control.ValueChanged += next =>
            {
                object converted = type == typeof(float) ? (object)(float)next : type == typeof(int) ? (object)(int)next : next;
                SetProperty(node, property, converted); MarkChanged(); RefreshTree();
            };
            _properties.AddChild(control);
        }
    }

    private void SetProperty(NodeBase node, PropertyInfo property, object value) => property.SetValue(node, value);

    private void AddToolSelector(int selectedIndex, Action<int> setter)
    {
        var option = new OptionButton();
        option.AddItem("Текущий выбранный инструмент", -1);
        var tools = _component.RobotTools?.EndEffectorOffset;
        if (tools != null)
            for (int i = 0; i < tools.Count; i++) option.AddItem(tools[i]?.ToolName ?? $"Инструмент {i + 1}", i);
        option.ItemSelected += index => { setter((int)index == 0 ? -1 : (int)index - 1); RefreshTree(); };
        int selected = selectedIndex < 0 ? 0 : Math.Clamp(selectedIndex + 1, 0, option.ItemCount - 1);
        option.Select(selected);
        _properties.AddChild(option);
    }

    private void AddIKConfigurationSelector(int selected, Action<int> setter)
    {
        var option = new OptionButton();
        option.AddItem("— не выбрана —", -1);
        for (int i = 0; i < 8; i++) option.AddItem($"Конфигурация {i + 1}", i);
        option.ItemSelected += index => { setter((int)index == 0 ? -1 : (int)index - 1); RefreshTree(); };
        option.Select(selected < 0 ? 0 : Math.Clamp(selected + 1, 0, 8));
        _properties.AddChild(option);
    }

    /// <summary>Добавляет точку текущей TCP-позы и сохраняет текущую IK-конфигурацию LIN-команды.</summary>
    private void AddCurrentPositionPointButton(NodeBase node)
    {
        var create = new Button { Text = "Создать точку по текущей позиции робота" };
        create.Pressed += () =>
        {
            int count = _component.GetAvailablePoints().Count;
            int? linearConfiguration = null;
            RobotPose linearTool = null;
            if (node is LINNode linearNode)
            {
                try
                {
                    var controller = _component.MotionController;
                    var tools = _component.RobotTools;
                    if (controller == null || tools == null)
                        throw new InvalidOperationException("Контроллер робота или менеджер инструментов не найден.");
                    linearTool = linearNode.ToolIndex < 0
                        ? tools.CurrentTool
                        : linearNode.ToolIndex < tools.EndEffectorOffset.Count
                            ? tools.EndEffectorOffset[linearNode.ToolIndex]
                            : throw new InvalidOperationException($"Инструмент {linearNode.ToolIndex + 1} не найден.");
                    linearConfiguration = controller.CurrentIkConfiguration;
                }
                catch (InvalidOperationException error)
                {
                    GD.PushError($"Не удалось определить конфигурацию IK для LIN-точки: {error.Message}");
                    return;
                }
            }
            WayPoint point = _component.CreateWaypointAtCurrentPosition($"Точка {count + 1}");
            if (point == null) return;
            if (linearConfiguration.HasValue)
            {
                var controller = _component.MotionController;
                point.SetPoint(controller.GetCurrentTcpPose(linearTool), controller.currentJointAngles);
                ((LINNode)node).IKConfiguration = linearConfiguration.Value;
            }
            PropertyInfo property = node.GetType().GetProperty("PointPath");
            property?.SetValue(node, _component.GetPathTo(point));
            MarkChanged();
            RefreshTree();
            Clear(_properties);
            BuildProperties(node);
        };
        _properties.AddChild(create);
    }

    private static string PropertyLabel(string name) => name switch
    {
        "PointPath" => "Точка движения", "SpeedPercent" => "Скорость, %", "SpeedMetersPerSecond" => "Скорость LIN",
        "ToolIndex" => "Инструмент", "IKConfiguration" => "Конфигурация IK",
        "Variable" => "Переменная", "Source" => "Переменная-источник", "Operation" => "Операция", "Value" => "Значение",
        "Channel" => "Канал", "ExpectedValue" => "Ожидаемое значение", _ => name
    };

    private static string EnumLabel(string value) => value switch
    {
        "VariableToVariable" => "Скопировать переменную", "VariableToValue" => "Задать значение",
        "Increment" => "Прибавить", "Decrement" => "Вычесть", "Toggle" => "Переключить",
        "AndWithValue" => "И со значением", "OrWithValue" => "ИЛИ со значением", "XorWithValue" => "Исключающее ИЛИ со значением",
        "AndWithVariable" => "И с переменной", "OrWithVariable" => "ИЛИ с переменной", "XorWithVariable" => "Исключающее ИЛИ с переменной",
        _ => value
    };

    private void BuildIfElseProperties(IfElseNode node)
    {
        var addBranch = new Button { Text = "+ Добавить ветку условия" };
        addBranch.Pressed += () =>
        {
            var branch = new ConditionBranch
            {
                Expression = "",
                BranchName = node.Branches.Count == 0 ? "Если" : $"Иначе если {node.Branches.Count + 1}"
            };
            OpenConditionBuilder(branch, node);
        };
        _properties.AddChild(addBranch);
        if (node.Branches.Count == 0)
        {
            _properties.AddChild(new Label { Text = "Условных веток пока нет. Создай ветку и введи выражение условия." });
            return;
        }

        _selectedBranchIndex = Math.Clamp(_selectedBranchIndex, 0, node.Branches.Count - 1);
        var branchSelect = new OptionButton();
        for (int i = 0; i < node.Branches.Count; i++) branchSelect.AddItem(string.IsNullOrWhiteSpace(node.Branches[i].BranchName) ? $"Ветка {i + 1}" : node.Branches[i].BranchName);
        branchSelect.Select(_selectedBranchIndex);
        branchSelect.ItemSelected += index =>
        {
            _selectedBranchIndex = (int)index;
            Clear(_properties);
            BuildProperties(node);
        };
        _properties.AddChild(branchSelect);
        var removeBranch = new Button { Text = "Удалить выбранную ветку" };
        removeBranch.Pressed += () =>
        {
            node.Branches.RemoveAt(_selectedBranchIndex);
            _selectedBranchIndex = Math.Max(0, _selectedBranchIndex - 1);
            MarkChanged();
            Clear(_properties);
            BuildProperties(node);
        };
        _properties.AddChild(removeBranch);

        var branch = node.Branches[_selectedBranchIndex];
        _properties.AddChild(new Label { Text = $"Условие: {branch.Expression}", AutowrapMode = TextServer.AutowrapMode.WordSmart });
        var editCondition = new Button { Text = "Изменить условие…" };
        editCondition.Pressed += () => OpenConditionBuilder(branch, node);
        _properties.AddChild(editCondition);
    }

    private void OpenConditionBuilder(ConditionBranch branch, IfElseNode node)
    {
        var dialog = new ConditionBuilderDialog(_component, branch.Expression, branch.BranchName);
        AddChild(dialog);
        dialog.ExpressionCreated += (expression, branchName) =>
        {
            branch.Expression = expression;
            branch.BranchName = branchName;
            int branchIndex = node.Branches.IndexOf(branch);
            if (branchIndex < 0)
            {
                node.Branches.Add(branch);
                branchIndex = node.Branches.Count - 1;
            }
            _selectedBranchIndex = branchIndex;
            MarkChanged();
            RefreshTree();
            Clear(_properties);
            BuildProperties(node);
        };
    }
    private void AddPointSelector(NodePath current, Action<NodePath> setter)
    {
        var option = new OptionButton(); option.AddItem("— выбрать точку —", 0);
        var points = _component.GetAvailablePoints();
        for (int i = 0; i < points.Count; i++) option.AddItem(points[i].Name, i + 1);
        option.ItemSelected += index =>
        {
            NodePath path = index == 0 ? new NodePath() : _component.GetPathTo(points[(int)index - 1]);
            setter(path); MarkChanged(); RefreshTree();
        };
        for (int i = 0; i < points.Count; i++) if (_component.GetPathTo(points[i]) == current) { option.Selected = i + 1; break; }
        _properties.AddChild(option);
    }

    private void AddVariableSelector<T>(T selected, Action<T> setter) where T : Variable
    {
        var option = new OptionButton(); option.AddItem("— выбрать переменную —", 0);
        var vars = _component.Variables.Variables;
        var choices = new System.Collections.Generic.List<T>();
        for (int i = 0; i < vars.Count; i++) if (vars[i] is T variable) { choices.Add(variable); option.AddItem(variable.Name); }
        option.ItemSelected += index => { setter(index == 0 ? null : choices[(int)index - 1]); MarkChanged(); RefreshTree(); };
        if (selected != null) for (int i = 0; i < choices.Count; i++) if (ReferenceEquals(choices[i], selected)) { option.Select(i + 1); break; }
        _properties.AddChild(option);
    }

    private void AddChannelSelector(IOChannel selected, bool input, Action<IOChannel> setter)
    {
        var option = new OptionButton(); option.AddItem("— выбрать канал —", 0);
        var channels = input ? _component.IO?.DigitalInputs : _component.IO?.DigitalOutputs;
        if (channels != null) foreach (var channel in channels) option.AddItem(channel.ChannelName);
        option.ItemSelected += index => { setter(index == 0 || channels == null ? null : channels[(int)index - 1]); MarkChanged(); RefreshTree(); };
        if (channels != null) for (int i = 0; i < channels.Count; i++) if (ReferenceEquals(channels[i], selected)) { option.Selected = i + 1; break; }
        _properties.AddChild(option);
    }

    private void RefreshRuntime()
    {
        if (!GodotObject.IsInstanceValid(_component)) return;
        _status.Text = $"Статус: {_component.ProgramStatusText}";
        var context = _component.Context;
        if (context != null)
        {
            string current = context.CurrentExecutingNode == null ? "—" : Describe(context.CurrentExecutingNode);
            _currentNode.Text = $"Текущая команда: {current}\n{context.CurrentStatusText}";
            string text = string.Join("\n", context.Log);
            if (_log.Text != text) { _log.Clear(); _log.AppendText(text); }
        }
    }

    private void MarkChanged()
    {
        _component.NotifyPropertyListChanged();
        _plugin.GetEditorInterface().MarkSceneAsUnsaved();
    }

    private static void Clear(Container container)
    {
        if (container == null) return;
        foreach (Node child in container.GetChildren()) { container.RemoveChild(child); child.QueueFree(); }
    }
}
#endif
