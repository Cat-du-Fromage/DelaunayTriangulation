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
        {//REMOVE Event_RandomSamples
            ECSUtils.SystemEventRequire<Event_Voronoi, Event_InitGrid>(ref _eventDescription, ref _em);
            RequireForUpdate(GetEntityQuery(_eventDescription));
        }

        protected override void OnUpdate()
        {
            Entity mapSettings = GetSingletonEntity<Data.Tag.MapSettings>();
            int mapSize = GetComponent<Data.MapData>(mapSettings).MapSize;
            /*
            //Give Sample an ID
            using NativeArray<float3> samplePoints = GetBuffer<Data.Chunks.PoissonDiscSample>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float3>().ToNativeArray(Allocator.Persistent);
            using NativeArray<float4> samplePointsGrid = new NativeArray<float4>(samplePoints.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            VoronoiInitSamplesJob voronoiInitSamplesJob = new VoronoiInitSamplesJob()
            {
                SamplePointsJob = samplePoints,
                SamplePointsGridJob = samplePointsGrid
            };
            JobHandle voronoiInitSamplesJobHandle = voronoiInitSamplesJob.ScheduleParallel(samplePoints.Length, JobsUtility.JobWorkerCount - 1, Dependency);

            //Give Vertices the ID of the nearest Sample
            using NativeArray<float3> vertices = GetBuffer<Data.Chunks.Vertices>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float3>().ToNativeArray(Allocator.Persistent);
            using NativeArray<float4> verticesGrid = new NativeArray<float4>(vertices.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            VoronoiJob voronoiJob = new VoronoiJob()
            {
                VerticesJob = vertices,
                SamplePointsGrid = samplePointsGrid,
                VerticesGridJob = verticesGrid,
            };
            JobHandle voronoiJobHandle = voronoiJob.ScheduleParallel(vertices.Length, JobsUtility.JobWorkerCount - 1, voronoiInitSamplesJobHandle);
            voronoiJobHandle.Complete();
            GetBuffer<Data.Chunks.VoronoiGrid>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float4>().CopyFrom(verticesGrid);
            */
            
            
            using NativeArray<float3> verticesPos = GetBuffer<Data.Vertices.VertexPosition>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float3>().ToNativeArray(Allocator.Persistent);
            using NativeArray<int> verticesIndex = GetBuffer<Data.Vertices.VertexCellIndex>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<int>().ToNativeArray(Allocator.Persistent);
            using NativeArray<float2> samplesPos = GetBuffer<Data.PoissonDiscSamples.PoissonDiscPosition>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float2>().ToNativeArray(Allocator.Persistent);
            using NativeArray<float4> voronoiesGrid = new NativeArray<float4>(verticesPos.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            VoronoiCellGridJob voronoiCellGridJob = NewVoronoiTest(mapSettings, verticesIndex, verticesPos, samplesPos, voronoiesGrid);
            JobHandle voronoiCellGridJobHandle = voronoiCellGridJob.ScheduleParallel(verticesPos.Length, JobsUtility.JobWorkerCount - 1, Dependency);
            voronoiCellGridJobHandle.Complete();
            GetBuffer<Data.Chunks.VoronoiGrid>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float4>().CopyFrom(voronoiesGrid);
            
            //samplePoints.Dispose(voronoiInitSamplesJobHandle);
            //samplePointsGrid.Dispose();
            //vertices.Dispose();
            //verticesGrid.Dispose();
            _em.RemoveComponent<Event_Voronoi>(GetSingletonEntity<Data.Tag.MapEventHolder>());
            _em.RemoveComponent<Event_RandomSamples>(GetSingletonEntity<Data.Tag.MapEventHolder>());
            _em.AddComponent<Event_IslandCoast>(GetSingletonEntity<Data.Tag.MapEventHolder>());
        }

        VoronoiCellGridJob NewVoronoiTest(Entity mapSettings, NativeArray<int> verticesIndex, NativeArray<float3> verticesPos, NativeArray<float2> samplesPos, NativeArray<float4> voronoies)
        {
            return new VoronoiCellGridJob()
            {
                MapSizeJob = GetComponent<Data.MapData>(mapSettings).MapSize,
                NumCellJob = GetComponent<Data.PoissonDiscData>(mapSettings).NumCellMap,
                RadiusJob = GetComponent<Data.PoissonDiscData>(mapSettings).Radius,
                JNtArr_VerticesPos = verticesPos,
                JNtArr_SamplesPos = samplesPos,
                JNtArr_VerticesCellIndex = verticesIndex,
                JVoronoiVertices = voronoies,
            };
        }
    }
}