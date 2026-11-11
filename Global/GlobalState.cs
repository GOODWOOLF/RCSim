#if TOOLS
using System;
using Godot;
namespace RoboSim.Global
{
    /// <summary>
    /// Глобальные состояния RTK Studio.
    /// </summary>
    [Tool]
    public class GlobalState
    {
        private static GlobalState _instance;

        public static GlobalState Instance =>
            _instance ??= new GlobalState();

        private RotateModeEnum _rotateMode = RotateModeEnum.Global;
        private bool _jogMode;
        private SimulationStatus _simStatus = SimulationStatus.Live;

        /// <summary>
        /// Режим вращения.
        /// </summary>
        public RotateModeEnum RotateMode
        {
            get => _rotateMode;
            set
            {
                if (_rotateMode == value)
                    return;

                _rotateMode = value;
                RotateModeChanged?.Invoke(value);
            }
        }

        /// <summary>
        /// Активен ли режим JOG.
        /// </summary>
        public bool JogMode
        {
            get => _jogMode;
            set
            {
                if (_jogMode == value)
                    return;

                _jogMode = value;
                JogModeChanged?.Invoke(value);
            }
        }

        /// <summary>
        /// Текущий режим симуляции.
        /// Live — редактирование и ручное управление.
        /// Play — выполнение симуляции.
        /// </summary>
        public SimulationStatus SimulationStatus
        {
            get => _simStatus;
            set
            {
                if (_simStatus == value)
                    return;

                _simStatus = value;
                SimStatusChanged?.Invoke(value);
            }
        }

        public bool IsSimulationRunning =>
            SimulationStatus == SimulationStatus.Play;

        public bool IsLive =>
            SimulationStatus == SimulationStatus.Live;

        public event Action<RotateModeEnum> RotateModeChanged;
        public event Action<bool> JogModeChanged;
        public event Action<SimulationStatus> SimStatusChanged;
    }

    public enum RotateModeEnum
    {
        Local,
        Global
    }

    public enum SimulationStatus
    {
        Live,
        Play,
        STOP
    }
}
#endif