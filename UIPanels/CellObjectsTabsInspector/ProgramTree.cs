#if TOOLS
using Godot;
using System;

namespace RoboSim.EditorPluginTools.Docks;

[Tool]
public partial class ProgramTree : Tree
{
    public Action<string, int, string, int> ReorderRequested;

    public ProgramTree()
    {
        HideRoot = true;
    }

    public override Variant _GetDragData(Vector2 atPosition)
    {
        var item = GetItemAtPosition(atPosition);
        if (item == null) return default;
        string key = item.GetMetadata(0).AsString();
        if (!TryParseKey(key, out _, out _)) return default;
        SetDragPreview(new Label { Text = item.GetText(0) });
        return Variant.From(key);
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (data.VariantType != Variant.Type.String) return false;
        DropModeFlags = (int)(Tree.DropModeFlagsEnum.OnItem | Tree.DropModeFlagsEnum.Inbetween);
        var target = GetItemAtPosition(atPosition);
        if (target == null) return false;
        string sourceKey = data.AsString();
        string targetKey = target.GetMetadata(0).AsString();
        return TryParseKey(sourceKey, out _, out _) && TryParseTarget(targetKey, out _, out _);
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        var target = GetItemAtPosition(atPosition);
        if (target == null || !TryParseKey(data.AsString(), out string sourceScope, out int sourceIndex) ||
            !TryParseTarget(target.GetMetadata(0).AsString(), out string targetScope, out int targetIndex))
            return;
        ReorderRequested?.Invoke(sourceScope, sourceIndex, targetScope, targetIndex);
    }

    private static bool TryParseTarget(string key, out string scope, out int index)
    {
        if (TryParseKey(key, out scope, out index)) return true;
        const string prefix = "scope|";
        if (key != null && key.StartsWith(prefix, StringComparison.Ordinal))
        {
            scope = key.Substring(prefix.Length);
            index = -1;
            return !string.IsNullOrWhiteSpace(scope);
        }
        scope = "";
        index = -1;
        return false;
    }

    public static string MakeKey(string scope, int index) => $"{scope}|{index}";
    public static string MakeScopeKey(string scope) => $"scope|{scope}";
    public static bool TryParseKey(string key, out string scope, out int index)
    {
        scope = "";
        index = -1;
        string[] parts = key.Split('|');
        if (parts.Length != 2 || !int.TryParse(parts[1], out index)) return false;
        scope = parts[0];
        return !string.IsNullOrWhiteSpace(scope);
    }
}
#endif
