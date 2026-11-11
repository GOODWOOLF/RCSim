using Godot;
using System;
using System.Collections.Generic;
using System.Text;

namespace RoboSim.RobotObject
{
    /// <summary>
    /// профиль скорости: трапеция или треугольник
    /// </summary>
    static class MoveCalculate
    {
        /// <summary>
        /// Параметры движения одного сустава (трапецеидальный / треугольный профиль)
        /// </summary>
        public class MoveParameters
        {
            // Общее время движения
            public float timeTotal;          
            public float timeAcceler;        // Время разгона
            public float timeBrake;          // Время торможения
            public float timeLinear;         // Время движения с постоянной скоростью

            public float distanceTotal;      // Общий путь (угол)
            public float distanceAcceler;    // Путь на участке разгона
            public float distanceBrake;      // Путь на участке торможения
            public float distanceLinear;     // Путь на участке постоянной скорости

            public float linearSpeed;        // Скорость на линейном участке (со знаком)
            public bool isTriangularProfile;// true = треугольный профиль (нет линейного участка)
            public int directRotate;       // Направление вращения: +1 или -1

            /// <summary>
            /// Конструктор с полным набором параметров
            /// </summary>
            public MoveParameters(
                float timeTotal,
                float timeAcceler,
                float timeBrake,
                float timeLinear,
                float distanceTotal,
                float distanceAcceler,
                float distanceBrake,
                float distanceLinear,
                float linearSpeed,
                int directRotate,
                bool isTriangularProfile)
            {
                this.timeTotal = timeTotal;
                this.timeAcceler = timeAcceler;
                this.timeBrake = timeBrake;
                this.timeLinear = timeLinear;

                this.distanceTotal = distanceTotal;
                this.distanceAcceler = distanceAcceler;
                this.distanceBrake = distanceBrake;
                this.distanceLinear = distanceLinear;

                this.linearSpeed = linearSpeed;
                this.directRotate = directRotate;
                this.isTriangularProfile = isTriangularProfile;
            }
        }
        /// <summary>
        /// расчет параметров равноускоренного движения, трапеция и треугольник
        /// </summary>
        /// <param name="Speed"></param>
        /// <param name="Acceler"></param>
        /// <param name="Brake"></param>
        /// <param name="startAngle"></param>
        /// <param name="endAngle"></param>
        /// <returns></returns>
        public static MoveParameters CalculateMove(float Speed, float Acceler, float Brake, float startAngle, float endAngle)
        {
            float tAcсeler = Speed / Acceler;
            float tBrake = Speed / Brake;
            float sAcсeler = (Acceler * tAcсeler * tAcсeler) / 2;
            float sBrake = (Brake * tBrake * tBrake) / 2;
            float sLinear = 0;
            float tLinear = 0;
            float vMax = Mathf.Abs(Speed);
            float tTotal = 0;
            float sTotal = Mathf.Abs(endAngle - startAngle);
            bool isTriangularProfile = false;
            int directRotate = endAngle - startAngle < 0 ? -1 : 1;
            //триангулярный профиль скорости
            if ((sAcсeler + sBrake) > sTotal)
            {
                isTriangularProfile = true;
                vMax = Mathf.Sqrt(sTotal / ((1 / (2 * Acceler)) + (1 / (2 * Brake))));
                if (sTotal <= 1e-6f)
                {
                    vMax = 0f;
                }
                tAcсeler = vMax / Acceler;
                tBrake = vMax / Brake;
                sAcсeler = (Acceler * tAcсeler * tAcсeler) / 2;
                sBrake = (Brake * tBrake * tBrake) / 2;
            }
            //трапецеидальный профиль скорости
            else
            {
                sLinear = sTotal - (sAcсeler + sBrake);
                tLinear = sLinear / Speed;
            }
            tTotal = tAcсeler + tBrake + tLinear;

            return new MoveParameters(tTotal, tAcсeler, tBrake, tLinear, sTotal, sAcсeler, sBrake, sLinear, vMax, directRotate, isTriangularProfile);
        }
    }
}
