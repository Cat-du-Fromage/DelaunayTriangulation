using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace KaizerWaldCode.Job
{
    [BurstCompile(CompileSynchronously = true)]
    public struct InitGridJob : IJobParallelFor
    {
        [ReadOnly] public int MapSizeJob;
        [ReadOnly] public int ChunkPointPerMeterJob;
        [ReadOnly] public int PointPerMeterJob;
        [ReadOnly] public int MapPointPerAxis;
        [ReadOnly] public float SpacingJob;
        [WriteOnly] public NativeArray<float3> VerticesJob;
        public void Execute(int index)
        {
            int z = (int)math.floor(index / (float)MapPointPerAxis);
            int x = index - math.mul(z, MapPointPerAxis);

            float3 pointPosition = math.mad(new float3(x, 0, z), new float3(SpacingJob, SpacingJob, SpacingJob), new float3(MapSizeJob / -2f, 0, MapSizeJob / -2f) );

            VerticesJob[index] = pointPosition;
        }
    }
}
