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

        public static NativeArray<T> AllocNtvAry<T>(int size) where T : struct
        {
            return new NativeArray<T>(size, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        }

        public static NativeArray<T> AllocFillNtvAry<T>(int size, T val) where T : struct
        {
            NativeArray<T> a = new NativeArray<T>(size, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < size; i++) { a[i] = val; }
            return a;
        }

        public static NativeArray<T> ArrayToNativeArray<T>(T[] array,Allocator alloc = Allocator.TempJob ,NativeArrayOptions init = NativeArrayOptions.ClearMemory) where T : struct
        {
            NativeArray<T> nA = new NativeArray<T>(array.Length, alloc, init);
            nA.CopyFrom(array);
            return nA;
        }

        public static void Fill<T>(ref NativeArray<T> array, int arrayLength, T val) where T : struct
        {
            for (int i = 0; i < arrayLength; i++) { array[i] = val; }
        }
    }
}
