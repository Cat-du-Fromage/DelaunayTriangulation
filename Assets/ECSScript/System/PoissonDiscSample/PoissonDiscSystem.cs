using KaizerWaldCode.Job;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
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
            _eventDescription = new EntityQueryDesc()
            {
                All = new ComponentType[] { typeof(Data.Events.Event_PoissonDisc) },
            };
            RequireForUpdate(GetEntityQuery(_eventDescription));
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        //WE SHALL USE WORLEY NOISE TO FIND EACH CLOSEST PIXEL!
        //https://www.youtube.com/watch?v=4066MndcyCk&t=592s&ab_channel=TheCodingTrain

        protected override void OnStartRunning()
        {

        }

        protected override void OnUpdate()
        {
            Entity mapSettings = GetSingletonEntity<Data.Tag.MapSettings>();

            int mapSize = GetComponent<Data.MapData>(GetSingletonEntity<Data.Tag.MapSettings>()).MapSize;
            float radius = 5f;
            float cellSize = radius / math.sqrt(2);
            int gridSize = (int)math.floor(mapSize / cellSize);

            NativeArray<float2> grid = new NativeArray<float2>(mapSize * mapSize, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            //FirstInit GridPoisson JOB
            PoissonDiscInitGrid poissonDiscInit = new PoissonDiscInitGrid()
            {
                DiscGridJob = grid,
            };
            JobHandle poissonDiscInitJobHandle = poissonDiscInit.ScheduleParallel(grid.Length, JobsUtility.JobWorkerCount - 1, Dependency);
            //poissonDiscInitJobHandle.Complete();
            //1598 : TopLeft corner
            Random pRNG = new Random(15646);

            

            NativeList<float2> activePoint = new NativeList<float2>(Allocator.TempJob);
            NativeList<float2> samplePoint = new NativeList<float2>(Allocator.TempJob);

            //grid = PoissonDiscSampling.PoissonDiscPoints(grid, activePoint, 15646, mapSize, radius);
            //PoissonDISC JOB

            PoissonDiscJob2 poissonDiscJob2 = new PoissonDiscJob2()
            {
                MapSize = mapSize,
                NumSampleBeforeRejectJob = 30,
                RadiusJob = radius,
                CellSize = cellSize,
                IndexInRow = gridSize,
                Row = gridSize,
                PRNG = pRNG,
                DiscGridJob = grid,
                ActivePointsJob = activePoint,
                SamplePointsJob = samplePoint,
            };
            JobHandle poissonDisc2JobHandle = poissonDiscJob2.Schedule(poissonDiscInitJobHandle);
            poissonDisc2JobHandle.Complete();
            Debug.Log($"sample Points LENGTH {samplePoint.Length}");
            NativeArray<float2> sampleNatArra = new NativeArray<float2>(samplePoint.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            sampleNatArra.CopyFrom(samplePoint.ToArray());
            samplePoint.Dispose();
            //CONVERT BACK to float3Position
            NativeArray<float3> gridPosition = new NativeArray<float3>(sampleNatArra.Length/*gridSize * gridSize*/, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            
            PoissonDiscPosition poissonDiscPosition = new PoissonDiscPosition()
            {
                MapSizeJob = mapSize,
                DiscGridJob = sampleNatArra,
                DiscPositionJob = gridPosition,
            };
            JobHandle poissonDiscPositionJobHandle = poissonDiscPosition.ScheduleParallel(gridPosition.Length, JobsUtility.JobWorkerCount - 1, poissonDisc2JobHandle);

            poissonDiscPositionJobHandle.Complete();
            //Debug.Log($"JobFinished : {_poissonPointList.Length}");
            GetBuffer<Data.Chunks.PoissonDiscGrid>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float2>().CopyFrom(sampleNatArra);
            sampleNatArra.Dispose();
            grid.Dispose();
            activePoint.Dispose();
            //samplePoint.Dispose();

            GetBuffer<Data.Chunks.PoissonDiscSample>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float3>().CopyFrom(gridPosition);
            gridPosition.Dispose();
            _em.RemoveComponent<Data.Events.Event_PoissonDisc>(GetSingletonEntity<Data.Tag.MapEventHolder>());
        }

        protected override void OnDestroy()
        {
            //if (_poissonPointList.IsCreated) _poissonPointList.Dispose();
        }
    }
}