using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RoboSim.Global.Components.IOComponents;

namespace RoboSim.RobotObject;

/// <summary>Evaluates user-authored Boolean expressions for program branches.</summary>
public sealed class ConditionExpressionParser
{
    private readonly RobotProgrammContext _context;
    private readonly List<Token> _tokens;
    private int _position;

    private ConditionExpressionParser(string source, RobotProgrammContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _tokens = Tokenize(source ?? string.Empty);
    }

    public static bool Evaluate(string expression, RobotProgrammContext context)
    {
        var parser = new ConditionExpressionParser(expression, context);
        Func<Value> result = parser.ParseOr();
        parser.Expect(TokenKind.End, "Лишний текст после выражения.");
        return result().AsBool(parser.Current.Position);
    }

    private Func<Value> ParseOr()
    {
        Func<Value> left = ParseAnd();
        while (Match(TokenKind.Or))
        {
            int position = Previous.Position;
            Func<Value> previous = left;
            Func<Value> right = ParseAnd();
            left = () => Value.Boolean(previous().AsBool(position) || right().AsBool(position));
        }
        return left;
    }

    private Func<Value> ParseAnd()
    {
        Func<Value> left = ParseEquality();
        while (Match(TokenKind.And))
        {
            int position = Previous.Position;
            Func<Value> previous = left;
            Func<Value> right = ParseEquality();
            left = () => Value.Boolean(previous().AsBool(position) && right().AsBool(position));
        }
        return left;
    }

    private Func<Value> ParseEquality()
    {
        Func<Value> left = ParseComparison();
        while (Current.Kind is TokenKind.Equal or TokenKind.NotEqual)
        {
            Token operation = _tokens[_position++];
            Func<Value> previous = left;
            Func<Value> right = ParseComparison();
            left = () =>
            {
                bool equals = previous().SameValue(right(), operation.Position);
                return Value.Boolean(operation.Kind == TokenKind.Equal ? equals : !equals);
            };
        }
        return left;
    }

    private Func<Value> ParseComparison()
    {
        Func<Value> left = ParseAdditive();
        while (Current.Kind is TokenKind.Greater or TokenKind.GreaterEqual or TokenKind.Less or TokenKind.LessEqual)
        {
            Token operation = _tokens[_position++];
            Func<Value> previous = left;
            Func<Value> right = ParseAdditive();
            left = () =>
            {
                float a = previous().AsNumber(operation.Position), b = right().AsNumber(operation.Position);
                return Value.Boolean(operation.Kind switch
                {
                    TokenKind.Greater => a > b, TokenKind.GreaterEqual => a >= b,
                    TokenKind.Less => a < b, _ => a <= b
                });
            };
        }
        return left;
    }

    private Func<Value> ParseAdditive()
    {
        Func<Value> left = ParseMultiplicative();
        while (Current.Kind is TokenKind.Plus or TokenKind.Minus)
        {
            Token operation = _tokens[_position++];
            Func<Value> previous = left;
            Func<Value> right = ParseMultiplicative();
            left = () =>
            {
                float a = previous().AsNumber(operation.Position), b = right().AsNumber(operation.Position);
                return Value.Number(operation.Kind == TokenKind.Plus ? a + b : a - b);
            };
        }
        return left;
    }

    private Func<Value> ParseMultiplicative()
    {
        Func<Value> left = ParseUnary();
        while (Current.Kind is TokenKind.Multiply or TokenKind.Divide or TokenKind.Modulo)
        {
            Token operation = _tokens[_position++];
            Func<Value> previous = left;
            Func<Value> right = ParseUnary();
            left = () =>
            {
                float a = previous().AsNumber(operation.Position), b = right().AsNumber(operation.Position);
                if ((operation.Kind == TokenKind.Divide || operation.Kind == TokenKind.Modulo) && MathfIsZero(b))
                    throw ErrorAt(operation, "Деление на ноль.");
                return Value.Number(operation.Kind switch { TokenKind.Multiply => a * b, TokenKind.Divide => a / b, _ => a % b });
            };
        }
        return left;
    }

    private Func<Value> ParseUnary()
    {
        if (Match(TokenKind.Not)) { int position = Previous.Position; Func<Value> operand = ParseUnary(); return () => Value.Boolean(!operand().AsBool(position)); }
        if (Match(TokenKind.Minus)) { int position = Previous.Position; Func<Value> operand = ParseUnary(); return () => Value.Number(-operand().AsNumber(position)); }
        if (Match(TokenKind.Plus)) { int position = Previous.Position; Func<Value> operand = ParseUnary(); return () => Value.Number(operand().AsNumber(position)); }
        return ParsePrimary();
    }

    private Func<Value> ParsePrimary()
    {
        if (Match(TokenKind.Number))
        {
            Token token = Previous;
            if (!float.TryParse(token.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float number)) throw ErrorAt(token, "Некорректное число.");
            return () => Value.Number(number);
        }
        if (Match(TokenKind.LeftParen))
        {
            Func<Value> value = ParseOr();
            Expect(TokenKind.RightParen, "Ожидается закрывающая скобка ')'.");
            return value;
        }
        if (Match(TokenKind.True)) return () => Value.Boolean(true);
        if (Match(TokenKind.False)) return () => Value.Boolean(false);
        if (Match(TokenKind.Reference)) return ParseReference(Previous);
        throw Error("Ожидается переменная, вход, выход, число или выражение в скобках.");
    }

    private Func<Value> ParseReference(Token reference)
    {
        string kind = reference.Text;
        Expect(TokenKind.LeftParen, $"После {kind} ожидается '('.");
        Token name = Expect(TokenKind.String, "Имя операнда укажите в кавычках.");
        Expect(TokenKind.RightParen, "После имени операнда ожидается ')'.");
        if (kind is "ПЕРЕМЕННАЯ" or "VAR")
        {
            Variable variable = _context.VariableManager?.FindByName(name.Text);
            return variable switch
            {
                NumberVariable n => () => Value.Number(n.Value),
                BoolVariable b => () => Value.Boolean(b.Value),
                _ => throw ErrorAt(name, $"Переменная «{name.Text}» не найдена.")
            };
        }
        var channels = kind is "ВХОД" or "DI" ? _context.IOManager?.DigitalInputs : _context.IOManager?.DigitalOutputs;
        IOChannel channel = channels?.FirstOrDefault(x => x?.ChannelName == name.Text);
        if (channel == null) throw ErrorAt(name, $"Канал «{name.Text}» не найден.");
        return () => Value.Boolean(channel.Value);
    }

    private static bool MathfIsZero(float value) => Math.Abs(value) < 1e-12f;

    private bool Match(TokenKind kind)
    { if (Current.Kind != kind) return false; _position++; return true; }
    private Token Expect(TokenKind kind, string message)
    { if (Current.Kind != kind) throw Error(message); return _tokens[_position++]; }
    private Token Current => _tokens[Math.Min(_position, _tokens.Count - 1)];
    private Token Previous => _tokens[Math.Max(0, _position - 1)];
    private FormatException Error(string message) => ErrorAt(Current, message);
    private static FormatException ErrorAt(Token token, string message) => new($"{message} Позиция {token.Position + 1}.");

    private static List<Token> Tokenize(string source)
    {
        var tokens = new List<Token>();
        for (int i = 0; i < source.Length;)
        {
            char c = source[i];
            if (char.IsWhiteSpace(c)) { i++; continue; }
            if (c is '"' or '\'')
            {
                int start = i++; char quote = c; var value = new System.Text.StringBuilder();
                while (i < source.Length && source[i] != quote)
                { if (source[i] == '\\' && i + 1 < source.Length) i++; value.Append(source[i++]); }
                if (i >= source.Length) throw new FormatException($"Не закрыта кавычка. Позиция {start + 1}.");
                i++; tokens.Add(new(TokenKind.String, value.ToString(), start)); continue;
            }
            if (char.IsDigit(c) || c == '.' && i + 1 < source.Length && char.IsDigit(source[i + 1]))
            {
                int start = i++;
                while (i < source.Length && (char.IsDigit(source[i]) || source[i] == '.')) i++;
                tokens.Add(new(TokenKind.Number, source[start..i], start)); continue;
            }
            if (char.IsLetter(c) || c == '_')
            {
                int start = i++;
                while (i < source.Length && (char.IsLetterOrDigit(source[i]) || source[i] == '_')) i++;
                string word = source[start..i];
                TokenKind kind = word.ToUpperInvariant() switch
                {
                    "И" => TokenKind.And, "ИЛИ" => TokenKind.Or, "НЕ" => TokenKind.Not,
                    "ИСТИНА" or "TRUE" => TokenKind.True, "ЛОЖЬ" or "FALSE" => TokenKind.False,
                    "ПЕРЕМЕННАЯ" or "ВХОД" or "ВЫХОД" or "VAR" or "DI" or "DO" => TokenKind.Reference,
                    _ => throw new FormatException($"Неизвестное слово «{word}». Позиция {start + 1}.")
                };
                if (kind == TokenKind.Reference) word = word.ToUpperInvariant();
                tokens.Add(new(kind, word, start)); continue;
            }
            if (i + 1 < source.Length)
            {
                string two = source.Substring(i, 2);
                TokenKind? kind = two switch
                {
                    "&&" => TokenKind.And, "||" => TokenKind.Or, "==" => TokenKind.Equal,
                    "!=" => TokenKind.NotEqual, ">=" => TokenKind.GreaterEqual, "<=" => TokenKind.LessEqual, _ => null
                };
                if (kind.HasValue) { tokens.Add(new(kind.Value, two, i)); i += 2; continue; }
            }
            TokenKind? single = c switch
            {
                '(' => TokenKind.LeftParen, ')' => TokenKind.RightParen,
                '!' => TokenKind.Not, '>' => TokenKind.Greater, '<' => TokenKind.Less,
                '+' => TokenKind.Plus, '-' => TokenKind.Minus, '*' => TokenKind.Multiply,
                '/' => TokenKind.Divide, '%' => TokenKind.Modulo, _ => null
            };
            if (single.HasValue) { tokens.Add(new(single.Value, c.ToString(), i)); i++; continue; }
            if (c == '=') throw new FormatException($"Используйте == для сравнения. Позиция {i + 1}.");
            throw new FormatException($"Неожиданный символ «{c}». Позиция {i + 1}.");
        }
        tokens.Add(new(TokenKind.End, "", source.Length));
        return tokens;
    }

    private enum TokenKind
    {
        Reference, String, Number, True, False, And, Or, Not,
        Equal, NotEqual, Greater, GreaterEqual, Less, LessEqual,
        Plus, Minus, Multiply, Divide, Modulo, LeftParen, RightParen, End
    }
    private readonly record struct Token(TokenKind Kind, string Text, int Position);
    private readonly record struct Value(bool IsBoolean, bool BooleanValue, float NumberValue)
    {
        public static Value Boolean(bool value) => new(true, value, 0);
        public static Value Number(float value) => new(false, false, value);
        public bool AsBool(int position) => IsBoolean ? BooleanValue : throw new FormatException($"Ожидалось логическое значение. Позиция {position + 1}.");
        public float AsNumber(int position) => !IsBoolean ? NumberValue : throw new FormatException($"В арифметике ожидалось число. Позиция {position + 1}.");
        public bool SameValue(Value other, int position)
        {
            if (IsBoolean != other.IsBoolean) throw new FormatException($"Нельзя сравнить число с логическим значением. Позиция {position + 1}.");
            return IsBoolean ? BooleanValue == other.BooleanValue : NumberValue == other.NumberValue;
        }
    }
}
