using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.noise;

namespace KaizerWaldCode.Job
{
    /// <summary>
    /// Process RandomJob
    /// </summary>
    [BurstCompile(CompileSynchronously = true)]
    public struct OffsetNoiseRandomJob : IJobFor
    {
        [ReadOnly] public Unity.Mathematics.Random RandomJob;
        [ReadOnly] public float2 OffsetJob;
        [WriteOnly] public NativeArray<float2> OctOffsetArrayJob;

        public void Execute(int index)
        {
            float offsetX = RandomJob.NextInt(-100000, 100000) + OffsetJob.x;
            float offsetY = RandomJob.NextInt(-100000, 100000) - OffsetJob.y;
            OctOffsetArrayJob[index] = new float2(offsetX, offsetY);
        }
    }

    /// <summary>
    /// Noise Height
    /// </summary>
    [BurstCompile(CompileSynchronously = true)]
    public struct NoiseJob : IJobFor
    {
        [ReadOnly] public int NumPointPerAxisJob;
        [ReadOnly] public int OctavesJob;
        [ReadOnly] public float LacunarityJob;
        [ReadOnly] public float PersistanceJob;
        [ReadOnly] public float ScaleJob;
        [ReadOnly] public float HeightMulJob;
        [ReadOnly] public NativeArray<float2> OctOffsetArray;


        [NativeDisableParallelForRestriction]
        [WriteOnly] public NativeArray<float> NoiseMap;

        public void Execute(int index)
        {
            float halfMapSize = math.mul(NumPointPerAxisJob, 0.5f);

            int y = (int)math.floor(index / (float)NumPointPerAxisJob);
            int x = index - math.mul(y, NumPointPerAxisJob);

            float amplitude = 1f;
            float frequency = 1f;
            float noiseHeight = 0;
            //Not needed in parallel! it's a layering of noise so it must be done contigiously
            for (int i = 0; i < OctavesJob; i++)
            {
                float sampleX = math.mul((x - halfMapSize + OctOffsetArray[i].x) / ScaleJob, frequency);
                float sampleY = math.mul((y - halfMapSize + OctOffsetArray[i].y) / ScaleJob, frequency);
                float2 sampleXY = new float2(sampleX, sampleY);

                float pNoiseValue = snoise(sampleXY);
                noiseHeight = math.mad(pNoiseValue, amplitude, noiseHeight);
                amplitude = math.mul(amplitude, PersistanceJob);
                frequency = math.mul(frequency, LacunarityJob);
            }
            //NoiseMap[index] = math.mul(math.abs(math.lerp(0, 1f, noiseHeight)), HeightMulJob);
            float noiseVal = noiseHeight;
            noiseVal = math.abs(noiseVal);
            NoiseMap[index] = math.mul(noiseVal, HeightMulJob);
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    public struct NoiseAttributionJob : IJobFor
    {
        [ReadOnly] public NativeArray<float> NoiseMapJob;
        [ReadOnly] public NativeArray<float4> VoronoiPointsJob;
        [ReadOnly] public NativeArray<float4> IslandPointsJob;

        [WriteOnly] public NativeArray<float3> VerticesJob;

        public void Execute(int index)
        {
            int indexVoronoi = (int)VoronoiPointsJob[index].w;
            if (IslandPointsJob[indexVoronoi].w == 1f)
            {
                VerticesJob[index] = new float3(VoronoiPointsJob[index].x, NoiseMapJob[index], VoronoiPointsJob[index].z);
            }
            else
            {
                VerticesJob[index] = new float3(VoronoiPointsJob[index].x, 0, VoronoiPointsJob[index].z);
            }
        }
    }
}