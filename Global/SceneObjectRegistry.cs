#if TOOLS
using Godot;
using System;
using System.Collections.Generic;

namespace RoboSim.Global;

/// <summary>Хранит CellObject активной редактируемой сцены для иерархии и редактора связей сигналов.</summary>
public static class SceneObjectRegistry
{
    private static readonly List<CellObject> _objects = new();
    private static Node _sceneRoot;

    /// <summary>Объекты CellObject, найденные в активной сцене.</summary>
    public static IReadOnlyList<CellObject> Objects => _objects;

    /// <summary>Корень сцены, для которой построен текущий реестр.</summary>
    public static Node SceneRoot => GodotObject.IsInstanceValid(_sceneRoot) ? _sceneRoot : null;

    /// <summary>Уведомляет панели редактора об изменении состава объектов активной сцены.</summary>
    public static event Action Changed;

    /// <summary>Пересобирает список CellObject рекурсивно для переданного корня сцены.</summary>
    public static void Refresh(Node sceneRoot)
    {
        bool rootChanged = !ReferenceEquals(_sceneRoot, sceneRoot);
        _sceneRoot = GodotObject.IsInstanceValid(sceneRoot) ? sceneRoot : null;

        var nextObjects = new List<CellObject>();
        if (GodotObject.IsInstanceValid(_sceneRoot)) CollectCellObjects(_sceneRoot, nextObjects);

        bool objectsChanged = nextObjects.Count != _objects.Count;
        if (!objectsChanged)
            for (int i = 0; i < nextObjects.Count; i++)
                if (!ReferenceEquals(nextObjects[i], _objects[i])) { objectsChanged = true; break; }

        if (!rootChanged && !objectsChanged) return;

        _objects.Clear();
        _objects.AddRange(nextObjects);
        Changed?.Invoke();
    }

    /// <summary>Добавляет все CellObject из поддерева в порядке обхода сцены.</summary>
    private static void CollectCellObjects(Node parent, List<CellObject> result)
    {
        if (parent is CellObject cellObject) result.Add(cellObject);
        foreach (Node child in parent.GetChildren())
        {
            if (GodotObject.IsInstanceValid(child)) CollectCellObjects(child, result);
        }
    }
}
#endif
