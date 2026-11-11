
#if TOOLS
using Godot;
using System;
using System.Collections.Generic;

namespace RoboSim.Global.Components.IOComponents
{
    public enum IOChannelType
    {
        DigitalInput,
        DigitalOutput,
    }

    /// <summary>
    /// КАНАЛ ИО
    /// </summary>
   


    [Tool]
    public partial class IOComponent : CellComponent
    {
        // =============================================
        // CONFIGURATION
        // =============================================

        [Export] public Godot.Collections.Array<IOChannel> DigitalInputs { get; set; } = new();
        [Export] public Godot.Collections.Array<IOChannel> DigitalOutputs { get; set; } = new();

        [ExportGroup("Control")]


 
        private bool _subscribed;


        // =============================================
        // INITIALIZATION
        // =============================================

        public  void Start()
        {
            SubscribeGlobalState();
        }


        public  void Exit()
        {
            UnsubscribeGlobalState();
        }


        private void SubscribeGlobalState()
        {
            if (_subscribed)
                return;

            GlobalState.Instance.SimStatusChanged += OnSimulationStatusChanged;
            _subscribed = true;
        }

        private void UnsubscribeGlobalState()
        {
            if (!_subscribed)
                return;

            GlobalState.Instance.SimStatusChanged -= OnSimulationStatusChanged;
            _subscribed = false;
        }

        /// <summary>
        /// Дествие при изменении режима симуляции
        /// </summary>
        /// <param name="status"></param>
        private void OnSimulationStatusChanged(SimulationStatus status)
        {
        }

        // =============================================
        //Добавление и удаление каналов
        // =============================================
        
        [Export] public string Name { get; set; } = "";
        [ExportToolButton("Добавить входной канал")]
        public Callable RunMethod => Callable.From(() => AddInputChannel(Name));
        /// <summary>
        /// Добавить входной канал 
        /// </summary>
        /// <param name="name"></param>
        public bool AddInputChannel(string name)
        {
            if (name == null) return false;
            IOChannel newChannel = new(name, IOChannelType.DigitalInput,this);
            DigitalInputs.Add(newChannel);
            return true;
        }
        /// <summary>
        /// добавить выходной канал
        /// </summary>
        /// <param name="name"></param>
        public bool AddOutputChannel(string name)
        {
            if (name == null) return false;
            IOChannel newChannel = new(name, IOChannelType.DigitalOutput,this);
            DigitalOutputs.Add(newChannel);
            return true;
        }
        /// <summary>
        /// Удаление канала, ВЫЗЫВАЕТСЯ САМИМ КАНАЛОМ ТОЛЬКО
        /// </summary>
        /// <param name="channel"></param>
        /// <exception cref="Exception"></exception>
        public bool Remove(IOChannel channel)
        {
            if (channel == null) throw new Exception("Переданный канал равен NULL");
            else if (channel.isInput)
            {
                if (!DigitalInputs.Contains(channel)) throw new Exception("Входной канал не находится в спике каналов");
                DigitalInputs.Remove(channel);
                return true;
            }
            else if (channel.isOutput)
            {
                if (!DigitalOutputs.Contains(channel)) throw new Exception("Выходной канал не находится в спсике каналов");
                DigitalOutputs.Remove(channel);
                return true;
            }
            return false;
        }


        // =============================================
        //Получение каналов
        // =============================================

        public IEnumerable<IOChannel> GetAllOutChannels()
        {
            foreach (IOChannel channel in DigitalOutputs)
                if (channel != null) yield return channel;
        }
        public IEnumerable<IOChannel> GetAllInpChannels()
        {
            foreach (IOChannel channel in DigitalInputs)
                if (channel != null) yield return channel;

        }
        public IOChannel GetOutChanneltoName(string name)
        {
            foreach (IOChannel channel in DigitalOutputs)
                if (channel.ChannelName == name)  return channel;
            return null;
        }
        public IOChannel GetInpChanneltoName(string name)
        {
            foreach (IOChannel channel in DigitalInputs)
                if (channel.ChannelName == name) return channel;
            return null;
        }



        // =============================================
        // СЕТТЕРЫ
        // =============================================

        //программа будет задавать выход
        public bool SetOutputChannel(bool value, string NameChannel)
        {
            IOChannel channel = GetOutChanneltoName(NameChannel);
            if (channel == null) return false;

            channel.Value = value;
            return true;
        }

        public override void Initialize()
        {
            //throw new NotImplementedException();
        }
    }
}
#endif
