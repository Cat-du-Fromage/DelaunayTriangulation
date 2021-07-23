using KaizerWaldCode.Job;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace KaizerWaldCode.System
{
    public class InitGridSystem : SystemBase
    {
        EntityQueryDesc _eventDescription;
        EntityManager _em;

        protected override void OnCreate()
        {
            _eventDescription = new EntityQueryDesc()
            {
                All = new ComponentType[] {typeof(Data.Events.Event_InitGrid)},
            };
            RequireForUpdate(GetEntityQuery(_eventDescription));
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        protected override void OnStartRunning()
        {
            Entity mapSetting = GetSingletonEntity<Data.Tag.MapSettings>();
            int MapPointPerAxis = GetComponent<Data.MapData>(mapSetting).MapPointPerAxis;
            //int PointPerAxis = GetComponent<Data.MapData>(mapSetting).ChunkPointPerAxis;
            NativeArray<float3> vertices = new NativeArray<float3> (MapPointPerAxis * MapPointPerAxis, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            Job.InitGridJob initGridJob = new InitGridJob()
            {
                MapSizeJob = GetComponent<Data.MapData>(mapSetting).MapSize,
                PointPerMeterJob = GetComponent<Data.MapData>(mapSetting).PointPerMeter,
                ChunkPointPerMeterJob = GetComponent<Data.MapData>(mapSetting).ChunkPointPerAxis,
                MapPointPerAxis = GetComponent<Data.MapData>(mapSetting).MapPointPerAxis,
                SpacingJob = GetComponent<Data.MapData>(mapSetting).PointSpacing,
                VerticesJob = vertices,
            };

            JobHandle initGridJobHandle = initGridJob.Schedule(MapPointPerAxis * MapPointPerAxis, JobsUtility.JobWorkerCount - 1);
            initGridJobHandle.Complete();

            GetBuffer<Data.Chunks.Vertices>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float3>().CopyFrom(vertices);

            vertices.Dispose();
            _em.RemoveComponent<Data.Events.Event_InitGrid>(GetSingletonEntity<Data.Tag.MapEventHolder>());

            if (GetEntityQuery(typeof(Data.Tag.MapChunk)).CalculateEntityCount() < (GetComponent<Data.MapData>(mapSetting).NumChunk* GetComponent<Data.MapData>(mapSetting).NumChunk))
            {
                _em.AddComponent<Data.Events.Event_CreateMapChunks>(GetSingletonEntity<Data.Tag.MapEventHolder>());
            }
            else
            {
                Debug.Log("Event addded");
                _em.AddComponent<Data.Events.Event_ChunksSlice>(GetSingletonEntity<Data.Tag.MapEventHolder>());
            }
            
        }

        protected override void OnUpdate()
        {

        }
    }
}
