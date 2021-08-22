using KaizerWaldCode.Job;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
namespace KaizerWaldCode.ECSSystem
{
    public class ChunksMeshDataSystem : SystemBase
    {
        EntityQueryDesc _eventDescription;
        EntityManager _em;
        private BeginSimulationEntityCommandBufferSystem BS_Ecb;

        protected override void OnCreate()
        {
            _eventDescription = new EntityQueryDesc()
            {
                All = new ComponentType[] {typeof(Data.Events.Event_ChunksMeshData) },
            };
            RequireForUpdate(GetEntityQuery(_eventDescription));
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;

            BS_Ecb = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            int mapSize = GetComponent<Data.MapData>(GetSingletonEntity<Data.Tag.MapSettings>()).MapSize;
            int chunkPoints = GetComponent<Data.MapData>(GetSingletonEntity<Data.Tag.MapSettings>()).ChunkPointPerAxis;
            int mapPoints = GetComponent<Data.MapData>(GetSingletonEntity<Data.Tag.MapSettings>()).MapPointPerAxis;

            NativeArray<float2> Uvs = new NativeArray<float2>((chunkPoints) * (chunkPoints), Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            NativeArray<int> Triangles = new NativeArray<int>(math.mul(chunkPoints-1,chunkPoints-1) * 6, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            MeshDataJob meshDataJob = new MeshDataJob()
            {
                ChunkSizeJob = GetComponent<Data.MapData>(GetSingletonEntity<Data.Tag.MapSettings>()).ChunkSize,
                ChunkPointPerAxisJob = chunkPoints,
                MapSizeJob = mapSize,
                UvsJob = Uvs,
                TrianglesJob = Triangles,
            };
            JobHandle meshDataJobHandle = meshDataJob.Schedule((chunkPoints) * (chunkPoints), JobsUtility.JobWorkerCount - 1);
            meshDataJobHandle.Complete();
            Debug.Log("Passage Mesh");
            Dependency = meshDataJobHandle;
            EntityCommandBuffer.ParallelWriter ecbBegin = BS_Ecb.CreateCommandBuffer().AsParallelWriter();
            Entities
                .WithBurst(synchronousCompilation: true)
                .WithAll<Data.Tag.MapChunk>()
                .WithReadOnly(Uvs)
                .WithReadOnly(Triangles)
                .WithDisposeOnCompletion(Uvs)
                .WithDisposeOnCompletion(Triangles)
                .ForEach((Entity ent, int entityInQueryIndex) =>
                {
                    ecbBegin.SetBuffer<Data.Chunks.Uvs>(entityInQueryIndex, ent).Reinterpret<float2>().AddRange(Uvs);
                    ecbBegin.SetBuffer<Data.Chunks.Triangles>(entityInQueryIndex, ent).Reinterpret<int>().AddRange(Triangles);
                }).ScheduleParallel();
            BS_Ecb.AddJobHandleForProducer(Dependency);
            
            _em.RemoveComponent<Data.Events.Event_ChunksMeshData>(GetSingletonEntity<Data.Tag.MapEventHolder>());
            _em.AddComponent<Data.Events.Event_CreateMesh>(GetSingletonEntity<Data.Tag.MapEventHolder>());
        }
    }
}