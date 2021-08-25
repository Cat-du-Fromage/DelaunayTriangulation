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

namespace KaizerWaldCode.ECSSystem
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

        protected override void OnUpdate()
        {
            Entity mapSettings = GetSingletonEntity<Data.Tag.MapSettings>();

            int mapSize = GetComponent<Data.MapData>(mapSettings).MapSize;
            float cellSize = GetComponent<Data.PoissonDiscData>(mapSettings).CellSize;
            int cellNum = GetComponent<Data.PoissonDiscData>(mapSettings).NumCellMap;
            int gridSize = (int)math.ceil(mapSize / cellSize);

            using NativeArray<int> gridCells = new NativeArray<int>(gridSize * gridSize, Allocator.TempJob);
            using NativeList<float2> activePoints = new NativeList<float2>(Allocator.TempJob);
            using NativeList<float2> NtLst_samplePoints = new NativeList<float2>(Allocator.TempJob);

            //Position X/Y of samples
            #region PoissonDiscSamples
            PoissonDiscGenerationJob poissonDiscJobSecond = GeneratePoissonDiscSamples(mapSettings, gridSize, gridCells, activePoints, NtLst_samplePoints);
            JobHandle poissonDiscSecondJobHandle = poissonDiscJobSecond.Schedule(Dependency);
            poissonDiscSecondJobHandle.Complete();
            #endregion PoissonDiscSamples

            NativeArray<float2> NtArr_samples = new NativeArray<float2>(cellNum * cellNum, Allocator.TempJob);
            NativeArray<int> sampleCellGrid = new NativeArray<int>(cellNum * cellNum, Allocator.TempJob); //need values to be set to 0
            Job
            .WithBurst()
            .WithCode(() =>
            {
                float2 defVal2 = new float2(-1);
                for (int i = 0; i < sampleCellGrid.Length; i++)
                {
                    sampleCellGrid[i] = -1;
                    NtArr_samples[i] = defVal2;
                }
            }).Run();

            //Index in the grid cell
            #region CellGrid
            PoissonDiscGridCellJob poissonDiscCellJob = GridCellPoissonDiscSamples(mapSettings, NtArr_samples, sampleCellGrid, NtLst_samplePoints);
            JobHandle poissonDiscCellJobHandle = poissonDiscCellJob.ScheduleParallel(NtLst_samplePoints.Length, JobsUtility.JobWorkerCount - 1, Dependency);
            poissonDiscCellJobHandle.Complete();
            #endregion CellGrid

            GetBuffer<Data.PoissonDiscSamples.PoissonDiscPosition>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float2>().CopyFrom(NtArr_samples);
            GetBuffer<Data.PoissonDiscSamples.PoissonDiscCellIndex>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<int>().CopyFrom(sampleCellGrid);

            NtArr_samples.Dispose();
            sampleCellGrid.Dispose();

            ECSUtils.EndEventSystem<Event_PoissonDisc, Event_Voronoi>(GetSingletonEntity<Data.Tag.MapEventHolder>(), _em);
        }


        PoissonDiscGenerationJob GeneratePoissonDiscSamples(Entity mapDatas, int gridSize, NativeArray<int> gridCells, NativeList<float2> activePoints, NativeList<float2> samplePoints)
        {
            return new PoissonDiscGenerationJob
            {
                MapSize = GetComponent<Data.MapData>(mapDatas).MapSize,
                NumSampleBeforeRejectJob = GetComponent<Data.PoissonDiscData>(mapDatas).SampleBeforeReject,
                RadiusJob = GetComponent<Data.PoissonDiscData>(mapDatas).Radius,
                CellSize = GetComponent<Data.PoissonDiscData>(mapDatas).CellSize,
                IndexInRow = gridSize,
                Row = gridSize,
                Prng = new Random(GetComponent<Data.PoissonDiscData>(mapDatas).Seed),
                //Prng = Random.CreateFromIndex(GetComponent<Data.PoissonDiscData>(mapDatas).Seed),
                DiscGridJob = gridCells,
                ActivePointsJob = activePoints,
                SamplePointsJob = samplePoints,
            };
        }

        PoissonDiscGridCellJob GridCellPoissonDiscSamples(Entity mapDatas, NativeArray<float2> discGrid, NativeArray<int> poissonCellGrid, NativeList<float2> NtLstPDS)
        {
            return new PoissonDiscGridCellJob
            {
                JNumCellMap = GetComponent<Data.PoissonDiscData>(mapDatas).NumCellMap,
                JRadius = GetComponent<Data.PoissonDiscData>(mapDatas).Radius,
                JNtLst_PDiscPos = NtLstPDS,
                JNtarr_PDiscPosArr = discGrid,
                JPoissonDiscCellGrid = poissonCellGrid,
            };
        }


    }
}