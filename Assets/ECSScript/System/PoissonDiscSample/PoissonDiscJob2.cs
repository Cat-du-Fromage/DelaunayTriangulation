using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

[BurstCompile(CompileSynchronously = true)]
public struct PoissonDiscJobSecond : IJob
{
    [ReadOnly] public int MapSize;

    [ReadOnly] public int NumSampleBeforeRejectJob;
    [ReadOnly] public float RadiusJob;
    [ReadOnly] public float CellSize; //(w)radius/math.sqrt(2)
    [ReadOnly] public int IndexInRow; // X(cols) : math.floor(mapHeight/cellSize)
    [ReadOnly] public int Row; // Y(rows) :math.floor(mapWidth/cellSize)

    //STEP 1
    [ReadOnly] public Random PRNG;
    //[ReadOnly] public float2 randomPoint; //new float2(PRNG(X), PRNG(Y))

    public NativeArray<float2> DiscGridJob;
    public NativeList<float2> ActivePointsJob;
    public NativeList<float2> SamplePointsJob;

    [BurstDiscard]
    public void GetRandom(float2 dir)
    {
        Debug.Log($"randdir = {dir}");
    }

    public void Execute()
    {

        float2 firstPoint = new float2(MapSize / 2f, MapSize / 2f);
        DiscGridJob[(int)math.mad((MapSize / 2f) / CellSize, IndexInRow, (MapSize / 2f) / CellSize)] = firstPoint;

        ActivePointsJob.Add(firstPoint);
        while (ActivePointsJob.Length > 0)
        {
            int spawnIndex = PRNG.NextInt(ActivePointsJob.Length);
            float2 spawnPosition = ActivePointsJob[spawnIndex];
            bool accepted = false;
            for (int k = 0; k < NumSampleBeforeRejectJob; k++)
            {
                float2 randDirection = PRNG.NextFloat2Direction();
                float2 sample = math.mad(randDirection, PRNG.NextFloat(RadiusJob, math.mul(2, RadiusJob)), spawnPosition) /*- (new float2(MapSize / 2f, MapSize / 2f))*/;
                //SamplePointsJob.Add(sample);

                int sampleX = (int)math.floor(sample.x / CellSize); //col
                int sampleY = (int)math.floor(sample.y / CellSize); //row
                //TEST for rejection
                if (Accepted(sample, sampleX, sampleY))
                {
                    DiscGridJob[math.mad(sampleY, IndexInRow, sampleX)] = sample;
                    ActivePointsJob.Add(sample);
                    SamplePointsJob.Add(sample);
                    accepted = true;
                    break;
                }
            }
            if (!accepted) ActivePointsJob.RemoveAt(spawnIndex);
        }

    }

    bool Accepted(float2 sample, int sampleX, int sampleY)
    {
        if (sample.x >= 0 && sample.x < MapSize && sample.y >= 0 && sample.y < MapSize)
        {
            if (DiscGridJob[math.mad(sampleY, IndexInRow, sampleX)].Equals(new float2(-1, -1)))
            {
                for (int x1 = -2; x1 <= 2; x1++)
                {
                    for (int y1 = -2; y1 <= 2; y1++)
                    {
                        int indexSample = math.mad(sampleY + y1, IndexInRow, sampleX + x1);
                        if (indexSample >= 0)
                        {
                            if (DiscGridJob[indexSample].Equals(new float2(-1, -1)))
                            {
                                float2 neighbor = DiscGridJob[indexSample];
                                if (math.distancesq(sample, neighbor) < math.mul(RadiusJob, RadiusJob))
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }
        return false;
    }
}
