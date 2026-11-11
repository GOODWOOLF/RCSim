
//#if TOOLS
//using Godot;
//using System.Collections.Generic;
//using РобоСим.Editor.Pages;
//using РобоСим.Global;
//using РобоСим.Global.Components;

//namespace РобоСим.Editor;

//[Tool]
//public partial class CellObjectWorkspace : EditorDock
//{
//    private EditorSelection _selection;

//    private VBoxContainer _root;

//    private Label _equipmentName;
//    private Label _equipmentType;

//    private TabContainer _tabs;


//    private CellObject _currentEquipment;

//    // =====================================================
//    // INITIALIZATION
//    // =====================================================

//    public override void _Ready()
//    {
//        BuildInterface();

//        _selection = EditorInterface.Singleton.GetSelection();

//        _selection.SelectionChanged += OnSelectionChanged;

//        UpdateSelection();
//    }

//    public override void _ExitTree()
//    {
//        if (_selection != null)
//            _selection.SelectionChanged -= OnSelectionChanged;

//        _currentEquipment = null;
//    }

//    // =====================================================
//    // MAIN INTERFACE
//    // =====================================================

//    private void BuildInterface()
//    {
//        _root = new VBoxContainer
//        {
//            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
//            SizeFlagsVertical = Control.SizeFlags.ExpandFill
//        };

//        AddChild(_root);

//        // HEADER

//        _equipmentName = new Label
//        {
//            Text = "No equipment selected"
//        };

//        _root.AddChild(_equipmentName);

//        _equipmentType = new Label
//        {
//            Text = "",
//            Modulate = new Color(0.65f, 0.65f, 0.65f)
//        };

//        _root.AddChild(_equipmentType);

//        _root.AddChild(new HSeparator());

//        // TABS

//        _tabs = new TabContainer
//        {
//            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
//            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
//            TabsVisible = true,
//            ClipContents = true
//        };

//        _root.AddChild(_tabs);

//        ShowEmptyState();
//    }

//    // =====================================================
//    // SELECTION
//    // =====================================================

//    private void OnSelectionChanged()
//    {
//        UpdateSelection();
//    }

//    public void RefreshSelection()
//    {
//        UpdateSelection();
//    }

//    private void UpdateSelection()
//    {
//        if (_selection == null)
//            return;

//        var selectedNodes = _selection.GetSelectedNodes();

//        if (selectedNodes.Count == 0)
//        {
//            _currentEquipment = null;
//            ShowEmptyState();
//            return;
//        }

//        // Последний выбранный узел считаем активным.
//        Node selectedNode = selectedNodes[selectedNodes.Count - 1];

//        if (selectedNode == null ||
//            !GodotObject.IsInstanceValid(selectedNode))
//        {
//            _currentEquipment = null;
//            ShowEmptyState();
//            return;
//        }

//        CellObject equipment = FindCellObject(selectedNode);

//        if (equipment == null)
//        {
//            _currentEquipment = null;
//            ShowEmptyState();
//            return;
//        }

//        // Оборудование не изменилось.
//        if (_currentEquipment == equipment)
//            return;

//        _currentEquipment = equipment;

//        BuildEquipmentWorkspace(equipment);
//    }

//    // =====================================================
//    // FIND CELL OBJECT
//    // =====================================================

//    private CellObject FindCellObject(Node node)
//    {
//        Node current = node;

//        while (current != null &&
//               GodotObject.IsInstanceValid(current))
//        {
//            if (current is CellObject equipment)
//                return equipment;

//            current = current.GetParent();
//        }

//        return null;
//    }

//    // =====================================================
//    // BUILD EQUIPMENT WORKSPACE
//    // =====================================================

//    private void BuildEquipmentWorkspace(CellObject equipment)
//    {
//        ClearTabs();

//        if (equipment == null ||
//            !GodotObject.IsInstanceValid(equipment))
//        {
//            ShowEmptyState();
//            return;
//        }

//        _equipmentName.Text = equipment.Name;
//        _equipmentType.Text = equipment.GetType().Name;

//        BuildGeneralPage(equipment);

//        BuildIOPage(equipment);

//        // Первая вкладка при выборе оборудования.
//        if (_tabs.GetTabCount() > 0)
//            _tabs.CurrentTab = 0;
//    }

//    // =====================================================
//    // GENERAL PAGE
//    // =====================================================

//    private void BuildGeneralPage(CellObject equipment)
//    {
//        var generalPage = new GeneralEquipmentPage
//        {
//            Name = "General",

//            SizeFlagsHorizontal =
//                Control.SizeFlags.ExpandFill,

//            SizeFlagsVertical =
//                Control.SizeFlags.ExpandFill
//        };

//        _tabs.AddChild(generalPage);

//        generalPage.Setup(equipment);
//    }

//    // =====================================================
//    // I/O PAGE
//    // =====================================================

//    private void BuildIOPage(CellObject equipment)
//    {
//        IOComponent ioComponent = FindIOComponent(equipment);

//        // Если у оборудования нет I/O,
//        // вкладка не создаётся.
//        if (ioComponent == null)
//            return;

//        var ioPage = new IOComponentPage
//        {
//            Name = "I/O",

//            SizeFlagsHorizontal =
//                Control.SizeFlags.ExpandFill,

//            SizeFlagsVertical =
//                Control.SizeFlags.ExpandFill
//        };

//        _tabs.AddChild(ioPage);

//        ioPage.Setup(
//            ioComponent,
//            SignalManager,
//            GetAllEquipment()
//        );
//    }

//    // =====================================================
//    // FIND I/O COMPONENT
//    // =====================================================

//    private IOComponent FindIOComponent(Node node)
//    {
//        if (node == null ||
//            !GodotObject.IsInstanceValid(node))
//            return null;

//        if (node is IOComponent io)
//            return io;

//        foreach (Node child in node.GetChildren())
//        {
//            IOComponent result = FindIOComponent(child);

//            if (result != null)
//                return result;
//        }

//        return null;
//    }

//    // =====================================================
//    // GET ALL EQUIPMENT
//    // =====================================================

//    private List<CellObject> GetAllEquipment()
//    {
//        var result = new List<CellObject>();

//        Node sceneRoot =
//            EditorInterface.Singleton.GetEditedSceneRoot();

//        if (sceneRoot != null)
//            CollectEquipment(sceneRoot, result);

//        return result;
//    }

//    private void CollectEquipment(
//        Node node,
//        List<CellObject> result)
//    {
//        if (node == null ||
//            !GodotObject.IsInstanceValid(node))
//            return;

//        if (node is CellObject equipment)
//            result.Add(equipment);

//        foreach (Node child in node.GetChildren())
//        {
//            CollectEquipment(child, result);
//        }
//    }

//    // =====================================================
//    // CLEAR TABS
//    // =====================================================

//    private void ClearTabs()
//    {
//        if (_tabs == null)
//            return;

//        foreach (Node child in _tabs.GetChildren())
//        {
//            _tabs.RemoveChild(child);
//            child.QueueFree();
//        }
//    }

//    // =====================================================
//    // EMPTY STATE
//    // =====================================================

//    private void ShowEmptyState()
//    {
//        if (_tabs == null)
//            return;

//        ClearTabs();

//        _equipmentName.Text = "No equipment selected";
//        _equipmentType.Text = "";

//        var emptyPage = new VBoxContainer
//        {
//            Name = "Empty",

//            SizeFlagsHorizontal =
//                Control.SizeFlags.ExpandFill,

//            SizeFlagsVertical =
//                Control.SizeFlags.ExpandFill
//        };

//        emptyPage.AddChild(new Label
//        {
//            Text = "Select equipment in the 3D Viewport.",

//            HorizontalAlignment =
//                HorizontalAlignment.Center,

//            VerticalAlignment =
//                VerticalAlignment.Center,

//            SizeFlagsVertical =
//                Control.SizeFlags.ExpandFill
//        });

//        _tabs.AddChild(emptyPage);
//    }
//}
//#endif
