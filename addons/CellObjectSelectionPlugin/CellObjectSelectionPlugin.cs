#if TOOLS
using Godot;
using RoboSim.Global;
using System.Collections.Generic;

[Tool]
public partial class CellObjectSelectionPlugin : EditorPlugin
{
    private EditorSelection _selection;

    private readonly HashSet<CellObject> _selectedCells = new();
    private bool _normalizingSelection;

    public override void _EnterTree()
    {
        _selection = EditorInterface.Singleton.GetSelection();
        _selection.SelectionChanged += OnSelectionChanged;

        OnSelectionChanged();
    }

    public override void _ExitTree()
    {
        if (_selection != null)
            _selection.SelectionChanged -= OnSelectionChanged;

        ClearSelectedCells();
    }

    private void OnSelectionChanged()
    {
        if (_normalizingSelection)
            return;

        var selectedNodes = _selection.GetSelectedNodes();

        var newSelectedCells = new HashSet<CellObject>();
        var newSelectionTargets = new HashSet<Node>();

        foreach (Node node in selectedNodes)
        {
            CellObject cell = FindCellObject(node);

            if (!GodotObject.IsInstanceValid(cell))
                continue;

            newSelectedCells.Add(cell);

            Node target = cell.GetEditorSelectionTarget();

            if (GodotObject.IsInstanceValid(target))
                newSelectionTargets.Add(target);
        }

        // Обновляем фактическое выделение, если оно отличается.
        bool selectionChanged =
            selectedNodes.Count != newSelectionTargets.Count;

        if (!selectionChanged)
        {
            foreach (Node node in selectedNodes)
            {
                if (!newSelectionTargets.Contains(node))
                {
                    selectionChanged = true;
                    break;
                }
            }
        }

        if (selectionChanged)
        {
            _normalizingSelection = true;

            _selection.Clear();

            foreach (Node target in newSelectionTargets)
            {
                if (GodotObject.IsInstanceValid(target))
                    _selection.AddNode(target);
            }

            _normalizingSelection = false;
        }

        // Уведомляем снятые с выделения объекты.
        foreach (CellObject oldCell in _selectedCells)
        {
            if (!GodotObject.IsInstanceValid(oldCell))
                continue;

            if (!newSelectedCells.Contains(oldCell))
                oldCell.OnDeselected();
        }

        // Уведомляем новые выделенные объекты.
        foreach (CellObject newCell in newSelectedCells)
        {
            if (!GodotObject.IsInstanceValid(newCell))
                continue;

            if (!_selectedCells.Contains(newCell))
                newCell.OnSelected();
        }

        _selectedCells.Clear();

        foreach (CellObject cell in newSelectedCells)
        {
            if (GodotObject.IsInstanceValid(cell))
                _selectedCells.Add(cell);
        }
    }

    private CellObject FindCellObject(Node node)
    {
        while (GodotObject.IsInstanceValid(node))
        {
            if (node is CellObject cell)
                return cell;

            node = node.GetParent();
        }

        return null;
    }

    private void ClearSelectedCells()
    {
        foreach (CellObject cell in _selectedCells)
        {
            if (GodotObject.IsInstanceValid(cell))
                cell.OnDeselected();
        }

        _selectedCells.Clear();
    }
}
#endif