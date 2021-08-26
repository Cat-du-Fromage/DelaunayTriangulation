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
    }
}
