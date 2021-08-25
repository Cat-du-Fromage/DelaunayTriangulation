using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
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

        #region Array conversion to NativeArray
        public static NativeArray<float3> ArrayToNativeArray(float3[] array,Allocator alloc = Allocator.TempJob ,NativeArrayOptions init = NativeArrayOptions.ClearMemory)
        {
            NativeArray<float3> nA = new NativeArray<float3>(array.Length, alloc, init);
            nA.CopyFrom(array);
            return nA;
        }
        public static NativeArray<int> ArrayToNativeArray(int[] array, Allocator alloc = Allocator.TempJob, NativeArrayOptions init = default)
        {
            NativeArray<int> nA = new NativeArray<int>(array.Length, alloc, init);
            nA.CopyFrom(array);
            return nA;
        }
        #endregion Array conversion to NativeArray
    }
}
