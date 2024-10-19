using System;

namespace Entities
{
    public class RobotPosition
    {
        public const ushort RegisterLength = 12;
        public const float MovementTolerance = 0.001f;

        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public float Yaw { get; set; }
        public float Pitch { get; set; }
        public float Roll { get; set; }

        public RobotPosition()
        {
        }

        public RobotPosition(float _x = 0, float _y = 0, float _z = 0)
        {
            X = _x;
            Y = _y;
            Z = _z;
        }

        public RobotPosition(float _x = 0, float _y = 0, float _z = 0, float _yaw = 0, float _pitch = 0, float _roll = 0)
        {
            X = _x;
            Y = _y;
            Z = _z;
            Yaw = _yaw;
            Pitch = _pitch;
            Roll = _roll;
        }

        public static double CalculatePointsDistance(RobotPosition rp1, RobotPosition rp2)
        {
            float xDiff = rp1.X - rp2.X;
            float yDiff = rp1.Y - rp2.Y;
            float zDiff = rp1.Z - rp2.Z;

            return Math.Sqrt(xDiff * xDiff + yDiff * yDiff + zDiff * zDiff);
        }

        public static bool AreEqual(RobotPosition rp1, RobotPosition rp2)
        {
            bool equalX = Math.Abs(rp2.X - rp1.X) <= MovementTolerance;

            bool equalY = Math.Abs(rp2.Y - rp1.Y) <= MovementTolerance;

            bool equalZ = Math.Abs(rp2.Z - rp1.Z) <= MovementTolerance;

            return equalX && equalY && equalZ;
        }

        public static bool AreNear(RobotPosition rp1, RobotPosition rp2, float tolerance)
        {
            bool equalX = Math.Abs(rp2.X - rp1.X) <= tolerance;

            bool equalY = Math.Abs(rp2.Y - rp1.Y) <= tolerance;

            bool equalZ = Math.Abs(rp2.Z - rp1.Z) <= tolerance;

            bool equalYaw = Math.Abs(rp2.Yaw - rp1.Yaw) <= tolerance;

            bool equalPitch = Math.Abs(rp2.Pitch - rp1.Pitch) <= tolerance;

            bool equalRoll = Math.Abs(rp2.Roll - rp1.Roll) <= tolerance;

            return equalX && equalY && equalZ && equalYaw && equalPitch && equalRoll;
        }
    }
}
