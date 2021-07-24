using KaizerWaldCode.Job;
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
            _eventDescription = new EntityQueryDesc()
            {
                All = new ComponentType[] { typeof(Data.Events.Event_PoissonDisc) },
            };
            RequireForUpdate(GetEntityQuery(_eventDescription));
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        //WE SHALL USE WORLEY NOISE TO FIND EACH CLOSEST PIXEL!
        //https://www.youtube.com/watch?v=4066MndcyCk&t=592s&ab_channel=TheCodingTrain

        protected override void OnUpdate()
        {
            Entity mapSettings = GetSingletonEntity<Data.Tag.MapSettings>();

            int mapSize = GetComponent<Data.MapData>(mapSettings).MapSize;
            float radius = 1f;
            float cellSize = radius / math.sqrt(2);
            int gridSize = (int)math.ceil(mapSize / cellSize);
            Random pRng = new Random(15646);

            NativeArray<int> gridCells = new NativeArray<int>(gridSize * gridSize, Allocator.TempJob);
            NativeList<float2> activePoints = new NativeList<float2>(Allocator.TempJob);
            NativeList<float2> samplePoints = new NativeList<float2>(Allocator.TempJob);
            #region PoissonDiscSamples
            PoissonDiscJobSecond poissonDiscJobSecond = new PoissonDiscJobSecond
            {
                MapSize = mapSize,
                NumSampleBeforeRejectJob = 30,
                RadiusJob = radius,
                CellSize = cellSize,
                IndexInRow = gridSize,
                Row = gridSize,
                Prng = pRng,
                DiscGridJob = gridCells,
                ActivePointsJob = activePoints,
                SamplePointsJob = samplePoints,
            };
            JobHandle poissonDiscSecondJobHandle = poissonDiscJobSecond.Schedule(Dependency);
            poissonDiscSecondJobHandle.Complete();
            activePoints.Dispose();
            #endregion PoissonDiscSamples
            //========================================================================================================================================================
            #region Conversion Float2 -> float3
            //JobHandle test = NativeSortExtension.Sort(gridCells, Dependency);
            
            NativeArray<float2> sampleNatArra = new NativeArray<float2>(samplePoints.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            sampleNatArra.CopyFrom(samplePoints.ToArray());
            samplePoints.Dispose(poissonDiscSecondJobHandle);
            //CONVERT BACK to float3 Position and offset points by mapsize/2
            //TO DO : Find a way to directly calculate sample point in the right direction => so we don't need to offset later
            NativeArray<float3> gridPosition = new NativeArray<float3>(sampleNatArra.Length/*gridSize * gridSize*/, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
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
            GetBuffer<Data.Chunks.PoissonDiscGrid>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float2>().CopyFrom(sampleNatArra);
            //Used for debuging points
            GetBuffer<Data.Chunks.PoissonDiscSample>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float3>().CopyFrom(gridPosition);
            sampleNatArra.Dispose();
            gridCells.Dispose();
            gridPosition.Dispose();

            _em.RemoveComponent<Data.Events.Event_PoissonDisc>(GetSingletonEntity<Data.Tag.MapEventHolder>());
            _em.AddComponent<Data.Events.Event_Voronoi>(GetSingletonEntity<Data.Tag.MapEventHolder>());
        }
    }
}