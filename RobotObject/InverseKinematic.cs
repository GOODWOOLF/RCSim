using Godot;
using System;
using System.Collections.Generic;
using System.Numerics;
/// <summary>
/// Обратная кинематика: 8 конфигураций по 6 углов.
/// Порто с Unity на Godot 4 (.NET). Математика без изменений.
/// </summary>
/// <summary>
/// обратная кинематика: 8 конфигураций по 6 углов
/// </summary>
public class InverseKinematic 
{
    /// <summary>
    /// целевая позиция
    /// </summary>
    private Godot.Vector3 Position;
    /// <summary>
    /// матрица целевого вращения
    /// </summary>
    private Basis RotateMatrix;

    /// <summary>
    /// Результат вычисления: 8 конфигурации по 6 углов
    /// </summary>
    private  List<float[]> Angles = new();
    /// <summary>
    /// Инициаоизация
    /// </summary>
     public InverseKinematic()
    {
        Position = Godot.Vector3.Zero;
        RotateMatrix = Basis.Identity;
        for (byte i = 0; i<=7; i++)
        {
            Angles.Add(new float[6]);
        }
    }

    /// <summary>
    /// элемент ориентации для осей 4 и 6
    /// </summary>
    /// <param name="conf"></param>
    /// <returns></returns>
    public float mm(int conf)
    {
        return RotateMatrix[0, 2] * ss23(conf) * cc1(conf) + RotateMatrix[1, 2] * ss23(conf) * ss1(conf) + RotateMatrix[2, 2] * cc23(conf);
    }
    /// <summary>
    /// синус первого угла
    /// </summary>
    /// <param name="conf"></param>
    /// <returns></returns>
    public float ss1(int conf)
    {
        return Mathf.Sin(Angles[conf][0]);
    }
    /// <summary>
    /// косинус первого угла
    /// </summary>
    /// <param name="conf"></param>
    /// <returns></returns>
    public float cc1(int conf)
    {
        return Mathf.Cos(Angles[conf][0]);
    }
    /// <summary>
    /// синус суммы второго и третьего углов
    /// </summary>
    /// <param name="conf"></param>
    /// <returns></returns>
    public float ss23(int conf)
    {
        return Mathf.Sin(Angles[conf][1] + Angles[conf][2]);
    }
    /// <summary>
    /// косинус суммы второго и третьего углов
    /// </summary>
    /// <param name="conf"></param>
    /// <returns></returns>
    public float cc23(int conf)
    {
        return Mathf.Cos(Angles[conf][1] + Angles[conf][2]);
    }
    /// <summary>
    /// числитель четвёртого угла
    /// </summary>
    /// <param name="conf"></param>
    /// <returns></returns>
    private float thetha4_numerator(int conf)
    {
        return RotateMatrix[0, 2] * cc23(conf) * cc1(conf) + RotateMatrix[1, 2] * cc23(conf) * ss1(conf) - RotateMatrix[2, 2] * ss23(conf);
    }
    /// <summary>
    /// знаменатель четвёртого угла
    /// </summary>
    /// <param name="conf"></param>
    /// <returns></returns>
    private float thetha4_denominator(int conf)
    {
        return RotateMatrix[1, 2] * cc1(conf) - RotateMatrix[0, 2] * ss1(conf);
    }
    /// <summary>
    /// числитель шестого угла
    /// </summary>
    /// <param name="conf"></param>
    /// <returns></returns>
    private float thetha6_numerator(int conf)
    {
        return -RotateMatrix[0, 0] * ss23(conf) * cc1(conf) - RotateMatrix[1, 0] * ss23(conf) * ss1(conf) - RotateMatrix[2, 0] * cc23(conf);
    }
    /// <summary>
    /// знаменатель шестого угла
    /// </summary>
    /// <param name="conf"></param>
    /// <returns></returns>
    private float thetha6_denominator(int conf)
    {
        return RotateMatrix[0, 1] * ss23(conf) * cc1(conf) + RotateMatrix[1, 1] * ss23(conf) * ss1(conf) + RotateMatrix[2, 1] * cc23(conf);
    }
    /// <summary>
    /// расчет кинематики для точки в системе робота (нужна позиция фланца)
    /// </summary>
    /// <param name="RP">размеры осей робота</param>
    /// <param name="position">позиция фланца робота</param>
    /// <param name="rotation">фращение вокруг точки в центре фланца</param>
    /// <returns></returns>
    public List<float[]> IKCalc(JointsSize RP, Godot.Vector3 position, Godot.Quaternion rotation)
    {
        return IK(RP, position, rotation);
    }
    /// <summary>
    /// Расчет ИК для 6-осевого робота по заданной позиции и ориентации эффектора, возвращает массив из 8 конфигураций ИК.
    /// </summary>
    /// <param name="RP">Параметры робота(размеры)</param>
    /// <param name="position">Целевая позиция в системме робота</param>
    /// <param name="rotation">Целевое вращение в системе робота</param>
    /// <returns>Массив из 8 конфигураций углов для заданной позиции и ориентации эффектора</returns>
    private List<float[]> IK(JointsSize RP, Godot.Vector3 position, Godot.Quaternion rotation)
    {
        Position = position;
        Godot.Vector3 C0 = new Godot.Vector3();//координаты центра запястья в системе координат робота
        RotateMatrix = new Basis(new Godot.Quaternion(x: -rotation.Z, y: -rotation.X, z: -rotation.Y, w: rotation.W));
        C0.X = Position.Z * 1000 - RP.c4 * RotateMatrix[0, 2];
        C0.Y = Position.X * 1000 - RP.c4 * RotateMatrix[1, 2];
        C0.Z = Position.Y * 1000 - RP.c4 * RotateMatrix[2, 2];

        float r = Mathf.Sqrt(C0.X * C0.X + C0.Y * C0.Y);
        float rr = r * r;
        float nx1 = Mathf.Sqrt(rr - RP.b * RP.b) - RP.a1;
        float f1 = Mathf.Atan2(RP.b, nx1 + RP.a1);
        //thetha1
        Angles[0][0] = Mathf.Atan2(C0.Y, C0.X) - f1; //i
        Angles[1][0] = Angles[0][0];  //i
        Angles[2][0] = Mathf.Atan2(C0.Y, C0.X) + f1 - Mathf.Pi; //ii
        Angles[3][0] = Angles[2][0]; //ii
        Angles[4][0] = Angles[0][0]; //i
        Angles[5][0] = Angles[0][0]; //i
        Angles[6][0] = Angles[2][0]; //ii
        Angles[7][0] = Angles[2][0]; //ii

        float k = Mathf.Sqrt(RP.a2 * RP.a2 + RP.c3 * RP.c3);
        float s1 = Mathf.Sqrt((nx1 * nx1) + ((C0.Z - RP.c1) * (C0.Z - RP.c1)));
        float s2 = MathF.Sqrt((nx1 + 2 * RP.a1) * (nx1 + 2 * RP.a1) + (C0.Z - RP.c1) * (C0.Z - RP.c1));
        float acosValue21 = (s1 * s1 + RP.c2 * RP.c2 - k * k) / (2 * s1 * RP.c2);
        float acosValue22 = (s2 * s2 + RP.c2 * RP.c2 - k * k) / (2 * s2 * RP.c2);
        //thetha2
        Angles[0][1] = (Mathf.Atan2(nx1, C0.Z - RP.c1) - Mathf.Acos(acosValue21)); //i
        Angles[1][1] = (Mathf.Atan2(nx1, C0.Z - RP.c1) + Mathf.Acos(acosValue21)); //ii       
        Angles[2][1] = -(Mathf.Atan2(nx1 + 2 * RP.a1, C0.Z - RP.c1) - Mathf.Acos(acosValue22)); //iii
        Angles[3][1] = -(Mathf.Atan2(nx1 + 2 * RP.a1, C0.Z - RP.c1) + Mathf.Acos(acosValue22)); //iv
        Angles[4][1] = Angles[0][1];//i
        Angles[5][1] = Angles[1][1];//ii
        Angles[6][1] = Angles[2][1];//iii
        Angles[7][1] = Angles[3][1];//iv

        float acosValue31 = (s1 * s1 - RP.c2 * RP.c2 - k * k) / (2 * RP.c2 * k);
        float acosValue32 = (s2 * s2 - RP.c2 * RP.c2 - k * k) / (2 * RP.c2 * k);
        // thetha3
        Angles[0][2] = Mathf.Acos(acosValue31) - Mathf.Atan2(RP.a2, RP.c3);//i
        Angles[1][2] = -Mathf.Acos(acosValue31) - Mathf.Atan2(RP.a2, RP.c3);//ii    
        Angles[2][2] = -Mathf.Atan2(RP.a2, RP.c3) - Mathf.Acos(acosValue32);//iii
        Angles[3][2] = -Mathf.Atan2(RP.a2, RP.c3) + Mathf.Acos(acosValue32);//iv
        Angles[4][2] = Angles[0][2]; //i
        Angles[5][2] = Angles[1][2]; //ii
        Angles[6][2] = Angles[2][2]; //iii
        Angles[7][2] = Angles[3][2]; //iv

        // thetha4
        Angles[0][3] = Mathf.Atan2(thetha4_denominator(0), thetha4_numerator(0));//i
        Angles[1][3] = Mathf.Atan2(thetha4_denominator(1), thetha4_numerator(1));//ii
        Angles[2][3] = Mathf.Atan2(thetha4_denominator(2), thetha4_numerator(2));//iii
        Angles[3][3] = Mathf.Atan2(thetha4_denominator(3), thetha4_numerator(3));//iv
        Angles[4][3] = Mathf.Atan2(thetha4_denominator(4), thetha4_numerator(4)) + Mathf.Pi; //v
        Angles[5][3] = Mathf.Atan2(thetha4_denominator(5), thetha4_numerator(5)) + Mathf.Pi; //vi
        Angles[6][3] = Mathf.Atan2(thetha4_denominator(6), thetha4_numerator(6)) + Mathf.Pi; //vii
        Angles[7][3] = Mathf.Atan2(thetha4_denominator(7), thetha4_numerator(7)) + Mathf.Pi; //viii

        //  thetha5
        Angles[0][4] = Mathf.Atan2(Mathf.Sqrt(1 - mm(0) * mm(0)), mm(0));//i
        Angles[1][4] = Mathf.Atan2(Mathf.Sqrt(1 - mm(1) * mm(1)), mm(1));//ii
        Angles[2][4] = Mathf.Atan2(Mathf.Sqrt(1 - mm(2) * mm(2)), mm(2));//iii
        Angles[3][4] = Mathf.Atan2(Mathf.Sqrt(1 - mm(3) * mm(3)), mm(3));//iv
        Angles[4][4] = -Mathf.Atan2(Mathf.Sqrt(1 - mm(4) * mm(4)), mm(4));//v
        Angles[5][4] = -Mathf.Atan2(Mathf.Sqrt(1 - mm(5) * mm(5)), mm(5));//vi
        Angles[6][4] = -Mathf.Atan2(Mathf.Sqrt(1 - mm(6) * mm(6)), mm(6));//vii
        Angles[7][4] = -Mathf.Atan2(Mathf.Sqrt(1 - mm(7) * mm(7)), mm(7));//viii

        //  thetha6
        Angles[0][5] = Mathf.Atan2(thetha6_denominator(0), thetha6_numerator(0));//i
        Angles[1][5] = Mathf.Atan2(thetha6_denominator(1), thetha6_numerator(1));//ii
        Angles[2][5] = Mathf.Atan2(thetha6_denominator(2), thetha6_numerator(2));//iii
        Angles[3][5] = Mathf.Atan2(thetha6_denominator(3), thetha6_numerator(3));//iv
        Angles[4][5] = Mathf.Atan2(thetha6_denominator(4), thetha6_numerator(4)) - Mathf.Pi;//v
        Angles[5][5] = Mathf.Atan2(thetha6_denominator(5), thetha6_numerator(5)) - Mathf.Pi;//vi
        Angles[6][5] = Mathf.Atan2(thetha6_denominator(6), thetha6_numerator(6)) - Mathf.Pi;//vii
        Angles[7][5] = Mathf.Atan2(thetha6_denominator(7), thetha6_numerator(7)) - Mathf.Pi;//viii

        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 6; j++)
            {
                // Перевод радиан в градусы
                float angle = Angles[i][j] * (180.0f / Mathf.Pi);

                // Нормализация в диапазон [-180; 180]
                angle = Mathf.PosMod(angle + 180.0f, 360.0f) - 180.0f;

                Angles[i][j] = angle;
            }
        }

        return Angles;
    }

    /// <summary>
    /// длины звеньев
    /// </summary>
   
    }

public class JointsSize
{
    [Export] public float a1;
    [Export] public float a2;
    [Export] public float b;
    [Export] public float c1;
    [Export] public float c2;
    [Export] public float c3;
    [Export] public float c4;
    public JointsSize(float a1, float a2, float b, float c1, float c2, float c3, float c4)
    {
        this.a1 = a1;
        this.a2 = a2;
        this.b = b;
        this.c1 = c1;
        this.c2 = c2;
        this.c3 = c3;
        this.c4 = c4;
    }
}
