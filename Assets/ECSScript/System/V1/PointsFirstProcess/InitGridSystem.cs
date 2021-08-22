using KaizerWaldCode.Job;
using KaizerWaldCode.Utils;
using static KaizerWaldCode.Utils.NativeCollectionUtils;
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

namespace KaizerWaldCode.ECSSystem
{
    /// <summary>
    /// Process vertices (float3)
    /// assign values dependending on map datas (notably spacing)
    /// 
    /// </summary>
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
            verticesArr = await ComputeShaderInit(verticesArr, initGridCShader, mapSetting, MapPointPerAxis);
            
            GetBuffer<Data.Vertices.VertexPosition>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float3>().CopyFrom(verticesArr);
            JobVerticesCellIndex(mapSetting, verticesArr);
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
            return vertArr;
        }

        void JobVerticesCellIndex(Entity mapSetting, float3[] vPos)
        {
            int numCells = GetComponent<Data.PoissonDiscData>(mapSetting).NumCellMap;
            //using NativeArray<float3> verticesPos = new NativeArray<float3>(vPos.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            //verticesPos.CopyFrom(vPos);
            using NativeArray<float3> verticesPos = ArrayToNativeArray(vPos,Allocator.TempJob,NativeArrayOptions.UninitializedMemory);
            using NativeArray<int> vCellIndex = new NativeArray<int>(vPos.Length, Allocator.TempJob);
            VerticesCellIndexJob verticesCellIndexJob = new VerticesCellIndexJob()
            {
                JNumCellMap = numCells,
                JRadius = GetComponent<Data.PoissonDiscData>(mapSetting).Radius,
                JVertices = verticesPos,
                JVerticesCellGrid = vCellIndex
            };
            JobHandle verticesCellIndexJobHandle = verticesCellIndexJob.ScheduleParallel(vPos.Length, JobsUtility.JobWorkerCount - 1, Dependency);
            verticesCellIndexJobHandle.Complete();
            GetBuffer<Data.Vertices.VertexCellIndex>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<int>().CopyFrom(vCellIndex);
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
