#if TOOLS
using Godot;
using RoboSim.RobotObject;
using RoboSim.RobotObject.RobotComponents;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RoboSim.EditorPluginTools.Docks;

[Tool]
public partial class ConditionBuilderDialog : Window
{
    private readonly RobotProgrammComponent _component;
    private readonly LineEdit _branchName;
    private readonly TextEdit _expression;
    private readonly VBoxContainer _suggestions;
    private readonly Label _validation;
    private readonly List<(string Label, string Text)> _operands = new();

    public event Action<string, string> ExpressionCreated;

    public ConditionBuilderDialog(RobotProgrammComponent component, string expression = "", string branchName = "Если")
    {
        _component = component;
        _branchName = new LineEdit { Text = branchName, PlaceholderText = "Имя ветки" };
        _expression = new TextEdit { Text = expression, PlaceholderText = "Например: ВХОД(\"Готов\") И ПЕРЕМЕННАЯ(\"Счётчик\") >= 3", SizeFlagsVertical = Control.SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 190), WrapMode = TextEdit.LineWrappingMode.Boundary };
        _suggestions = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _validation = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        BuildOperands();
        Build();
        _expression.TextChanged += UpdateValidation;
        CloseRequested += QueueFree;
        UpdateValidation();
    }

    public override void _Ready() => PopupCentered(new Vector2I(960, 650));

    private void Build()
    {
        Title = "Условие ветки";
        MinSize = new Vector2I(900, 620);
        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 7);
        AddChild(root);
        var header = new HBoxContainer();
        header.AddChild(new Label { Text = "Условие ветки", ThemeTypeVariation = "HeaderLarge", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        header.AddChild(new Label { Text = "Название:" });
        _branchName.CustomMinimumSize = new Vector2(180, 0);
        header.AddChild(_branchName);
        root.AddChild(header);

        var split = new HSplitContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        var left = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        left.AddChild(new Label { Text = "Формула условия", ThemeTypeVariation = "HeaderSmall" });
        left.AddChild(_expression);
        left.AddChild(new Label { Text = "Подсказки: выберите операнд, чтобы вставить его в позицию курсора." });
        var suggestionScroll = new ScrollContainer { CustomMinimumSize = new Vector2(0, 90), SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        suggestionScroll.AddChild(_suggestions);
        left.AddChild(suggestionScroll);
        left.AddChild(new HSeparator());
        left.AddChild(new Label { Text = "Синтаксис", ThemeTypeVariation = "HeaderSmall" });
        left.AddChild(new Label
        {
            Text = "Логика: И / &&, ИЛИ / ||, НЕ / !; используйте скобки.\nСравнения: ==  !=  >  >=  <  <=\nАрифметика: +  -  *  /  % и унарные +/-.\nОперанды: ПЕРЕМЕННАЯ(\"имя\"), ВХОД(\"имя\"), ВЫХОД(\"имя\").\nЛогические значения: ИСТИНА, ЛОЖЬ. Приоритет: арифметика → сравнение → НЕ → И → ИЛИ.\nПример: !(ВХОД(\"Готов\") || ПЕРЕМЕННАЯ(\"Счётчик\") + 1 >= 3)",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });
        split.AddChild(left);

        var right = new VBoxContainer { CustomMinimumSize = new Vector2(270, 0) };
        right.AddChild(new Label { Text = "Доступные входы, выходы и переменные", ThemeTypeVariation = "HeaderSmall" });
        var operandScroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        var list = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        AddSection(list, "Входы", _component.IO?.DigitalInputs?.Where(x => x != null).Select(x => (x.ChannelName, $"ВХОД({Quote(x.ChannelName)})")));
        AddSection(list, "Выходы", _component.IO?.DigitalOutputs?.Where(x => x != null).Select(x => (x.ChannelName, $"ВЫХОД({Quote(x.ChannelName)})")));
        AddSection(list, "Переменные", _component.Variables?.Variables?.Where(x => x != null).Select(x => (x.Name, $"ПЕРЕМЕННАЯ({Quote(x.Name)})")));
        operandScroll.AddChild(list);
        right.AddChild(operandScroll);
        split.AddChild(right);
        root.AddChild(split);
        root.AddChild(_validation);

        var footer = new HBoxContainer();
        footer.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        var cancel = new Button { Text = "Отмена" };
        cancel.Pressed += QueueFree;
        var save = new Button { Text = "Сохранить ветку" };
        save.Pressed += Save;
        footer.AddChild(cancel);
        footer.AddChild(save);
        root.AddChild(footer);
    }

    private void BuildOperands()
    {
        AddOperands("ВХОД", _component.IO?.DigitalInputs?.Where(x => x != null).Select(x => x.ChannelName));
        AddOperands("ВЫХОД", _component.IO?.DigitalOutputs?.Where(x => x != null).Select(x => x.ChannelName));
        AddOperands("ПЕРЕМЕННАЯ", _component.Variables?.Variables?.Where(x => x != null).Select(x => x.Name));
        _operands.Add(("ИСТИНА", "ИСТИНА"));
        _operands.Add(("ЛОЖЬ", "ЛОЖЬ"));
    }

    private void AddOperands(string kind, IEnumerable<string> names)
    { if (names != null) foreach (string name in names) _operands.Add(($"{kind}: {name}", $"{kind}({Quote(name)})")); }

    private void AddSection(VBoxContainer parent, string title, IEnumerable<(string, string)> items)
    {
        parent.AddChild(new Label { Text = title, ThemeTypeVariation = "HeaderSmall" });
        var values = items?.ToList() ?? new List<(string, string)>();
        if (values.Count == 0) parent.AddChild(new Label { Text = "— нет" });
        else foreach (var (label, text) in values) AddOperandButton(parent, label, text);
        parent.AddChild(new HSeparator());
    }

    private void AddOperandButton(Container parent, string label, string text)
    {
        var button = new Button { Text = label, TooltipText = $"Вставить {text}", Alignment = HorizontalAlignment.Left, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        button.Pressed += () => { _expression.InsertTextAtCaret(text); _expression.GrabFocus(); UpdateValidation(); };
        parent.AddChild(button);
    }

    private void UpdateValidation()
    {
        Clear(_suggestions);
        string query = CurrentToken();
        foreach (var (label, text) in _operands.Where(x => string.IsNullOrEmpty(query) || x.Label.Contains(query, StringComparison.OrdinalIgnoreCase) || x.Text.Contains(query, StringComparison.OrdinalIgnoreCase)).Take(8))
            AddOperandButton(_suggestions, label, text);
        try
        {
            var context = _component.Context ?? new RobotProgrammContext(_component, _component.Variables, _component.IO);
            ConditionExpressionParser.Evaluate(_expression.Text, context);
            _validation.Text = "Синтаксис корректен.";
            _validation.AddThemeColorOverride("font_color", new Color(0.35f, 0.85f, 0.55f));
        }
        catch (Exception error)
        {
            _validation.Text = error.Message;
            _validation.AddThemeColorOverride("font_color", new Color(1f, 0.58f, 0.35f));
        }
    }

    private string CurrentToken()
    {
        string[] lines = _expression.Text.Split('\n');
        int line = Math.Clamp(_expression.GetCaretLine(), 0, lines.Length - 1);
        string prefix = lines[line][..Math.Clamp(_expression.GetCaretColumn(), 0, lines[line].Length)];
        int start = prefix.LastIndexOfAny(new[] { ' ', '\t', '(', ')', '=', '!', '>', '<', '+', '-', '*', '/', '%' });
        return prefix[(start + 1)..].Trim('"', '\'');
    }

    private void Save()
    {
        try
        {
            var context = _component.Context ?? new RobotProgrammContext(_component, _component.Variables, _component.IO);
            ConditionExpressionParser.Evaluate(_expression.Text, context);
            ExpressionCreated?.Invoke(_expression.Text.Trim(), string.IsNullOrWhiteSpace(_branchName.Text) ? "Если" : _branchName.Text.Trim());
            QueueFree();
        }
        catch (Exception error)
        {
            _validation.Text = error.Message;
            _validation.AddThemeColorOverride("font_color", new Color(1f, 0.45f, 0.35f));
        }
    }

    private static string Quote(string value) => "\"" + (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    private static void Clear(Container container) { foreach (Node child in container.GetChildren()) { container.RemoveChild(child); child.QueueFree(); } }
}
#endif
