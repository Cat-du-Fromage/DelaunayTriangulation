using KaizerWaldCode.Job;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace KaizerWaldCode.System
{
    [UpdateAfter(typeof(CreateMeshesSystem))]
    public class VoronoiSystem : SystemBase
    {
        private EntityQueryDesc _eventDescription;
        private EntityManager _em;

        protected override void OnCreate()
        {
            _eventDescription = new EntityQueryDesc()
            {
                All = new ComponentType[] {typeof(Data.Events.Event_Voronoi)},
            };
            RequireForUpdate(GetEntityQuery(_eventDescription));
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        protected override void OnUpdate()
        {
            Entity mapSettings = GetSingletonEntity<Data.Tag.MapSettings>();
            int mapSize = GetComponent<Data.MapData>(mapSettings).MapSize;

            Texture2D tex = new Texture2D(mapSize, mapSize);
            Material mat = _em.GetSharedComponentData<RenderMesh>(GetComponent<Data.Authoring.Auth_ChunkPrefab>(GetSingletonEntity<Data.Tag.ChunksHolder>()).prefab).material;

            NativeArray<float3> samplePoints = GetBuffer<Data.Chunks.PoissonDiscSample>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float3>().ToNativeArray(Allocator.TempJob);
            NativeArray<Color> colors = new NativeArray<Color>(mapSize * mapSize, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            VoronoiJob voronoiJob = new VoronoiJob()
            {
                MapSizeJob = GetComponent<Data.MapData>(mapSettings).MapSize,
                SamplePointsJob = samplePoints,
                VoronoiColorJob = colors,
            };
            JobHandle voronoiJobHandle = voronoiJob.ScheduleParallel(mapSize * mapSize, JobsUtility.JobWorkerCount - 1, Dependency);
            voronoiJobHandle.Complete();

            tex.SetPixels(colors.ToArray());
            tex.Apply();
            mat.mainTexture = tex;

            samplePoints.Dispose();
            colors.Dispose();

            Entities
                .WithoutBurst()
                .WithStructuralChanges()
                .WithAll<Data.Tag.MapChunk>()
                .ForEach((Entity ent, int entityInQueryIndex) =>
                {
                    Mesh _mesh = _em.GetSharedComponentData<RenderMesh>(ent).mesh;
                    _em.SetSharedComponentData(ent, new RenderMesh(){material = mat ,mesh = _mesh});
                }).Run();

            _em.RemoveComponent<Data.Events.Event_Voronoi>(GetSingletonEntity<Data.Tag.MapEventHolder>());
        }
    }
}