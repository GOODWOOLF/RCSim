
#if TOOLS
using Godot;
using System;
using System.IO;

namespace РобоСим.Editor;

[Tool]
public partial class CellObjectsLibrary : EditorDock
{
    public event Action<string> ObjectActivated;

    private const string EquipmentPath = "res://RTK/CellObjects";

    private Tree _tree;

    public override void _Ready()
    {
        BuildInterface();
        RefreshLibrary();
    }

    private void BuildInterface()
    {
        var mainContainer = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };

        AddChild(mainContainer);

        var toolbar = new HBoxContainer();
        mainContainer.AddChild(toolbar);

        var title = new Label
        {
            Text = "Cell Objects",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };

        toolbar.AddChild(title);

        var refreshButton = new Button
        {
            Text = "Refresh",
            TooltipText = "Refresh equipment library"
        };

        refreshButton.Pressed += RefreshLibrary;
        toolbar.AddChild(refreshButton);

        _tree = new Tree
        {
            HideRoot = true,
            Columns = 1,
            SelectMode = Tree.SelectModeEnum.Single,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };

        _tree.ItemActivated += OnItemActivated;
        mainContainer.AddChild(_tree);
    }

    public void RefreshLibrary()
    {
        if (_tree == null)
            return;

        _tree.Clear();

        if (!DirAccess.DirExistsAbsolute(EquipmentPath))
        {
            GD.PushWarning($"Equipment directory not found: {EquipmentPath}");
            return;
        }

        TreeItem root = _tree.CreateItem();

        ScanDirectory(EquipmentPath, root);
    }

    private void ScanDirectory(string path, TreeItem parent)
    {
        using DirAccess directory = DirAccess.Open(path);

        if (directory == null)
        {
            GD.PushWarning($"Cannot open directory: {path}");
            return;
        }

        directory.ListDirBegin();

        string fileName = directory.GetNext();

        while (!string.IsNullOrEmpty(fileName))
        {
            if (fileName == "." || fileName == "..")
            {
                fileName = directory.GetNext();
                continue;
            }

            string fullPath = path.PathJoin(fileName);

            if (directory.CurrentIsDir())
            {
                TreeItem folderItem = _tree.CreateItem(parent);

                folderItem.SetText(0, fileName);
                folderItem.SetMetadata(0, "");

                ScanDirectory(fullPath, folderItem);
            }
            else
            {
                string extension = Path.GetExtension(fileName)
                    .ToLowerInvariant();

                if (extension == ".tscn" || extension == ".res")
                {
                    AddObjectItem(parent, fullPath, fileName);
                }
            }

            fileName = directory.GetNext();
        }

        directory.ListDirEnd();
    }

    private void AddObjectItem(
        TreeItem parent,
        string path,
        string fileName)
    {
        TreeItem item = _tree.CreateItem(parent);

        item.SetText(0, Path.GetFileNameWithoutExtension(fileName));
        item.SetMetadata(0, path);
        item.SetTooltipText(0, path);
    }

    // Вызывается при активации элемента дерева.
    private void OnItemActivated()
    {
        TreeItem item = _tree.GetSelected();

        if (item == null)
            return;

        string path = item.GetMetadata(0).AsString();

        // Папка не содержит путь к объекту.
        if (string.IsNullOrEmpty(path))
            return;

        if (!ResourceLoader.Exists(path))
        {
            GD.PushWarning($"Object not found: {path}");
            return;
        }

        GD.Print($"Object activated: {path}");

        ObjectActivated?.Invoke(path);
    }
}
#endif
