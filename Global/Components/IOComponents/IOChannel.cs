#if TOOLS
using Godot;
using System;
using RoboSim.Global.IO;

namespace RoboSim.Global.Components.IOComponents
{
    [GlobalClass]
    public partial class IOChannel : Resource
    {
        [Export]
        public string ChannelName { get; set; } = "";

        
        public IOChannelType ChannelType { get; private set; }
        public event Action<bool> ValueChanged;

        [Export]
        private bool _value;
        public bool Value
        {
            get => _value;
            set
            {
                if (_value == value) return;
                _value = value;
                if (isInput) return;
                ValueChanged?.Invoke(value);

            }
        }
        private IOComponent ioComponent;

        public IOChannel(string nameChannel, IOChannelType typeChannel, IOComponent ioc)
        {
            ChannelName = nameChannel;
            ChannelType = typeChannel;
            ioComponent = ioc;
        }
        /// <summary>
        /// >Корректное удаление канала
        /// </summary>
        /// <returns></returns>
        public bool Remove()
        {
            bool resDisc = Disconnect(out string er);
            return ioComponent.Remove(this);

        }

        /// <summary>
        /// >Корректное отключение канала от связи
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public bool Disconnect(out string error)
        {
            error = "";
            if (isInput)
            {
                bool res = SignalLinkManager.Instance.DisconnectInput(this, out string er);
                er = error;
                return res;
            }
            else if (isOutput)
            {
                bool res = SignalLinkManager.Instance.DisconnectOutput(this, out string er);
                er = error;
                return res;
            }
            else
            {
                throw new Exception("Канал не является ни входным, ни выходным!");
            }
        }

        public bool isInput => ChannelType == IOChannelType.DigitalInput ? true : false;
        public bool isOutput => ChannelType == IOChannelType.DigitalOutput ? true : false;


    }
}
#endif