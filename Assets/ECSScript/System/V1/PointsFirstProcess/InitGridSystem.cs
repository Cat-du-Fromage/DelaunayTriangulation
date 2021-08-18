using KaizerWaldCode.Job;
using KaizerwaldCode.Utils;
using KaizerWaldCode.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEditor;
using System.Threading.Tasks;

namespace KaizerWaldCode.System
{
    public class InitGridSystem : SystemBase
    {
        private EntityQueryDesc _eventDescription;
        private EntityManager _em;

        protected override void OnCreate()
        {
            ECSUtils.SystemEventRequire<Data.Events.Event_InitGrid>(ref _eventDescription, ref _em);
            RequireForUpdate(GetEntityQuery(_eventDescription));
        }

        protected async override void OnStartRunning()
        {
            Entity mapSetting = GetSingletonEntity<Data.Tag.MapSettings>();
            int MapPointPerAxis = GetComponent<Data.MapData>(mapSetting).MapPointPerAxis;

            ComputeShader initGridCShader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/ECSScript/ComputeShader/FirstInitPoint.compute");

            float3[] verticesArr = new float3[MapPointPerAxis * MapPointPerAxis];

            //using NativeArray<float3> vertices = new NativeArray<float3>(MapPointPerAxis * MapPointPerAxis, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            //JobInit(ref verticesArr, mapSetting, MapPointPerAxis);
            verticesArr = await ComputeShaderInit(verticesArr, initGridCShader, mapSetting, MapPointPerAxis);
            
            GetBuffer<Data.Chunks.Vertices>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float3>().CopyFrom(verticesArr);

            JobGridCellPoints(verticesArr, mapSetting, MapPointPerAxis);
            //don't use utils here because of the editor controls that add some specific checks
            EndSystemEvent(GetSingletonEntity<Data.Tag.MapEventHolder>(), mapSetting);
        }

        async Task<float3[]> ComputeShaderInit(float3[] vertArr, ComputeShader cs, Entity mapSetting, int mapPointPerAxis)
        {
            cs.SetInt("pointPerAxis", mapPointPerAxis);
            cs.SetInt("mapSize", GetComponent<Data.MapData>(mapSetting).MapSize);
            cs.SetFloat("spacing", GetComponent<Data.MapData>(mapSetting).PointSpacing);


            using ComputeBuffer verticesBuffer = ShaderUtils.CreateAndSetBuffer<float3>(vertArr, cs, "grid");

            vertArr = await ShaderUtils.AsyncGpuRequest<float3>(cs, new int3(mapPointPerAxis, 1, mapPointPerAxis), verticesBuffer);
            ShaderUtils.Release(verticesBuffer);
            return vertArr;
        }

        void JobInit(ref float3[] vertArr, Entity mapSetting, int MapPointPerAxis)
        {
            using NativeArray<float3> vertices = new NativeArray<float3>(MapPointPerAxis * MapPointPerAxis, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            Job.InitGridJob initGridJob = new InitGridJob()
            {
                MapSizeJob = GetComponent<Data.MapData>(mapSetting).MapSize,
                PointPerMeterJob = GetComponent<Data.MapData>(mapSetting).PointPerMeter,
                ChunkPointPerMeterJob = GetComponent<Data.MapData>(mapSetting).ChunkPointPerAxis,
                MapPointPerAxis = GetComponent<Data.MapData>(mapSetting).MapPointPerAxis,
                SpacingJob = GetComponent<Data.MapData>(mapSetting).PointSpacing,
                VerticesJob = vertices,
            };

            JobHandle initGridJobHandle = initGridJob.ScheduleParallel(MapPointPerAxis * MapPointPerAxis, JobsUtility.JobWorkerCount - 1, Dependency);
            initGridJobHandle.Complete();
            vertArr = vertices.ToArray();
        }

        void JobGridCellPoints(float3[] vertArr, Entity mapSetting, int MapPointPerAxis)
        {
            using NativeArray<float3> vertices = new NativeArray<float3>(MapPointPerAxis * MapPointPerAxis, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            vertices.CopyFrom(vertArr);
            using NativeArray<float3> cellGrid = new NativeArray<float3>(MapPointPerAxis * MapPointPerAxis, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            PointGridCellJob gridCellJob = new PointGridCellJob()
            {
                MapSizeJob = GetComponent<Data.MapData>(mapSetting).MapSize,
                NumCellMap = GetComponent<Data.PoissonDiscData>(mapSetting).NumCellMap,
                MapPointPerAxis = MapPointPerAxis,
                Radius = GetComponent<Data.PoissonDiscData>(mapSetting).Radius,
                VerticesJob = vertices,
                VerticesCellGrid = cellGrid,
            };
            JobHandle gridCellJobHandle = gridCellJob.ScheduleParallel(MapPointPerAxis * MapPointPerAxis, JobsUtility.JobWorkerCount - 1, Dependency);
            gridCellJobHandle.Complete();
            GetBuffer<Data.Chunks.VerticesCellGrid>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float3>().CopyFrom(cellGrid);
        }

        /// <summary>
        /// Custom EndSystem for Editor management purpose
        /// </summary>
        /// <param name="eventHolder"></param>
        /// <param name="mapSetting"></param>
        void EndSystemEvent(Entity eventHolder, Entity mapSetting)
        {
            _em.RemoveComponent<Data.Events.Event_InitGrid>(GetSingletonEntity<Data.Tag.MapEventHolder>());
            if (GetEntityQuery(typeof(Data.Tag.MapChunk)).CalculateEntityCount() < (GetComponent<Data.MapData>(mapSetting).NumChunk * GetComponent<Data.MapData>(mapSetting).NumChunk))
            {
                _em.AddComponent<Data.Events.Event_CreateMapChunks>(eventHolder);
            }
            else
            {
                _em.AddComponent<Data.Events.Event_ChunksSlice>(eventHolder);
            }
        }

        protected override void OnUpdate()
        {

        }
    }
}
