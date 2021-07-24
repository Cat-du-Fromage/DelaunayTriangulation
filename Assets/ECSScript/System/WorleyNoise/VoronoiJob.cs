using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace KaizerWaldCode.Job
{
    public struct VoronoiJob : IJobFor
    {
        [ReadOnly] public int MapSizeJob;

        [ReadOnly] public NativeArray<float3> SamplePointsJob;
        [WriteOnly] public NativeArray<Color> VoronoiColorJob;
        public void Execute(int index)
        {
            int y = (int)math.floor(index / (float)MapSizeJob);
            int x = index - math.mul(y, MapSizeJob);

            NativeArray<float> distances = new NativeArray<float>(SamplePointsJob.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < SamplePointsJob.Length; i++)
            {
                distances[i] = math.distance(new float2(x, y), SamplePointsJob[i].xz);
            }
            distances.Sort();
            VoronoiColorJob[index] = new Color(distances[0], distances[0], distances[0]);
            distances.Dispose();
        }
    }
}