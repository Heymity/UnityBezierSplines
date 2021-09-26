using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Bezier
{
    public static class CubicBezier
    {
        public static Vector3 GetPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;
            float oneMinusTSqr = oneMinusT * oneMinusT;
            float tSquared = t * t;
            return
                oneMinusTSqr * oneMinusT * p0 +
                3f * oneMinusTSqr * t * p1 +
                3f * oneMinusT * tSquared * p2 +
                tSquared * t * p3;
        }

        public static Vector3 GetFirstDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            t = Mathf.Clamp01(t);
            //float oneMinusT = 1f - t;
            //return
            //    3f * oneMinusT * oneMinusT * (p1 - p0) +
            //    6f * oneMinusT * t * (p2 - p1) +
            //    3f * t * t * (p3 - p2);

            float tSquared = t * t;
            return
                p0 * ((-3 * tSquared) + (6 * t) - 3) +
                p1 * ((9 * tSquared) - (12 * t) + 3) +
                p2 * ((-9 * tSquared) + (6 * t)) +
                p3 * (3 * tSquared);
        }

        public static Vector3 GetSecondDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            t = Mathf.Clamp01(t);
            return
                p0 * ((-6 * t) + 6) +
                p1 * ((18 * t) - 12) +
                p2 * ((-18 * t) + 6) +
                p3 * (6 * t);
        }

        public static (float firstRoot, float secondRoot) GetFirstDerivativeRoots(float p0, float p1, float p2, float p3)
        {
            var a = (-3 * p0) + (9 * p1) + (-9 * p2) + (3 * p3);
            var b = (6 * p0) + (-12 * p1) + (6 * p2);
            var c = (-3 * p0) + (3 * p1);

            var discriminant = (b * b) - (4 * a * c);

            if (discriminant < 0) return (float.NaN, float.NaN);

            var sqrtDiscriminant = Mathf.Sqrt(discriminant);

            var firstRoot = (-b + sqrtDiscriminant) / (2 * a);
            var secondRoot = (-b - sqrtDiscriminant) / (2 * a);

            return (IsOnBezierRange(firstRoot) ? firstRoot : float.NaN, IsOnBezierRange(secondRoot) ? secondRoot : float.NaN);
        }

        public static bool IsOnBezierRange(float t) => t >= 0 && t <= 1;

        public static Bounds GetBoundingBox(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            var (xDerivativeRoot1, xDerivativeRoot2) = GetFirstDerivativeRoots(p0.x, p1.x, p2.x, p3.x);
            var (yDerivativeRoot1, yDerivativeRoot2) = GetFirstDerivativeRoots(p0.y, p1.y, p2.y, p3.y);
            var (zDerivativeRoot1, zDerivativeRoot2) = GetFirstDerivativeRoots(p0.z, p1.z, p2.z, p3.z);

            var x1Point = GetPoint(p0, p1, p2, p3, xDerivativeRoot1);
            var x2Point = GetPoint(p0, p1, p2, p3, xDerivativeRoot2);
            var y1Point = GetPoint(p0, p1, p2, p3, yDerivativeRoot1);
            var y2Point = GetPoint(p0, p1, p2, p3, yDerivativeRoot2);
            var z1Point = GetPoint(p0, p1, p2, p3, zDerivativeRoot1);
            var z2Point = GetPoint(p0, p1, p2, p3, zDerivativeRoot2);

            var tZero = GetPoint(p0, p1, p2, p3, 0f);
            var tOne = GetPoint(p0, p1, p2, p3, 1f);

            float[] xList = new float[] { x1Point.x, x2Point.x, tZero.x, tOne.x }.Where(value => !float.IsNaN(value)).ToArray();
            float[] yList = new float[] { y1Point.y, y2Point.y, tZero.y, tOne.y }.Where(value => !float.IsNaN(value)).ToArray();
            float[] zList = new float[] { z1Point.z, z2Point.z, tZero.z, tOne.z }.Where(value => !float.IsNaN(value)).ToArray();

            var maxX = xList.Max();
            var minX = xList.Min();
            var maxY = yList.Max();
            var minY = yList.Min();
            var maxZ = zList.Max();
            var minZ = zList.Min();

            var center = new Vector3((maxX - minX) / 2 + minX, (maxY - minY) / 2 + minY, (maxZ - minZ) / 2 + minZ);
            var size = new Vector3(maxX - minX, maxY - minY, maxZ - minZ);

            return new Bounds(center, size);
        }

        public static float GetCurvature(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            var d = GetFirstDerivative(p0, p1, p2, p3, t);
            var dd = GetSecondDerivative(p0, p1, p2, p3, t);
            var numerator = d.x * dd.y - dd.x * d.y;
            var denominator = Mathf.Pow(d.x * d.x + d.y * d.y, 3 / 2);
            if (numerator == 0) return float.NaN;
            return numerator / denominator;
        }
    }
}