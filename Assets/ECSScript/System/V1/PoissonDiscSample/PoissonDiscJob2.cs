using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
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

    [BurstCompile(CompileSynchronously = true)]
    public struct PoissonDiscGridCell : IJobFor
    {
        [ReadOnly] public uint NumCellMap;
        [ReadOnly] public uint Radius;
        [ReadOnly] public NativeArray<float2> DiscGridJob;
        [NativeDisableParallelForRestriction]//CAREFUL without this length is limited to 14 in parallel in native array
        [WriteOnly]public NativeArray<float3> PoissonDiscGridJob;
        public void Execute(int index)
        {
            float2 cellGrid = float2.zero;
            FindCell(ref cellGrid, DiscGridJob[index]);
            //
            int cellIndex = (int) math.mad(cellGrid.y, NumCellMap, cellGrid.x);
            PoissonDiscGridJob[cellIndex] = new float3(DiscGridJob[index], math.mad(cellGrid.y, NumCellMap, cellGrid.x));
        }

        void FindCell(ref float2 cellGrid, float2 samplPos)
        {
            cellGrid.y = NumCellMap;
            for (int yG = 0; yG < NumCellMap; yG++)
            {
                if (samplPos.y <= yG * Radius + Radius)
                {
                    cellGrid.y = yG;
                    break;
                }
            }
            cellGrid.x = NumCellMap;
            for (int xG = 0; xG < NumCellMap; xG++)
            {
                if (samplPos.x <= xG * Radius + Radius)
                {
                    cellGrid.x = xG;
                    break;
                }
            }
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    public struct PoissonDiscJobSecond : IJob
    {
        [ReadOnly] public int MapSize;

        [ReadOnly] public uint NumSampleBeforeRejectJob;
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
            while (ActivePointsJob.Length > 0)
            {
                int spawnIndex = Prng.NextInt(ActivePointsJob.Length);
                float2 spawnPosition = ActivePointsJob[spawnIndex];
                bool accepted = false;
                for (int k = 0; k < NumSampleBeforeRejectJob; k++)
                {
                    float2 randDirection = Prng.NextFloat2Direction();
                    float2 sample = math.mad(randDirection, Prng.NextFloat(RadiusJob, math.mul(2, RadiusJob)), spawnPosition);

                    int sampleX = (int)(sample.x / CellSize); //col
                    int sampleY = (int)(sample.y / CellSize); //row
                    //TEST for rejection
                    if (SampleAccepted(sample, sampleX, sampleY))
                    {
                        SamplePointsJob.Add(sample);
                        ActivePointsJob.Add(sample);
                        DiscGridJob[math.mad(sampleY, Row, sampleX)] = SamplePointsJob.Length;
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
                int searchStartX = math.max(0, sampleX - 2);
                int searchEndX = math.min(sampleX + 2, IndexInRow - 1);

                int searchStartY = math.max(0, sampleY - 2);
                int searchEndY = math.min(sampleY + 2, Row - 1);

                // <= or it will created strange cluster of points at the borders of the map
                for (int y = searchStartY; y <= searchEndY; y++)
                {
                    for (int x = searchStartX; x <= searchEndX; x++)
                    {
                        int indexSample = DiscGridJob[math.mad(y, Row, x)] - 1;
                        if (indexSample != -1)
                        {
                            if (math.distancesq(sample, SamplePointsJob[indexSample]) < math.mul(RadiusJob, RadiusJob)) return false;
                        }
                    }
                }
                return true;
            }
            return false;
        }
    }
}