using System;

namespace Entities
{
    public class JointRobotPosition
    {
        public const ushort RegisterLength = 12;

        public float J1 { get; set; }
        public float J2 { get; set; }
        public float J3 { get; set; }
        public float J4 { get; set; }
        public float J5 { get; set; }
        public float J6 { get; set; }

        public JointRobotPosition(float _j1 = 0, float _j2 = 0, float _j3 = 0, float _j4 = 0, float _j5 = 0, float _j6 = 0)
        {
            J1 = _j1;
            J2 = _j2;
            J3 = _j3;
            J4 = _j4;
            J5 = _j5;
            J6 = _j6;
        }

        public static bool IsEmpty(JointRobotPosition rp)
        {
            return rp.J1 == 0 && rp.J2  == 0 && rp.J3 == 0 && rp.J4 == 0 && rp.J5 == 0 && rp.J6 == 0;
        }
    }
}
