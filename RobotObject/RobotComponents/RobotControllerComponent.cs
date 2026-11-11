#if TOOLS
using Godot;
using RoboSim.RobotObject;
using RoboSim.RobotObject.RobotComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RoboSim.Global;
using RoboSim.Global.Components;
using static RoboSim.RobotObject.MoveCalculate;

[Tool]
public partial class RobotControllerComponent : ControllerCellComponent, IRobotMotion
{
    RobotFactoryParamComponent _factoryParameters;
    RobotMoveParametersComponent _moveParameters;
    RobotToolsComponent _tools;
    /// <summary>
    /// >Захват
    /// </summary>
    [Export] public Node3D EndEffectorPoint;
    [Export] public float[] currentJointAngles = new float[6];
    /// <summary>
    /// >Фланец
    /// </summary>
    [Export] public Node3D Flange; 
	/// <summary>
	/// >Оси
	/// </summary>
    [Export] public Node3D[] jointsTransform;
    /// <summary>
    /// >Конфигурация
    /// </summary>
    [Export] public int NumIK;
    /// <summary>Включает проверку механических лимитов суставов при PTP и LIN.</summary>
    [Export] public bool EnforceJointLimits { get; set; } = true;
    public int CurrentIkConfiguration => NumIK;
    private InverseKinematic _ik;
    private bool JOGModeOn => GlobalState.Instance.JogMode;

    //>>>Буфер
    RobotPose oldPose = new(Vector3.Zero, Quaternion.Identity);
    //>>>Допуск
    const float PositionTolerance = 0.0001f;
    const float RotationTolerance = 0.0001f;

    //>===========================================================================================================
    /// <summary>
    /// Целевые углы управление осями из интерфейса
    /// </summary>
    public float[] targetJointAngles = new float[6] {0,0,0,0,0,0 };
    //>===========================================================================================================

    /// <summary>Возвращает заданные в компоненте производителя пределы выбранной оси.</summary>
    public (float Lower, float Upper) GetJointAngleLimits(int jointIndex)
    {
        if (_factoryParameters?.downLimit == null || _factoryParameters.upLimit == null ||
            jointIndex < 0 || jointIndex >= _factoryParameters.downLimit.Length || jointIndex >= _factoryParameters.upLimit.Length)
            throw new ArgumentOutOfRangeException(nameof(jointIndex), "Для оси не заданы пределы в компоненте параметров робота.");
        return (_factoryParameters.downLimit[jointIndex], _factoryParameters.upLimit[jointIndex]);
    }

    public override void Initialize()
    {
        _ik = new InverseKinematic();
        _factoryParameters = _parent.GetComponent<RobotFactoryParamComponent>();
        _moveParameters = _parent.GetComponent<RobotMoveParametersComponent>();
        _tools = _parent.GetComponent<RobotToolsComponent>();

        if (_factoryParameters == null) throw new Exception("_factoryParameters не получен");
        if (_moveParameters == null) throw new Exception("_moveParameters не получен");
        if (_tools == null) throw new Exception("_tools не получен");

        SetAnglesJointsTransform(new float[] { 0, 0, -90, 0, -90, 0 }, true);
        RobotPose pose = CalculateFK(_tools.CurrentTool, new float[] { 0, 0, -90, 0, -90, 0 });
        EndEffectorPoint.Position = pose.Position;
        EndEffectorPoint.Quaternion = pose.Rotation;
        oldPose.Position = pose.Position;
        oldPose.Rotation = pose.Rotation;
        _tools.ActiveToolChanged += OnActiveToolChanged;
    }

    private void OnActiveToolChanged()
    {
        if (EndEffectorPoint == null || _tools == null || _factoryParameters == null || _tools.EndEffectorOffset.Count == 0)
            return;

        // Суставы остаются на месте; переносим только TCP-маркер в рамку нового инструмента.
        RobotPose tcpPose = CalculateFK(_tools.CurrentTool, currentJointAngles);
        EndEffectorPoint.Position = tcpPose.Position;
        EndEffectorPoint.Quaternion = tcpPose.Rotation;
        oldPose.Position = tcpPose.Position;
        oldPose.Rotation = tcpPose.Rotation;
        currentJointAngles.CopyTo(targetJointAngles, 0);
    }

    public override void _ExitTree()
    {
        if (_tools != null) _tools.ActiveToolChanged -= OnActiveToolChanged;
        base._ExitTree();
    }

    public override void _Process(double delta)
    {
        if (!(GlobalState.Instance.SimulationStatus == SimulationStatus.Live) || !JOGModeOn) return;
        if (EndEffectorPoint == null) return;
        if (!currentJointAngles.SequenceEqual(targetJointAngles))
        {

            SetAnglesJointsTransform(targetJointAngles, false);
            RobotPose pose = CalculateFK(_tools.CurrentTool, targetJointAngles);
            EndEffectorPoint.Position = pose.Position;
            EndEffectorPoint.Quaternion = pose.Rotation;
            oldPose.Position = pose.Position;
            oldPose.Rotation = pose.Rotation;
            try { NumIK = FindClosestIkConfiguration(pose, _tools.CurrentTool); }
            catch (InvalidOperationException) { /* Сохраняем текущую конфигурацию, если IK не вернул допустимые решения. */ }
            return;
        }
        // 1. Получаем целевую позицию и ориентацию из узла 
        Vector3 targetPosition = EndEffectorPoint.Position ; // Позиция в локальных координатах
        Quaternion targetRotation = EndEffectorPoint.Quaternion;
        // 2. Проверка изменилась ли позиция
        if (targetPosition.DistanceTo(oldPose.Position) < PositionTolerance && targetRotation.AngleTo(oldPose.Rotation) < RotationTolerance) return;
        //следование
        JOG(new RobotPose(targetPosition, targetRotation));


    }

    /// <summary>
    /// Преобразует углы IK в ближайшие непрерывные углы относительно текущего положения,
    /// не позволяя отдельной оси сделать скачок на 360 градусов или пересечь свой лимит.
    /// </summary>
    private bool TryConvertIkAnglesContinuously(float[] rawAngles, float[] referenceAngles, out float[] converted, out string error)
    {
        converted = null;
        error = null;
        if (rawAngles == null || referenceAngles == null || _factoryParameters.downLimit == null || _factoryParameters.upLimit == null ||
            rawAngles.Length != referenceAngles.Length || rawAngles.Length != _factoryParameters.downLimit.Length || rawAngles.Length != _factoryParameters.upLimit.Length)
        {
            error = "Массивы углов IK и лимитов осей имеют разные размеры.";
            return false;
        }

        converted = new float[rawAngles.Length];
        for (int joint = 0; joint < rawAngles.Length; joint++)
        {
            float raw = rawAngles[joint];
            if (float.IsNaN(raw) || float.IsInfinity(raw) || float.IsNaN(referenceAngles[joint]) || float.IsInfinity(referenceAngles[joint]))
            {
                converted = null;
                error = $"IK вернул некорректный угол для оси {joint + 1}.";
                return false;
            }

            float lower = _factoryParameters.downLimit[joint];
            float upper = _factoryParameters.upLimit[joint];
            int minTurns = EnforceJointLimits
                ? Mathf.CeilToInt((lower - raw) / 360f)
                : Mathf.RoundToInt((referenceAngles[joint] - raw) / 360f);
            int maxTurns = EnforceJointLimits
                ? Mathf.FloorToInt((upper - raw) / 360f)
                : minTurns;
            float closest = 0f;
            float closestDelta = float.PositiveInfinity;
            for (int turns = minTurns; turns <= maxTurns; turns++)
            {
                float candidate = raw + turns * 360f;
                float delta = candidate - referenceAngles[joint];
                if (Mathf.Abs(delta) > 180.0001f) continue;
                if (Mathf.Abs(delta) < closestDelta)
                {
                    closest = candidate;
                    closestDelta = Mathf.Abs(delta);
                }
            }

            if (float.IsPositiveInfinity(closestDelta))
            {
                int nearestTurns = Mathf.RoundToInt((referenceAngles[joint] - raw) / 360f);
                float continuousTarget = raw + nearestTurns * 360f;
                if (continuousTarget > upper)
                    error = $"Ось {joint + 1} достигла верхнего предела {upper:0.###}°. Для непрерывного движения требуется {continuousTarget:0.###}°.";
                else if (continuousTarget < lower)
                    error = $"Ось {joint + 1} достигла нижнего предела {lower:0.###}°. Для непрерывного движения требуется {continuousTarget:0.###}°.";
                else
                    error = $"Для оси {joint + 1} IK требует скачок больше 180° за один шаг.";
                converted = null;
                return false;
            }

            converted[joint] = closest;
        }

        return true;
    }

    private RobotPose ResolveTool(int toolIndex)
    {
        if (toolIndex < 0) return _tools.CurrentTool;
        if (_tools.EndEffectorOffset == null || toolIndex >= _tools.EndEffectorOffset.Count)
            throw new InvalidOperationException($"Инструмент с индексом {toolIndex} не найден.");
        return _tools.EndEffectorOffset[toolIndex];
    }

    /// <summary>Решает одну явно заданную конфигурацию IK и разворачивает её углы непрерывно.</summary>
    private float[] SolveIk(RobotPose tcpPose, RobotPose tool, int configuration)
    {
        if (configuration < 0 || configuration >= 8)
            throw new InvalidOperationException("Для линейного движения нужно явно выбрать конфигурацию IK от 1 до 8.");

        RobotPose flangePose = CalcPoseFlanceTargetPoint(tcpPose, tool);
        var solutions = _ik.IKCalc(_factoryParameters.jointsSize, flangePose.Position, flangePose.Rotation);
        if (configuration >= solutions.Count)
            throw new InvalidOperationException($"Конфигурация IK {configuration + 1} не возвращена для этой позы.");
        if (!TryConvertIkAnglesContinuously(solutions[configuration], currentJointAngles, out float[] converted, out string error))
            throw new InvalidOperationException(error ?? $"Конфигурация IK {configuration + 1} недостижима.");
        return converted;
    }

    /// <summary>Находит ближайшую к текущим суставным углам конфигурацию для создания LIN-точки.</summary>
    public int FindClosestIkConfiguration(RobotPose tcpPose, RobotPose tool)
    {
        RobotPose flangePose = CalcPoseFlanceTargetPoint(tcpPose, tool ?? _tools.CurrentTool);
        var solutions = _ik.IKCalc(_factoryParameters.jointsSize, flangePose.Position, flangePose.Rotation);
        int bestConfiguration = -1;
        float bestCost = float.PositiveInfinity;
        for (int configuration = 0; configuration < solutions.Count; configuration++)
        {
            if (!TryConvertIkAnglesContinuously(solutions[configuration], currentJointAngles, out float[] angles, out _)) continue;
            float cost = 0f;
            for (int joint = 0; joint < angles.Length; joint++)
            {
                float delta = angles[joint] - currentJointAngles[joint];
                cost += delta * delta;
            }
            if (cost < bestCost) { bestCost = cost; bestConfiguration = configuration; }
        }
        if (bestConfiguration < 0) throw new InvalidOperationException("Для текущей позы не найдено достижимой конфигурации IK.");
        return bestConfiguration;
    }

    private float[] SolveLinPoseOrStop(
        RobotPose startPose,
        RobotPose endPose,
        float progress,
        float lastValidProgress,
        RobotPose tool,
        int ikConfiguration)
    {
        RobotPose Interpolate(float amount) => new(
            startPose.Position.Lerp(endPose.Position, amount),
            startPose.Rotation.Slerp(endPose.Rotation, amount));

        try
        {
            return SolveIk(Interpolate(progress), tool, ikConfiguration);
        }
        catch (InvalidOperationException)
        {
            // На ошибке уточняем границу достижимой части LIN, чтобы остановка
            // произошла у лимита, а не на предыдущем грубом шаге физики.
            float low = lastValidProgress;
            float high = progress;
            float[] lastValidAngles = (float[])currentJointAngles.Clone();
            for (int iteration = 0; iteration < 16; iteration++)
            {
                float middle = (low + high) * 0.5f;
                try
                {
                    lastValidAngles = SolveIk(Interpolate(middle), tool, ikConfiguration);
                    low = middle;
                }
                catch (InvalidOperationException)
                {
                    high = middle;
                }
            }

            SetAnglesJointsTransform(lastValidAngles, true, true, tool);
            throw;
        }
    }
    /// <summary>
    /// Следование роботом за JOG
    /// </summary>
    private void JOG(RobotPose newPose)
    {  

        // Точка относительно системы робота
        float[] ang;
        try { ang = SolveIk(newPose, _tools.CurrentTool, NumIK); }
        catch (InvalidOperationException)
        {
            EndEffectorPoint.Position = oldPose.Position;
            EndEffectorPoint.Quaternion = oldPose.Rotation;
            return;
        }
        SetAnglesJointsTransform(ang, true);
        oldPose.Position = newPose.Position;
        oldPose.Rotation = newPose.Rotation;
    }
    /// <summary>Находит позу фланца, если TCP-поза и инструмент заданы в локальной системе робота.</summary>
    private RobotPose CalcPoseFlanceTargetPoint(RobotPose targetTcp, RobotPose eeo)
    {
        Quaternion flangeInBase = targetTcp.Rotation * eeo.Rotation.Inverse();
        Vector3 flangeInBasePos = targetTcp.Position - flangeInBase * eeo.Position;

        return new RobotPose(flangeInBasePos, flangeInBase);
    }

    private RobotPose GetTcpPose(RobotPose tool)
    {
        RobotPose flange = GetFlanceCurrentPosition();
        return new RobotPose(flange.Position + flange.Rotation * tool.Position, flange.Rotation * tool.Rotation);
    }

    /// <summary>Возвращает текущую TCP-позу для активного инструмента.</summary>
    public RobotPose GetCurrentTcpPose() => GetTcpPose(_tools.CurrentTool);

    /// <summary>Возвращает текущую TCP-позу для указанного инструмента.</summary>
    public RobotPose GetCurrentTcpPose(RobotPose tool) => GetTcpPose(tool ?? _tools.CurrentTool);

    /// <summary>Переключает текущую IK-конфигурацию, сохраняя текущую TCP-позу.</summary>
    public bool TrySetCurrentIkConfiguration(int configuration, out string error)
    {
        error = null;
        if (configuration < 0 || configuration >= 8)
        {
            error = "Номер конфигурации IK должен быть от 1 до 8.";
            return false;
        }

        try
        {
            RobotPose tcpPose = GetCurrentTcpPose();
            float[] angles = SolveIk(tcpPose, _tools.CurrentTool, configuration);
            NumIK = configuration;
            SetAnglesJointsTransform(angles, true, true, _tools.CurrentTool);
            oldPose.Position = EndEffectorPoint.Position;
            oldPose.Rotation = EndEffectorPoint.Quaternion;
            return true;
        }
        catch (InvalidOperationException exception)
        {
            error = exception.Message;
            return false;
        }
    }
    /// <summary>
    /// Возвращает текущую позу фланца робота в локале
    /// </summary>
    private RobotPose GetFlanceCurrentPosition()
    {
        Quaternion robotRotation =
            _parent.GlobalTransform.Basis.GetRotationQuaternion();

        Quaternion flangeRotation =
            Flange.GlobalTransform.Basis.GetRotationQuaternion();

        return new RobotPose(
            GlobalTransform.AffineInverse() * Flange.GlobalPosition,

            robotRotation.Inverse() * flangeRotation
        );
    }
    /// <summary>
    /// Перемещение суставов на переданные углы
    /// </summary>
    private void SetAnglesJointsTransform(float[] angles,bool updateTargetAngles, bool updateEndEffector = false, RobotPose tool = null)
    {
        for (byte i = 0; i <= 5; i++)
        {
            jointsTransform[i].Quaternion = new Quaternion(_factoryParameters.jointsDirect[i],Mathf.DegToRad(angles[i]));
        }

        angles.CopyTo(currentJointAngles, 0);

        if (updateTargetAngles)
        {
            angles.CopyTo(targetJointAngles, 0);
        }

        RobotPose pose = CalculateFK(tool ?? _tools.CurrentTool, angles);
        if (updateEndEffector)
        {
            EndEffectorPoint.Position = pose.Position;
            EndEffectorPoint.Quaternion = pose.Rotation;
        }
        
    }
    /// <summary>
    /// Прямая кинематика: позиция захвата по переданным значениям в локале
    /// </summary>
    private RobotPose CalculateFK(RobotPose EEO,float[] jointsAngles){
        float[] bufJointsAngles = new float[6];

        currentJointAngles.CopyTo(bufJointsAngles, 0);
        // Устанавливаем переданные углы
        for (byte i = 0; i <= 5; i++)
        {
            jointsTransform[i].Quaternion =
                new Quaternion(_factoryParameters.jointsDirect[i].Normalized(),Mathf.DegToRad(jointsAngles[i]));
        }

        Quaternion robotRotation = _parent.GlobalTransform.Basis.GetRotationQuaternion();

        Quaternion flangeRotation =
            Flange.GlobalTransform.Basis.GetRotationQuaternion();

        RobotPose anglesFK = new RobotPose(GlobalTransform.AffineInverse() * Flange.GlobalPosition + robotRotation.Inverse() * flangeRotation * EEO.Position,
            EEO.Rotation * robotRotation.Inverse() * flangeRotation
            );

        // Восстанавливаем исходные углы
        for (byte i = 0; i <= 5; i++)
        {
            jointsTransform[i].Quaternion =
                new Quaternion(
                    _factoryParameters.jointsDirect[i].Normalized(),
                    Mathf.DegToRad(
                        bufJointsAngles[i]
                        )
                    );
        }

        return anglesFK;
    }
    /// <summary>Вычисляет пройденное расстояние для заданного профиля ускорения и торможения.</summary>
    private float CalculateProfileDistance(
    MoveParameters param,
    float time,
    float acceleration,
    float braking)
    {
        time = Mathf.Clamp(time, 0f, param.timeTotal);

        if (param.isTriangularProfile)
        {
            if (time <= param.timeAcceler)
            {
                // Ускорение
                return 0.5f * acceleration * time * time;
            }

            // Торможение
            float brakeTime = time - param.timeAcceler;

            return param.distanceAcceler
                 + param.linearSpeed * brakeTime
                 - 0.5f * braking * brakeTime * brakeTime;
        }

        if (time <= param.timeAcceler)
        {
            // Ускорение
            return 0.5f * acceleration * time * time;
        }

        if (time <= param.timeAcceler + param.timeLinear)
        {
            // Постоянная скорость
            float linearTime = time - param.timeAcceler;

            return param.distanceAcceler
                 + param.linearSpeed * linearTime;
        }

        // Торможение
        float tBrake = time
                     - param.timeAcceler
                     - param.timeLinear;

        return param.distanceAcceler
             + param.distanceLinear
             + param.linearSpeed * tBrake
             - 0.5f * braking * tBrake * tBrake;
    }
    /// <summary>
    /// Расчёт PTP движения.
    /// Движение выполняется только по шести осям робота.
    /// </summary>
    private async Task CalculatePTPMove(float[] angles, RobotProgrammContext rpc, float speedPercent = 100f, RobotPose tool = null)
    {
        GD.Print("Старт расчёта PTP");

        float[] startAngles = new float[6];

        currentJointAngles.CopyTo(startAngles, 0);


        float[] endAngles = new float[6];
        angles.CopyTo(endAngles, 0);
        List <MoveParameters> jointsParameters = new();

        float maxTime = 0f;
        int numJointLong = 0;

        // Расчёт параметров движения каждой оси
        float speedScale = Mathf.Clamp(speedPercent, 1f, 100f) / 100f;
        for (int i = 0; i < 6; i++)
        {
            MoveParameters param = MoveCalculate.CalculateMove(_moveParameters.jointsSpeedLimit[i] * speedScale, _moveParameters.jointsAccelerLimit[i] * speedScale,
                _moveParameters.jointsBrakeLimit[i] * speedScale, startAngles[i],endAngles[i]);

            jointsParameters.Add(param);

            if (param.timeTotal > maxTime)
            {
                maxTime = param.timeTotal;
                numJointLong = i;
            }
        }

        await SimulationPTPMove(
            jointsParameters,
            startAngles,
            endAngles,
            numJointLong,
            rpc,
            tool ?? _tools.CurrentTool);
    }
    private async Task SimulationPTPMove(
    List<MoveParameters> jointsParameters,
    float[] startAngles,
    float[] endAngles,
    int numJointLong,
    RobotProgrammContext rpc,
    RobotPose tool)
    {
        float t = 0f;

        MoveParameters slowParam = jointsParameters[numJointLong];

        while (t < slowParam.timeTotal)
        {
            rpc.Cancellation.Token.ThrowIfCancellationRequested();
            if (!rpc.IsRunning())
            {
                return;
            }

            float slowAngleJoint = CalculateProfileDistance(
                slowParam,
                t,
                _moveParameters.jointsAccelerLimit[numJointLong],
                _moveParameters.jointsBrakeLimit[numJointLong]);

            float percentMove = 0f;

            if (Mathf.Abs(slowParam.distanceTotal) > 0.00001f)
            {
                percentMove = Mathf.Clamp(
                    Mathf.Abs(slowAngleJoint / slowParam.distanceTotal),
                    0f,
                    1f);
            }

            float[] computedAngles = new float[6];

            for (int i = 0; i < 6; i++)
            {
                MoveParameters param = jointsParameters[i];

                if (i == numJointLong)
                {
                    computedAngles[i] =
                        startAngles[i]
                        + slowAngleJoint * param.directRotate;
                }
                else
                {
                    computedAngles[i] =
                        startAngles[i]
                        + param.distanceTotal
                        * percentMove
                        * param.directRotate;
                }
            }

            SetAnglesJointsTransform(computedAngles, true, true, tool);

            await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);

            t += (float)GetPhysicsProcessDeltaTime();
        }

        // Точная конечная позиция
        SetAnglesJointsTransform(endAngles, true, true, tool);

        GD.Print("Конец движения PTP");
    }
    /// <summary>Выполняет TCP-линейную траекторию только в явно заданной конфигурации IK.</summary>
    public async Task CalculateLINMove(RobotPose targetPositionAndOrientation, RobotProgrammContext rpc, float speedMetersPerSecond, RobotPose tool, int ikConfiguration)
    {
        GD.Print("Старт расчёта LIN");
        if (speedMetersPerSecond <= 0f)
            throw new ArgumentOutOfRangeException(nameof(speedMetersPerSecond), "Скорость LIN должна быть больше 0 м/с.");
        if (ikConfiguration < 0 || ikConfiguration >= 8)
            throw new InvalidOperationException("Для LIN нужно выбрать конфигурацию IK от 1 до 8.");

        tool ??= _tools.CurrentTool;
        RobotPose endPose = targetPositionAndOrientation;

        RobotPose startPose = GetTcpPose(tool);

        float distance = startPose.Position.DistanceTo(endPose.Position);

        // Чистое вращение
        if (distance < 0.00001f)
        {
            float angleRad = startPose.Rotation.AngleTo(endPose.Rotation);

            MoveParameters rotParam = MoveCalculate.CalculateMove(
                _moveParameters.angularSpeedDefault * Mathf.DegToRad(1f),
                _moveParameters.angularAccelerDefault * Mathf.DegToRad(1f),
                _moveParameters.angularBrakeDefault * Mathf.DegToRad(1f),
                0f,
                angleRad);

            await SimulationLINRotate(
                rotParam,
                startPose,
                endPose,
                rpc,
                tool,
                ikConfiguration);
        }
        else
        {
            MoveParameters moveParam = MoveCalculate.CalculateMove(
                speedMetersPerSecond,
                _moveParameters.linearAccelerDefault,
                _moveParameters.linearBrakeDefault,
                0f,
                distance);

            await SimulationLINMove(
                moveParam,
                rpc,
                startPose,
                endPose,
                tool,
                ikConfiguration);
        }
    }

    /// <summary>Выполняет заданную точку единым маршрутом для программы и менеджера точек.</summary>
    public async Task<NodeStatus> ExecuteMotionAsync(
        WayPoint point,
        RobotMotionType motionType,
        float speed,
        int toolIndex,
        int ikConfiguration,
        RobotProgrammContext context)
    {
        if (point == null) return context.Fail($"{motionType}: точка не задана.");
        try
        {
            context.Cancellation.Token.ThrowIfCancellationRequested();
            RobotPose tool = ResolveTool(toolIndex);
            switch (motionType)
            {
                case RobotMotionType.PTP:
                    context.ResetLinearConfigurationSequence();
                    float[] targetAngles = ValidatePtpPointAngles(point);
                    await CalculatePTPMove(targetAngles, context, speed, tool);
                    break;
                case RobotMotionType.LIN:
                    string configurationError = context.ValidateLinearConfiguration(ikConfiguration, point.Name);
                    if (configurationError != null) return context.Fail(configurationError);
                    await CalculateLINMove(point.GetPose(), context, speed, tool, ikConfiguration);
                    break;
                default:
                    return context.Fail($"Неизвестный тип движения: {motionType}.");
            }

            return context.IsRunning() ? NodeStatus.Success : NodeStatus.Failure;
        }
        catch (OperationCanceledException) { return NodeStatus.Failure; }
        catch (Exception error) { return context.Fail($"{motionType} {point.Name}: {error.Message}"); }
    }

    /// <summary>Проверяет сохранённые суставные углы точки PTP без вызова обратной кинематики.</summary>
    /// <summary>Проверяет сохранённые суставные углы точки PTP без вызова обратной кинематики.</summary>
    private float[] ValidatePtpPointAngles(WayPoint point)
    {
        if (!point.HasStoredAngles)
            throw new InvalidOperationException($"У точки '{point.Name}' нет сохранённых суставных углов. Обновите или пересоздайте точку.");
        float[] angles = point.GetCopyAngles();
        if (angles == null || angles.Length != 6)
            throw new InvalidOperationException($"У точки '{point.Name}' не сохранены шесть суставных углов.");

        for (int joint = 0; joint < angles.Length; joint++)
        {
            float angle = angles[joint];
            if (float.IsNaN(angle) || float.IsInfinity(angle))
                throw new InvalidOperationException($"У точки '{point.Name}' некорректный угол оси {joint + 1}.");
            if (EnforceJointLimits && (angle < _factoryParameters.downLimit[joint] || angle > _factoryParameters.upLimit[joint]))
                throw new InvalidOperationException(
                    $"PTP-точка '{point.Name}': угол оси {joint + 1} ({angle:0.###}°) выходит за лимит " +
                    $"[{_factoryParameters.downLimit[joint]:0.###}°; {_factoryParameters.upLimit[joint]:0.###}°].");
        }

        return angles;
    }
    private async Task SimulationLINMove(
    MoveParameters moveParam,
    RobotProgrammContext rpc,
    RobotPose startPose,
    RobotPose endPose,
    RobotPose tool,
    int ikConfiguration)
    {
        float t = 0f;
        float lastValidProgress = 0f;

        while (t < moveParam.timeTotal)
        {
            rpc.Cancellation.Token.ThrowIfCancellationRequested();

            float currentWay = CalculateProfileDistance(
                moveParam,
                t,
                _moveParameters.linearAccelerDefault,
                _moveParameters.linearBrakeDefault);

            float percentMove = 0f;

            if (Mathf.Abs(moveParam.distanceTotal) > 0.00001f)
            {
                percentMove = Mathf.Clamp(
                    currentWay / moveParam.distanceTotal,
                    0f,
                    1f);
            }

            // Интерполяция позиции и ориентации
            float[] ang = SolveLinPoseOrStop(
                startPose, endPose, percentMove, lastValidProgress, tool, ikConfiguration);
            SetAnglesJointsTransform(ang, true,true,tool);
            lastValidProgress = percentMove;

            await ToSignal(
                GetTree(),
                SceneTree.SignalName.PhysicsFrame);

            t += (float)GetPhysicsProcessDeltaTime();
        }
        float[] ang1 = SolveLinPoseOrStop(
            startPose, endPose, 1f, lastValidProgress, tool, ikConfiguration);
        SetAnglesJointsTransform(ang1, true, true, tool);
    }
    private async Task SimulationLINRotate(
    MoveParameters rotParam,
    RobotPose startPose,
    RobotPose endPose,
    RobotProgrammContext rpc,
    RobotPose tool,
    int ikConfiguration)
    {
        float t = 0f;
        float lastValidProgress = 0f;

        float angularAcceleration =
            _moveParameters.angularAccelerDefault * Mathf.DegToRad(1f);

        while (t < rotParam.timeTotal)
        {
            rpc.Cancellation.Token.ThrowIfCancellationRequested();

            float currentAngle = CalculateProfileDistance(rotParam,t,angularAcceleration,angularAcceleration);

            float percentRot = 0f;

            if (Mathf.Abs(rotParam.distanceTotal) > 0.00001f)
            {
                percentRot = Mathf.Clamp(
                    currentAngle / rotParam.distanceTotal,
                    0f,
                    1f);
            }

            // Вращение инструмента без изменения позиции
            float[] ang = SolveLinPoseOrStop(
                startPose, endPose, percentRot, lastValidProgress, tool, ikConfiguration);
            SetAnglesJointsTransform(ang, true, true, tool);
            lastValidProgress = percentRot;

            await ToSignal(
                GetTree(),
                SceneTree.SignalName.PhysicsFrame);

            t += (float)GetPhysicsProcessDeltaTime();
        }
        float[] ang1 = SolveLinPoseOrStop(
            startPose, endPose, 1f, lastValidProgress, tool, ikConfiguration);
        SetAnglesJointsTransform(ang1, true, true, tool);
    }
    //>===========================================================================================================
    //
    //                                    УПРАВЛЕНИЕ ИЗ ИНТЕРФЕЙСА
    //
    //>===========================================================================================================

    public bool ProgrammRun = false;

    public void StartProgramm()
    {
        ProgrammRun = true;
    }
    public void StopProgramm()
    {
        ProgrammRun = false;
    }


}

#endif
