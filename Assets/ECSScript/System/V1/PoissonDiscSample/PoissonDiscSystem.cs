using KaizerWaldCode.Job;
using KaizerWaldCode.Utils;
using KaizerWaldCode.Data.Events;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace KaizerWaldCode.System
{
    public class PoissonDiscSystem : SystemBase
    {
        private EntityQueryDesc _eventDescription;
        private EntityManager _em;

        protected override void OnCreate()
        {
            ECSUtils.SystemEventRequire<Event_PoissonDisc>(ref _eventDescription, ref _em);
            RequireForUpdate(GetEntityQuery(_eventDescription));
        }

        //WE SHALL USE WORLEY NOISE TO FIND EACH CLOSEST PIXEL!
        //https://www.youtube.com/watch?v=4066MndcyCk&t=592s&ab_channel=TheCodingTrain

        protected override void OnUpdate()
        {
            Entity mapSettings = GetSingletonEntity<Data.Tag.MapSettings>();

            int mapSize = GetComponent<Data.MapData>(mapSettings).MapSize;
            float cellSize = GetComponent<Data.PoissonDiscData>(mapSettings).CellSize;
            int gridSize = (int)math.ceil(mapSize / cellSize);

            using NativeArray<int> gridCells = new NativeArray<int>(gridSize * gridSize, Allocator.TempJob);
            using NativeList<float2> activePoints = new NativeList<float2>(Allocator.TempJob);
            using NativeList<float2> samplePoints = new NativeList<float2>(Allocator.TempJob);
            #region PoissonDiscSamples

            PoissonDiscJobSecond poissonDiscJobSecond = PoissonMethodJob(mapSettings, gridSize, gridCells, activePoints, samplePoints);
            JobHandle poissonDiscSecondJobHandle = poissonDiscJobSecond.Schedule(Dependency);
            poissonDiscSecondJobHandle.Complete();

            #endregion PoissonDiscSamples
            //========================================================================================================================================================
            #region Conversion Float2 -> float3
            using NativeArray<float2> sampleNatArra = new NativeArray<float2>(samplePoints.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            sampleNatArra.CopyFrom(samplePoints.ToArray());

            #region CellGrid

            uint cellNum = GetComponent<Data.PoissonDiscData>(mapSettings).NumCellMap;
            //need values to be set to 0
            using NativeArray<float3> sampleCellGrid = new NativeArray<float3>((int)math.mul(cellNum, cellNum), Allocator.TempJob);
            PoissonDiscGridCell poissonDiscGridCellJob = PoissonDiscSampleGridCell(mapSettings, sampleNatArra, sampleCellGrid);
            JobHandle poissonDiscGridCellJobHandle = poissonDiscGridCellJob.ScheduleParallel(samplePoints.Length, JobsUtility.JobWorkerCount - 1, Dependency);
            poissonDiscGridCellJobHandle.Complete();
            GetBuffer<Data.Chunks.PDiscGrid>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float3>().CopyFrom(sampleCellGrid);

            #endregion CellGrid
            //CONVERT BACK to float3 Position and offset points by mapsize/2
            //TO DO : Find a way to directly calculate sample point in the right direction => so we don't need to offset later
            using NativeArray<float3> gridPosition = new NativeArray<float3>(sampleNatArra.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            PoissonDiscPosition poissonDiscPosition = new PoissonDiscPosition
            {
                MapSizeJob = mapSize,
                DiscGridJob = sampleNatArra,
                DiscPositionJob = gridPosition,
            };
            JobHandle poissonDiscPositionJobHandle = poissonDiscPosition.ScheduleParallel(gridPosition.Length, JobsUtility.JobWorkerCount - 1, poissonDiscSecondJobHandle);
            poissonDiscPositionJobHandle.Complete();

            #endregion Conversion Float2 -> float3
            //========================================================================================================================================================
            //Used for voronoi / worley noise
            //GetBuffer<Data.Chunks.PDiscGrid>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<int>().CopyFrom(gridCells);
            GetBuffer<Data.Chunks.PoissonDiscGrid>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float2>().CopyFrom(sampleNatArra);
            //Used for debuging points
            GetBuffer<Data.Chunks.PoissonDiscSample>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float3>().CopyFrom(gridPosition);

            ECSUtils.EndEventSystem<Event_PoissonDisc, Event_Voronoi>(GetSingletonEntity<Data.Tag.MapEventHolder>(), _em);
        }

        PoissonDiscJobSecond PoissonMethodJob(Entity mapDatas, int gridSize, NativeArray<int> gridCells, NativeList<float2> activePoints, NativeList<float2> samplePoints)
        {
            return new PoissonDiscJobSecond
            {
                MapSize = GetComponent<Data.MapData>(mapDatas).MapSize,
                NumSampleBeforeRejectJob = GetComponent<Data.PoissonDiscData>(mapDatas).SampleBeforeReject,
                RadiusJob = GetComponent<Data.PoissonDiscData>(mapDatas).Radius,
                CellSize = GetComponent<Data.PoissonDiscData>(mapDatas).CellSize,
                IndexInRow = gridSize,
                Row = gridSize,
                Prng = new Random(GetComponent<Data.PoissonDiscData>(mapDatas).Seed),
                DiscGridJob = gridCells,
                ActivePointsJob = activePoints,
                SamplePointsJob = samplePoints,
            };
        }

        PoissonDiscGridCell PoissonDiscSampleGridCell(Entity mapDatas, NativeArray<float2> discGrid, NativeArray<float3> poissonCellGrid)
        {
            return new PoissonDiscGridCell()
            {
                NumCellMap = GetComponent<Data.PoissonDiscData>(mapDatas).NumCellMap,
                Radius = GetComponent<Data.PoissonDiscData>(mapDatas).Radius,
                DiscGridJob = discGrid,
                PoissonDiscGridJob = poissonCellGrid,
            };
        }


    }
}