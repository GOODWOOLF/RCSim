#if TOOLS
using Godot;
using System;
using System.Collections.Generic;
using RoboSim.Global.Components;

namespace RoboSim.Global
{
    /// <summary>
    /// Базовый объект технологической ячейки.
    /// </summary>
    [Tool]
    public abstract partial class CellObject : Node3D
    {
        [Export]
        public string ID { get; private set; } = "";

        protected  List<CellComponent> _components = new();

        [Export]
        public CellObjectType ObjectType { get; private set; } = CellObjectType.Generic;

        public IReadOnlyList<CellComponent> Components => _components;

        /// <summary>
        /// Возвращает первый компонент указанного типа.
        /// </summary>
        public T GetComponent<T>() where T : CellComponent
        {
            foreach (CellComponent component in _components)
            {
                if (component is T typedComponent)
                    return typedComponent;
            }

            return null;
        }
        public override void _Ready()
        {
            base._Ready();
            ID = Guid.NewGuid().ToString();
            // 1. Собираем компоненты
            foreach (Node child in GetNode("Components").GetChildren())
            {
                if (child is CellComponent component)
                    _components.Add(component);
            }
            // 2. Назначаем родителя всем
            foreach (var component in _components)
                component.SetParent(this);

            // 3. Инициализируем все компоненты
            foreach (var component in _components)
                component.Initialize();

        }
        protected bool isSelected = false;
        public void OnSelected()
        {
            GD.Print($"Объект выделен: {Name}");
            isSelected = true;
        }

        public void OnDeselected()
        {
            GD.Print($"Выделение снято: {Name}");
            isSelected = false;
        }
        public virtual Node GetEditorSelectionTarget()
        {
            return this;
        }




    }
}

public enum CellObjectType
{
    RobotRR,
    Conveyor,
    CNC1,
    Generic
}
#endif