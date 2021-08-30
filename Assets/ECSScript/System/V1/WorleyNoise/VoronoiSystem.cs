using System;
using KaizerWaldCode.Job;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using KaizerWaldCode.Utils;
using KaizerWaldCode.Data.Events;
using Unity.Rendering;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

namespace KaizerWaldCode.ECSSystem
{
    public class VoronoiSystem : SystemBase
    {
        private EntityQueryDesc _eventDescription;
        private EntityManager _em;

        protected override void OnCreate()
        {
            ECSUtils.SystemEventRequire<Event_Voronoi, Event_InitGrid>(ref _eventDescription, ref _em);
            RequireForUpdate(GetEntityQuery(_eventDescription));
        }

        protected override void OnUpdate()
        {
            Entity mapSettings = GetSingletonEntity<Data.Tag.MapSettings>();

            using NativeArray<float3> verticesPos = GetBuffer<Data.Vertices.VertexPosition>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float3>().ToNativeArray(Allocator.Persistent);
            using NativeArray<int> verticesIndex = GetBuffer<Data.Vertices.VertexCellIndex>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<int>().ToNativeArray(Allocator.Persistent);
            using NativeArray<float2> samplesPos = GetBuffer<Data.PoissonDiscSamples.PoissonDiscPosition>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float2>().ToNativeArray(Allocator.Persistent);
            using NativeArray<float4> voronoiesGrid = new NativeArray<float4>(verticesPos.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            VoronoiCellGridJob voronoiCellGridJob = VoronoiCalculation(mapSettings, verticesIndex, verticesPos, samplesPos, voronoiesGrid);
            JobHandle voronoiCellGridJobHandle = voronoiCellGridJob.ScheduleParallel(verticesPos.Length, JobsUtility.JobWorkerCount - 1, Dependency);
            voronoiCellGridJobHandle.Complete();

            GetBuffer<Data.Chunks.VoronoiGrid>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float4>().CopyFrom(voronoiesGrid);

            _em.RemoveComponent<Event_RandomSamples>(GetSingletonEntity<Data.Tag.MapEventHolder>());
            ECSUtils.EndEventSystem<Event_Voronoi, Event_IslandCoast>(GetSingletonEntity<Data.Tag.MapEventHolder>(),_em);
        }

        VoronoiCellGridJob VoronoiCalculation(Entity mapSettings, NativeArray<int> verticesIndex, NativeArray<float3> verticesPos, NativeArray<float2> samplesPos, NativeArray<float4> voronoies)
        {
            return new VoronoiCellGridJob()
            {
                NumCellJob = GetComponent<Data.PoissonDiscData>(mapSettings).NumCellMap,
                JNtArr_VerticesPos = verticesPos,
                JNtArr_SamplesPos = samplesPos,
                JNtArr_VerticesCellIndex = verticesIndex,
                JVoronoiVertices = voronoies,
            };
        }
    }
}