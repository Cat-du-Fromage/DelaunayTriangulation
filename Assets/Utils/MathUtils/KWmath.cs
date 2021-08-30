using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
namespace KaizerWaldCode.Utils
{
    public static class KWmath
    {
        /// <summary>
        /// Return the determinant of 2 vector
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static float Det(float2 v1, float2 v2)
        {
            return (v1.x * v2.y) - (v1.y * v2.x);
        }

        /// <summary>
        /// Return the determinant of 2 vector
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static float Det(float v1x, float v1y, float v2x, float v2y)
        {
            return (v1x * v2y) - (v1y * v2x);
        }

        /// <summary>
        /// Return the Determinant of a 3X3 matrix
        /// </summary>
        /// <param name="v1">Lower part of the matrix (g/h/i)</param>
        /// <param name="v2">Mid part of the matrix (d/e/f)</param>
        /// <param name="v3">Top part of the matrix (a/b/c)</param>
        /// <returns></returns>
        public static float Det(float3 v1, float3 v2, float3 v3)
        {
            return v3.x * Det(float2(v2.y,v1.y), float2(v2.z, v1.z)) - v3.y * Det(float2(v2.x, v1.x), float2(v2.z, v1.z)) + v3.z * Det(float2(v2.x, v1.x), float2(v2.y, v1.y));
        }

        /// <summary>
        /// Get Radius of the circumscribed circle
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static float Circumradius(float2 a, float2 b, float2 c)
        {
            //CIRCUMCENTER!!
            float dx = b.x - a.x;
            float dy = b.y - a.y;
            float ex = c.x - a.x;
            float ey = c.y - a.y;
            float bl = dx * dx + dy * dy;
            float cl = ex * ex + ey * ey;
            float d = 0.5f / (dx*ey - dy*ex);
            //change from here
            float x = (ey * bl - dy * cl) * d;
            float y = (dx * cl - ex * bl) * d;
            return x * x + y * y;
        }

        public static float2 GetCircumcenter(float2 a, float2 b, float2 c)
        {
            float dx = b.x - a.x;
            float dy = b.y - a.y;
            //float2 AB = float2(b.x - a.x, b.y - a.y);
            float ex = c.x - a.x;
            float ey = c.y - a.y;
            //float2 AC = float2(c.x - a.x, c.y - a.y);
            float bl = dx * dx + dy * dy;
            float cl = ex * ex + ey * ey;
            float d = 0.5f / (dx * ey - dy * ex);
            float x = a.x + (ey * bl - dy * cl) * d;
            float y = a.y + (dx * cl - ex * bl) * d;
            return float2(x, y);
        }

        #region CircumCenter

        // Function to find the line given two points
        public static float3 LineFromPoint(float2 p1, float2 p2)
        {
            //float a = p2.y - p1.y;
            //float b = p1.x - p2.x;
            //float c = select(a * p1.x + b * p1.y, a * p1.x - b * p1.y, b < 0);
            //float c = a * p1.x + b * p1.y;
            return float3(p2.y - p1.y, p1.x - p2.x, select((p2.y - p1.y) * p1.x + (p1.x - p2.x) * p1.y, (p2.y - p1.y) * p1.x - (p1.x - p2.x) * p1.y, (p1.x - p2.x) < 0));
        }

        public static float3 PerpendicularBisectorFromLine(float2 p1, float2 p2, float3 abc)
        {
            float2 midPoint = float2((p1.x + p2.x) / 2, (p1.y + p2.y) / 2);

            // c = -bx + ay
            /*
            c = -abc.y * (midPoint.x) + abc.x * (midPoint.y);

            float temp = abc.x;
            a = -abc.y;
            b = temp;
            */
            return float3(-abc.y, abc.x, -abc.y * (midPoint.x) + abc.x * (midPoint.y));
        }


        // Returns the intersection point of two lines
        static float2 LineLineIntersection(float2 a, float2 b, float2 c)
        {
            //double determinant = a1 * b2 - a2 * b1;
            /*
            float determinant = Det(a,b);
            if (determinant == 0)
            {
                // The lines are parallel. This is simplified
                // by returning a pair of FLT_MAX
                return float2(Single.MaxValue, Single.MaxValue);
            }
            else
            {
                float x = Det(c,b) / determinant; //(b2 * c1 - b1 * c2) / determinant;
                float y = Det(a, c) / determinant; // (a1 * c2 - a2 * c1) / determinant;
                return float2(x, y);
            }
            */
            return select(float2(Det(c, b) / Det(a, b), Det(a, c) / Det(a, b)), float2(Single.MaxValue, Single.MaxValue), Det(a, b) == 0);
        }


        public static float2 FindCircumCenter(float2 P, float2 Q, float2 R)
        {
            // Line PQ is represented as ax + by = c
            float3 abc = LineFromPoint(P, Q);

            // Line QR is represented as ex + fy = g
            float3 efg = LineFromPoint(Q, R);

            // Converting lines PQ and QR to perpendicular
            // vbisectors. After this, L = ax + by = c
            // M = ex + fy = g
            float3 bisABC = PerpendicularBisectorFromLine(P, Q, LineFromPoint(P, Q));
            float3 bisEFG = PerpendicularBisectorFromLine(Q, R, LineFromPoint(Q, R));

            // The point of intersection of L and M gives
            // the circumcenter

            return LineLineIntersection(float2(bisABC.x, bisEFG.x), float2(bisABC.y, bisEFG.y), float2(bisABC.z, bisEFG.z));
        }
        #endregion CircumCenter
        /// <summary>
        /// return if a point c in left to the vector ab
        /// CAREFUL with direction while assigning a and b : a -> b != b -> a
        /// </summary>
        /// <param name="a">start/origin of the vector ab</param>
        /// <param name="b">end of the vector ab</param>
        /// <param name="c">point checked if positioned left to vector ab</param>
        /// <returns></returns>
        public static bool IsLeft(float2 a, float2 b, float2 c)
        {
            return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x) > 0;
        }

        /// <summary>
        /// return if a point c in right to the vector ab
        /// CAREFUL with direction while assigning a and b : a -> b != b -> a
        /// </summary>
        /// <param name="a">start/origin of the vector ab</param>
        /// <param name="b">end of the vector ab</param>
        /// <param name="c">point checked if positioned right to vector ab</param>
        /// <returns></returns>
        public static bool IsRight(float2 a, float2 b, float2 c)
        {
            return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x) < 0;
        }
    }
}
