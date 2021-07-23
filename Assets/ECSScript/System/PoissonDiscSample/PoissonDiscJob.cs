using System;
using System.Linq;
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
    [BurstCompile]
    public struct PoissonDiscJob : IJob
    {
        [ReadOnly] public float PI2Job;
        [ReadOnly] public float CellSizeJob; //radius / Mathf.Sqrt(2);
        [ReadOnly] public float RadiusJob;
        [ReadOnly] public int NumSampleBeforeRejectJob;
        [ReadOnly] public int LengthGridXJob;//(int)math.ceil(SampleMapSizeJob.x / CellSizeJob);
        [ReadOnly] public int LengthGridYJob;//(int)math.ceil(SampleMapSizeJob.y / CellSizeJob);

        [ReadOnly] public float2 SampleMapSizeJob;
        [ReadOnly] public Random RandomJob;

        public NativeList<float2> PoissonPointsJob;
        //public NativeArray<float3> SpawnPointsJob;

        //number of time a cell fit into a sampleRegionSize(sample region : ex. grid 3x3)
        public NativeList<float2> SpawnPointsJob;
        [DeallocateOnJobCompletion]public NativeArray<int> GridJob;

        [BurstDiscard]
        public void GetXZ(int ind)
        {
            Debug.Log($"Pass = {ind}");
        }

        [BurstDiscard]
        public void Pass(float val)
        {
            Debug.Log($"float val = {val}");
        }

        public void Execute()
        {
            //NativeList<float2> SpawnPointsJob = new NativeList<float2>(1, Allocator.Temp);
            //SpawnPointsJob.Add(SampleMapSizeJob / 2f);
            int pointJobIndex = 0;
            GetXZ(SpawnPointsJob.Length);

            while (SpawnPointsJob.Length > 0)
            {
                
                int spawnIndex = RandomJob.NextInt(SpawnPointsJob.Length);
                float2 spawnCenter = SpawnPointsJob[spawnIndex];
                bool candidateAccepted = false;
                GetXZ(spawnIndex);
                //Keep track of the index of PointJob
                for (int i = 0; i < NumSampleBeforeRejectJob; i++)
                {
                    //GetXZ(i);
                    float angle = RandomJob.NextFloat(1f) * PI2Job; //math.mul(math.PI, 2)
                    float2 dir = new float2(math.sin(angle), math.cos(angle));

                    //float3 candidate = spawnCenter + dir * RandomJob.NextFloat(RadiusJob, math.mul(2, RadiusJob));
                    float2 candidate = spawnCenter + dir * RandomJob.NextFloat(RadiusJob, math.mul(2f, RadiusJob));
                    Pass( (spawnCenter+dir * candidate).x );
                    Pass((spawnCenter + dir * candidate).y);
                    if (IsValid(candidate))
                    {
                        //PoissonPointsJob[pointJobIndex] = candidate;
                        PoissonPointsJob.Add(candidate);
                        pointJobIndex++;
                        SpawnPointsJob.Add(candidate);
                        GridJob[math.mad( (int)(candidate.y / CellSizeJob), LengthGridXJob, (int)(candidate.x / CellSizeJob) )] = PoissonPointsJob.Length;
                        candidateAccepted = true;
                        break;
                    }
                }

                if (!candidateAccepted)
                {
                    SpawnPointsJob.RemoveAt(spawnIndex);
                }
            }
        }

        bool IsValid(float2 candidate)
        {
            if (candidate.x >= 0 && candidate.x < SampleMapSizeJob.x && candidate.y >= 0 && candidate.y < SampleMapSizeJob.y)
            {
                int cellX = (int)(candidate.x / CellSizeJob);
                int cellY = (int)(candidate.y / CellSizeJob);

                int searchStartX = math.max(0, cellX - 2);
                int searchEndX = math.min(cellX + 2, LengthGridXJob - 1);

                int searchStartY = math.max(0, cellY - 2);
                int searchEndY = math.min(cellY + 2, LengthGridYJob - 1);
                GetXZ(searchStartY);
                GetXZ(searchEndY);
                for (int y = searchStartY; y <= searchEndY; y++)
                {
                    for (int x = searchStartX; x <= searchEndX; x++)
                    {
                        int pointIndex = GridJob[math.mad(y,math.min((searchEndX + searchStartX), (searchEndY + searchStartY)) ,x)] - 1;
                        GetXZ(pointIndex);
                        GetXZ(PoissonPointsJob.Length);
                        if (pointIndex != -1)
                        {
                            float sqrDst = math.length(candidate - PoissonPointsJob[pointIndex]);
                            if (sqrDst < RadiusJob)
                            {
                                return false;
                            }
                        }
                    }
                }

                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// STEP 0
    /// Init grid to -1 value
    /// </summary>
    [BurstCompile]
    public struct PoissonDiscInitGrid : IJobFor
    {
        [WriteOnly] public NativeArray<float2> DiscGridJob; //Size = X:math.floor(mapHeight/cellSize) Y:math.floor(mapWidth/cellSize)
        public void Execute(int index)
        {
            DiscGridJob[index] = new float2(-1,-1);
        }
    }

    /// <summary>
    /// STEP 0
    /// Init grid to -1 value
    /// </summary>
    [BurstCompile]
    public struct PoissonDiscPosition : IJobFor
    {
        [ReadOnly] public int MapSizeJob;
        [ReadOnly] public NativeArray<float2> DiscGridJob;
        [WriteOnly] public NativeArray<float3> DiscPositionJob;
        public void Execute(int index)
        {
            DiscPositionJob[index] = new float3(DiscGridJob[index].x - MapSizeJob / 2f, 1, DiscGridJob[index].y - MapSizeJob / 2f);
        }
    }


    [BurstCompile(CompileSynchronously = true)]
    public struct PoissonDiscJob2 : IJob
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

        [BurstDiscard]
        public void GetfloatVal(float val, float val2)
        {
            Debug.Log($"distance val = {val}, distance radius ={val2}");
        }

        [BurstDiscard]
        public void GetIndex(int index)
        {
            Debug.Log($"Index = {index}");
        }

        [BurstDiscard]
        public void GetSpawnPoints(int index)
        {
            Debug.Log($"Remain = {index}");
        }

        [BurstDiscard]
        public void GetFalse()
        {
            Debug.Log($"it's not accepted");
        }

        public void Execute()
        {
            
            //Set Random on the Map Size
            int x = (int)math.floor(PRNG.NextInt(0, MapSize));
            int y = (int)math.floor(PRNG.NextInt(0, MapSize));
            //Get Position On the "cell grid"
            int i = (int)math.floor(x / CellSize);
            int j = (int)math.floor(y / CellSize);
            
            //CAREFUL the map is center around 0,0 so we can have negativ values!!
            float2 randomPoint = new float2(x - (MapSize / 2f), y - (MapSize / 2f));
            //this line below cause a crash??!! WHY!?
            //float2 randomPoint = new float2((int)math.floor(PRNG.NextInt(0, MapSize)) - (MapSize / 2f), (int)math.floor(PRNG.NextInt(0, MapSize)) - (MapSize / 2f));
            DiscGridJob[j * IndexInRow + i] = randomPoint;

            ActivePointsJob.Add(randomPoint);
            //Step3
            //SEEMS LIKE WHILE LOOP ARE NOT ACCEPTED!!
            while (ActivePointsJob.Length > 0)
            {
                int spawnIndex = PRNG.NextInt(ActivePointsJob.Length);
                float2 spawnPosition = ActivePointsJob[spawnIndex];
                bool accepted = false;
                for (int k = 0; k < NumSampleBeforeRejectJob; k++)
                {
                    float2 randDirection = PRNG.NextFloat2Direction();

                    //float angle = PRNG.NextFloat(1f) * math.mul(math.PI,2); //CHeck values!
                    //float2 randDirection = new float2(Mathf.Sin(angle), Mathf.Cos(angle));

                    float2 sample = math.mad(randDirection,PRNG.NextFloat(RadiusJob, math.mul(2, RadiusJob)), spawnPosition) /*- (new float2(MapSize / 2f, MapSize / 2f))*/;
                    //SamplePointsJob.Add(sample);

                    int sampleX = (int)math.floor(sample.x / CellSize); //col
                    int sampleY = (int)math.floor(sample.y / CellSize); //row
                    GetfloatVal(sampleX, sampleY);
                    //TEST for rejection
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
                                                accepted = false;
                                                GetFalse();
                                            }

                                            else
                                            {
                                                accepted = true;
                                            }

                                        }
                                    }
                                }
                            }
                            
                            if (accepted)
                            {
                                DiscGridJob[math.mad(sampleY, IndexInRow, sampleX)] = sample;
                                GetIndex(math.mad(sampleY, IndexInRow, sampleX));
                                ActivePointsJob.Add(sample);
                                SamplePointsJob.Add(sample);
                                break;
                            }
                            
                        }
                    }
                }

                GetIndex(ActivePointsJob.Length);

                if (!accepted) ActivePointsJob.RemoveAt(spawnIndex);
            }

        }
    }

}
