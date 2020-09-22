using System;
using VRageMath;

namespace TestingTask
{
    static class PositionHelper
    {
        public static Vector3 GetCollisionDirection(Vector3 interPos, Vector3 targetPos, Vector3 interVel, Vector3 targetVel, float interSpeed, float interRad, float targetRad, out bool isSuccess)
        {
            isSuccess = true;

            Vector3 targetDir = targetPos - interPos;
            float iSpeed2 = interSpeed;
            float tSpeed2 = SqrMagnitude(targetVel);
            float fDot1 = Vector3.Dot(targetDir, targetVel);
            float targetDist2 = SqrMagnitude(targetDir);
            float abRadDist = (interRad + targetRad) * (interRad + targetRad);
            float d = (fDot1 * fDot1) - targetDist2 * (tSpeed2 - iSpeed2) - abRadDist;

            if (d < 0)
            {
                isSuccess = false;
                return Vector3.Zero;
            }

            float sqrt = (float)Math.Sqrt(d);
            float t1 = (-fDot1 - sqrt) / targetDist2;
            float t2 = (-fDot1 + sqrt) / targetDist2;
            float time = Math.Max(t1, t2);

            if (time <= 0)
            {
                isSuccess = false;
                return Vector3.Zero;
            }
            else
            {
                return time * targetDir + targetVel;
            }
        }

        public static float GetCollisionTime(Vector3 aPos, Vector3 bPos, Vector3 aVel, Vector3 bVel, float aRad, float bRad)
        {
            Vector3 abPos = aPos - bPos;
            Vector3 abVel = aVel - bVel;
            float a = Vector3.Dot(abVel, abVel);
            float b = 2 * Vector3.Dot(abPos, abVel);
            float c = Vector3.Dot(abPos, abPos) - (aRad + bRad) * (aRad + bRad);
            float discriminant = b * b - 4 * a * c;

            float t;
            if (discriminant < 0)
            {
                t = -b / (2 * a);
                return 0;
            }
            else
            {
                float t0 = (-b + (float)Math.Sqrt(discriminant)) / (2 * a);
                float t1 = (-b - (float)Math.Sqrt(discriminant)) / (2 * a);
                t = Math.Min(t0, t1);

                if (t < 0)
                    return 0;
            }

            if (t < 0)
                t = 0;

            return t;
        }

        public static float SqrMagnitude(Vector3 a)
        {
            return a.X * a.X + a.Y * a.Y + a.Z * a.Z;
        }
    }
}
