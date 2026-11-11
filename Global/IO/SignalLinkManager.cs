
#if TOOLS
using System.Linq;
using RoboSim.Global.Components;
using Godot;
using RoboSim.Global.Components.IOComponents;
namespace RoboSim.Global.IO
{
   
    public partial class SignalLinkManager
    {
        private static SignalLinkManager _instance;

        public static SignalLinkManager Instance =>
            _instance ??= new SignalLinkManager();

        [Export] public Godot.Collections.Array<SignalConnection> _connections = new();


        // =============================================
        // CONNECT
        // =============================================
        /// <summary>
        /// >Соденинение каналов (ОБЯЗАТЕЛЬНО: source - выходной канал, target - входной канал)
        /// </summary>
        /// <param name="source">компонент ИО</param>
        /// <param name="channelSource">выходной канал</param>
        /// <param name="target">компонент ИО</param>
        /// <param name="channelTarget">входной канал</param>
        /// <param name="error">возвращаемая ошибка</param>
        /// <returns>true - успешно, иначе - ошибка</returns>
        public bool TryConnect(IOComponent source, IOChannel channelSource,IOComponent target, IOChannel channelTarget, out string error)
        {
            error = "";

            if (!IsValid(source) || !IsValid(target))
            {
                error = " ОШИБКА источник или целевой компонент некорректны";
                return false;
            }


            if (channelSource == null || channelTarget == null)
            {
                error = "Каналы не NULL";
                return false;
            }

            if (!channelSource.isOutput || !channelTarget.isInput)
            {
                error = "Неправильное направление связи, только Выход -> Вход";
                return false;
            }

            /*if (output.IsDigital != input.IsDigital)
            {
                error = "Digital and analog channels cannot be connected.";
                return false;
            }*/

            foreach (var connection in _connections)
            {
                if (connection.SourceChannel == channelSource &&
                    connection.TargetChannel == channelTarget)
                {
                    error = "Каналы уже соединены";
                    return false;
                }

            }
            //создаем соединение
            var newConnection = new SignalConnection
            {
                Source = source,
                SourceChannel = channelSource,

                Target = target,
                TargetChannel = channelTarget
            };

            //добавляем соединение
            _connections.Add(newConnection);

            //подписка на выходной канал
            channelSource.ValueChanged += newConnection.HandleUpdateSignal;
            //синхронизация
            channelTarget.Value = channelSource.Value;

            return true;
        }

        // =============================================
        // DISCONNECT
        // =============================================

        public bool DisconnectLink(/*IOComponent source,*/IOChannel channelSource,/*IOComponent target*/IOChannel channelTarget,out string error)
        {
            error = "";

            /*IOChannel sourceOutputChannel = source.GetOutChanneltoName(channelSource);
            IOChannel targetInputChannel = target.GetInpChanneltoName(channelTarget);*/

            if (channelSource == null || channelTarget == null)
            {
                error = "Каналы не найдены";
                return false;
            }

            var connection = _connections.FirstOrDefault(c =>
                c.SourceChannel == channelSource &&
                c.TargetChannel == channelTarget);

            if (connection == null)
            {
                error = "Соединение не найдено";
                return false;
            }

            // Отписываем обработчик
            connection.SourceChannel.ValueChanged -= connection.HandleUpdateSignal;

            // Удаляем соединение
            _connections.Remove(connection);

            return true;
        }
        /// <summary>
        /// >отключение связей по входу (удаляется 1 связь)
        /// </summary>
        /// <param name="inputChannel"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public bool DisconnectInput(IOChannel inputChannel, out string error)
        {
            error = "";

            if (inputChannel == null)
            {
                error = "Некорректный канал";
                return false;
            }

            var connection = _connections.FirstOrDefault(c => c.TargetChannel == inputChannel);

            if (connection == null)
            {
                error = "Соединений с этим каналом не найдено";
                return false;
            }

            // Отписываем обработчик
            connection.SourceChannel.ValueChanged -= connection.HandleUpdateSignal;

            inputChannel.Value = false;
            // Удаляем соединение
            _connections.Remove(connection);
            return true;
        }
        /// <summary>
        /// >отключение связей по выходу (удаляются все связи)
        /// </summary>
        /// <param name="outputChannel"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public bool DisconnectOutput(IOChannel outputChannel, out string error)
        {
            error = "";

            if (outputChannel == null)
            {
                error = "Некорректный канал";
                return false;
            }

            var connection = _connections.Where(c=> c.TargetChannel == outputChannel).ToList();

            if (connection.Count == 0)
            {
                error = "Соединений с этим каналом не найдено";
                return false;
            }

            // Отписываем обработчик
            foreach(var con in connection)
            {
                con.SourceChannel.ValueChanged -= con.HandleUpdateSignal;

                con.TargetChannel.Value = false;
                // Удаляем соединение
                _connections.Remove(con);
            }
            return true;
        }


        // =============================================
        // HELPERS
        // =============================================

        private bool IsValid(IOComponent component)
        {
            return component != null &&
                   GodotObject.IsInstanceValid(component);
        }

    }
}
#endif
