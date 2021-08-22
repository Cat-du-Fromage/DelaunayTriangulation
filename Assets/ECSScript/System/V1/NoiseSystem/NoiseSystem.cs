using KaizerWaldCode.Job;
using KaizerWaldCode.Data;
using KaizerWaldCode.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Transforms;

namespace KaizerWaldCode.ECSSystem
{
    public class NoiseSystem : SystemBase
    {
        private EntityQueryDesc _eventDescription;
        private EntityManager _em;

        protected override void OnCreate()
        {
            ECSUtils.SystemEventRequire<Data.Events.Event_Noise, Data.Events.Event_RandomSamples>(ref _eventDescription, ref _em);
            RequireForUpdate(GetEntityQuery(_eventDescription));
        }

        protected override void OnUpdate()
        {
            Entity mapSetting = GetSingletonEntity<Data.Tag.MapSettings>();
            MapData mapData = GetComponent<MapData>(mapSetting);
            NoiseData noiseData = GetComponent<NoiseData>(mapSetting);

            //Offsets Randomize
            using NativeArray<float2> octavesOffsets = new NativeArray<float2>(noiseData.Octaves, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            OffsetNoiseRandomJob noiseRandomJob = OffsetNoiseRandom(octavesOffsets, noiseData);
            JobHandle noiseRandomJobHandle = noiseRandomJob.ScheduleParallel(noiseData.Octaves, JobsUtility.JobWorkerCount - 1, Dependency);

            //Noise Map
            using NativeArray<float> noiseMap = new NativeArray<float>(mapData.MapPointPerAxis * mapData.MapPointPerAxis, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NoiseJob noiseJob = NoiseMap(mapData, noiseData, octavesOffsets, noiseMap);
            JobHandle noiseJobHandle = noiseJob.ScheduleParallel(mapData.MapPointPerAxis * mapData.MapPointPerAxis, JobsUtility.JobWorkerCount - 1, noiseRandomJobHandle);

            using NativeArray<float4> voronoiPoints = GetBuffer<Data.Chunks.VoronoiGrid>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float4>().ToNativeArray(Allocator.TempJob);
            using NativeArray<float4> islandPoints = GetBuffer<Data.Chunks.IslandPoissonDisc>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float4>().ToNativeArray(Allocator.TempJob);
            using NativeArray<float3> vertices = new NativeArray<float3>(mapData.MapPointPerAxis * mapData.MapPointPerAxis, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NoiseAttributionJob noiseAttributionJob = new NoiseAttributionJob()
            {
                NoiseMapJob = noiseMap,
                VoronoiPointsJob = voronoiPoints,
                IslandPointsJob = islandPoints,
                VerticesJob = vertices,
            };
            JobHandle noiseAttributionJobHandle = noiseAttributionJob.ScheduleParallel(mapData.MapPointPerAxis * mapData.MapPointPerAxis, JobsUtility.JobWorkerCount - 1, noiseJobHandle);
            noiseAttributionJobHandle.Complete();
            GetBuffer<Data.Vertices.VertexPosition>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float3>().CopyFrom(vertices);

            _em.RemoveComponent<Data.Events.Event_Noise>(GetSingletonEntity<Data.Tag.MapEventHolder>());

            //_em.RemoveComponent<Data.Events.Event_RandomSamples>(GetSingletonEntity<Data.Tag.MapEventHolder>());
            //_em.AddComponent<Data.Events.Event_IslandCoast>(GetSingletonEntity<Data.Tag.MapEventHolder>());
        }

        OffsetNoiseRandomJob OffsetNoiseRandom(NativeArray<float2> octavesOffsets, NoiseData noiseData)
        {
            return new OffsetNoiseRandomJob()
            {
                RandomJob = new Random(noiseData.Seed),
                OffsetJob = noiseData.Offset,
                OctOffsetArrayJob = octavesOffsets,
            };
        }

        NoiseJob NoiseMap(MapData mapData, NoiseData noiseData, NativeArray<float2> octavesOffsets, NativeArray<float> noiseMap)
        {
            return new NoiseJob()
            {
                NumPointPerAxisJob = mapData.MapPointPerAxis,
                OctavesJob = noiseData.Octaves,
                LacunarityJob = noiseData.Lacunarity,
                PersistanceJob = noiseData.Persistance,
                ScaleJob = noiseData.Scale,
                HeightMulJob = noiseData.HeightMultiplier,
                OctOffsetArray = octavesOffsets,
                NoiseMap = noiseMap,
            };
        }
    }
}