using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

using static Unity.Mathematics.math;
using float2 = Unity.Mathematics.float2;
using Random = Unity.Mathematics.Random;

namespace KaizerWaldCode.Job
{
    [BurstCompile(CompileSynchronously = true)]
    public struct PoissonDiscPosition : IJobFor
    {
        [ReadOnly] public int MapSizeJob;
        [ReadOnly] public NativeArray<float2> DiscGridJob;
        [WriteOnly] public NativeArray<float3> DiscPositionJob;
        public void Execute(int index)
        {
            DiscPositionJob[index] = new float3(DiscGridJob[index].x - MapSizeJob / 2f, 0, DiscGridJob[index].y - MapSizeJob / 2f);
        }
    }

    /// <summary>
    /// Cell grid correspond to
    /// </summary>
    [BurstCompile(CompileSynchronously = true)]
    public struct PoissonDiscGridCellJob : IJobFor
    {
        [ReadOnly] public int JNumCellMap;
        [ReadOnly] public int JRadius;
        [ReadOnly] public NativeList<float2> JNtLst_PDiscPos;
        [NativeDisableParallelForRestriction]
        [WriteOnly] public NativeArray<float2> JNtarr_PDiscPosArr;
        [NativeDisableParallelForRestriction]//CAREFUL without this length is limited to 14 in parallel in native array
        [WriteOnly]public NativeArray<int> JPoissonDiscCellGrid;
        public void Execute(int index)
        {
            float2 cellGrid = new float2(JNumCellMap);
            FindCell(ref cellGrid, JNtLst_PDiscPos[index]);
            int cellIndex = (int)mad(cellGrid.y, JNumCellMap, cellGrid.x);
            JNtarr_PDiscPosArr[cellIndex] = JNtLst_PDiscPos[index];
            JPoissonDiscCellGrid[cellIndex] = cellIndex;
        }

        void FindCell(ref float2 cellGrid, float2 samplPos)
        {
            
            for (int i = 0; i < JNumCellMap; i++)
            {
                if ((int)cellGrid.y == JNumCellMap) cellGrid.y = select(JNumCellMap, i, samplPos.y <= mad(i, JRadius, JRadius));
                if ((int)cellGrid.x == JNumCellMap) cellGrid.x = select(JNumCellMap, i, samplPos.x <= mad(i, JRadius, JRadius));
                if ((int)cellGrid.x != JNumCellMap && (int)cellGrid.y != JNumCellMap) break;
            }
            
            /*
            for (int yG = 0; yG < JNumCellMap; yG++)
            {
                if (samplPos.y <= math.mad(yG, JRadius, JRadius))
                {
                    cellGrid.y = yG;
                    break;
                }
            }
            for (int xG = 0; xG < JNumCellMap; xG++)
            {
                if (samplPos.x <= math.mad(xG, JRadius, JRadius))
                {
                    cellGrid.x = xG;
                    break;
                }
            }
            */
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    public struct PoissonDiscGenerationJob : IJob
    {
        [ReadOnly] public int MapSize;

        [ReadOnly] public int NumSampleBeforeRejectJob;
        [ReadOnly] public float RadiusJob;
        [ReadOnly] public float CellSize; //(w)radius/math.sqrt(2)
        [ReadOnly] public int IndexInRow; // X(cols) : math.floor(mapHeight/cellSize)
        [ReadOnly] public int Row; // Y(rows) :math.floor(mapWidth/cellSize)
        [ReadOnly] public Random Prng;

        public NativeArray<int> DiscGridJob;
        public NativeList<float2> ActivePointsJob;
        public NativeList<float2> SamplePointsJob;

        public void Execute()
        {
            //float2 firstPoint = new float2(MapSize / 2f, MapSize / 2f);
            float2 firstPoint = float2.zero;
            ActivePointsJob.Add(firstPoint);
            while (!ActivePointsJob.IsEmpty)
            {
                int spawnIndex = Prng.NextInt(ActivePointsJob.Length);
                float2 spawnPosition = ActivePointsJob[spawnIndex];
                bool accepted = false;
                for (int k = 0; k < NumSampleBeforeRejectJob; k++)
                {
                    //Prng = Random.CreateFromIndex(JSeed);
                    float2 randDirection = Prng.NextFloat2Direction();
                    float2 sample = mad(randDirection, Prng.NextFloat(RadiusJob, mul(2, RadiusJob)), spawnPosition);

                    int sampleX = (int)(sample.x / CellSize); //col
                    int sampleY = (int)(sample.y / CellSize); //row
                    //TEST for rejection
                    if (SampleAccepted(sample, sampleX, sampleY))
                    {
                        SamplePointsJob.Add(sample);
                        ActivePointsJob.Add(sample);
                        DiscGridJob[mad(sampleY, Row, sampleX)] = SamplePointsJob.Length;
                        accepted = true;
                        break;
                    }
                }

                if (!accepted) ActivePointsJob.RemoveAt(spawnIndex);
            }

        }

        bool SampleAccepted(float2 sample, int sampleX, int sampleY)
        {
            if (sample.x >= 0 && sample.x < MapSize && sample.y >= 0 && sample.y < MapSize)
            {
                int searchStartX = max(0, sampleX - 2);
                int searchEndX = min(sampleX + 2, IndexInRow - 1);

                int searchStartY = max(0, sampleY - 2);
                int searchEndY = min(sampleY + 2, Row - 1);

                // <= or it will created strange cluster of points at the borders of the map
                for (int y = searchStartY; y <= searchEndY; y++)
                {
                    for (int x = searchStartX; x <= searchEndX; x++)
                    {
                        int indexSample = DiscGridJob[mad(y, Row, x)] - 1;
                        if (indexSample != -1)
                        {
                            if (distancesq(sample, SamplePointsJob[indexSample]) < mul(RadiusJob, RadiusJob)) return false;
                        }
                    }
                }
                return true;
            }
            return false;
        }
    }
}