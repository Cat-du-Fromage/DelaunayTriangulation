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

namespace KaizerWaldCode.System
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
            
            using NativeArray<float3> vertices = GetBuffer<Data.Chunks.VerticesCellGrid>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float3>().ToNativeArray(Allocator.Persistent);
            using NativeArray<float3> samplesCell = GetBuffer<Data.Chunks.PDiscGrid>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float3>().ToNativeArray(Allocator.Persistent);
            using NativeArray<float4> voronoiesGrid = new NativeArray<float4>(vertices.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            VoronoiCellGridJob voronoiCellGridJob = NewVoronoiTest(mapSettings, vertices, samplesCell, voronoiesGrid);
            JobHandle voronoiCellGridJobHandle = voronoiCellGridJob.ScheduleParallel(vertices.Length, JobsUtility.JobWorkerCount - 1, Dependency);
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

        void DebugPlane()
        {
            Entity mapSettings = GetSingletonEntity<Data.Tag.MapSettings>();
            int mapSize = GetComponent<Data.MapData>(mapSettings).MapSize;

            Texture2D tex = new Texture2D(mapSize, mapSize);
            Material matVornoi = AssetDatabase.LoadAssetAtPath<Material>("Assets/ECSScript/Resources/Material/Voronoi.mat");
            matVornoi.mainTexture = tex;
            //Material mat = _em.GetSharedComponentData<RenderMesh>(GetComponent<Data.Authoring.Auth_ChunkPrefab>(GetSingletonEntity<Data.Tag.ChunksHolder>()).prefab).material;

            NativeArray<float2> samplePoints = GetBuffer<Data.Chunks.PoissonDiscGrid>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float2>().ToNativeArray(Allocator.TempJob);
            NativeArray<Color> colors = new NativeArray<Color>(mapSize * mapSize, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            VoronoiDebugJob voronoiJob = new VoronoiDebugJob()
            {
                MapSizeJob = GetComponent<Data.MapData>(mapSettings).MapSize,
                SamplePointsJob = samplePoints,
                VoronoiColorJob = colors,
            };
            JobHandle voronoiJobHandle = voronoiJob.ScheduleParallel(mapSize * mapSize, JobsUtility.JobWorkerCount - 1, Dependency);
            voronoiJobHandle.Complete();

            tex.SetPixels(colors.ToArray());
            tex.Apply();

            samplePoints.Dispose();
            colors.Dispose();

            Entity voronoiPrefab = GetComponent<Data.Debug.Authoring.Auth_VoronoiPrefab>(GetSingletonEntity<Data.Tag.Debug.PrefabHolder>()).prefab;
            Entity voronoiDebug = _em.Instantiate(voronoiPrefab);

            Mesh vorMesh = _em.GetSharedComponentData<RenderMesh>(voronoiDebug).mesh;
            /*
            Entities
                .WithoutBurst()
                .WithStructuralChanges()
                .WithAll<Data.Tag.MapChunk>()
                .ForEach((Entity ent, int entityInQueryIndex) =>
                {
                    Mesh _mesh = _em.GetSharedComponentData<RenderMesh>(ent).mesh;
                    _em.SetSharedComponentData(ent, new RenderMesh(){material = mat ,mesh = _mesh});
                }).Run();
            */
            _em.AddComponentData(voronoiDebug, new NonUniformScale() {Value = 5f}); //5 because default size of a plane == 10
            _em.SetSharedComponentData(voronoiDebug, new RenderMesh() { material = matVornoi, mesh = vorMesh });
        }

        VoronoiCellGridJob NewVoronoiTest(Entity mapSettings, NativeArray<float3> vertices, NativeArray<float3> samples, NativeArray<float4> voronoies)
        {
            return new VoronoiCellGridJob()
            {
                MapSizeJob = GetComponent<Data.MapData>(mapSettings).MapSize,
                NumCellJob = (int)GetComponent<Data.PoissonDiscData>(mapSettings).NumCellMap,
                RadiusJob = (int)GetComponent<Data.PoissonDiscData>(mapSettings).Radius,
                VerticesCellGridJob = vertices,
                SampleCellGridJob = samples,
                VoronoiVerticesJob = voronoies,
            };
        }
    }
}