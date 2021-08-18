using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace KaizerWaldCode.Utils
{
    /// <summary>
    /// Doesn't work yet
    /// Problem 1 : can't allocate a native array who is not initialize
    /// </summary>
    public static class NativeCollectionUtils
    {
        public static void AllocNtvArray(ref NativeArray<float3> array, int size)
        {
            array = new NativeArray<float3>(size, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        }
    }
}
