using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace KaizerWaldCode
{
    public static class PoissonDiscSampling
    {
        /*
        float2 randomPoint = float2.zero;
        int i = (int)math.floor(randomPoint.x / cellSize);
        int j = (int)math.floor(randomPoint.y / cellSize);
        discGrid[j * indexInRow + i] = randomPoint;
        */
        private static float4 GetRandomPoint(Random pRNG, int mapSize, float cellSize)
        {
            //Set Random on the Map Size
            int xRand = (int)math.floor(pRNG.NextInt(0, mapSize));
            int yRand = (int)math.floor(pRNG.NextInt(0, mapSize));
            //Get Position On the "cell grid"
            int xRandPos = (int)math.floor(xRand / cellSize);
            int yRandPos = (int)math.floor(yRand / cellSize);
            //CAREFUL the map is center around 0,0 so we can have negativ values!!
            return new float4(xRandPos, yRandPos, xRand - (mapSize / 2f), yRand - (mapSize / 2f));
        }
        public static NativeArray<float2> PoissonDiscPoints(NativeArray<float2> discGrid, NativeList<float2> activePoint, uint seed, int mapSize, float radius)
        {
            int numSampleBeforeReject = 30;
            float cellSize = radius / math.sqrt(2);
            Random pRNG = new Random(seed);

            int indexInRow = (int)math.floor(mapSize / cellSize); // X(cols)
            int row = (int)math.floor(mapSize / cellSize); // Y(rows)

            float4 randomPoint = GetRandomPoint(pRNG, mapSize, cellSize);
            discGrid[(int)randomPoint.y * indexInRow + (int)randomPoint.x] = randomPoint.zw;
            activePoint.Add(randomPoint.zw);

            while (activePoint.Length > 0)
            {
                int spawnIndex = pRNG.NextInt(activePoint.Length);
                float2 spawnPosition = activePoint[spawnIndex];
                bool accepted = true;

                for (int k = 0; k < numSampleBeforeReject; k++)
                {
                    float2 randDirection = pRNG.NextFloat2Direction();
                    float2 sample = spawnPosition + randDirection * pRNG.NextFloat(radius, math.mul(2, radius));

                    if (sample.x >= 0 && sample.x < indexInRow && sample.y >= 0 && sample.y < row)
                    {
                        int sampleX = (int)math.floor(sample.x / cellSize); //col
                        int sampleY = (int)math.floor(sample.y / cellSize); //row

                        for (int x1 = -1; x1 <= 1; x1++)
                        {
                            for (int y1 = -1; y1 <= 1; y1++)
                            {
                                int indexSample = math.mad(sampleY + y1, indexInRow, sampleX + x1);
                                if (indexSample >= 0)
                                {
                                    float2 neighbor = discGrid[indexSample];
                                    if (neighbor.xy.Equals(new float2(-1, -1)))
                                    {
                                        if (math.distance(sample, neighbor) < radius)
                                        {
                                            accepted = false;
                                        }

                                    }
                                }
                            }
                        }
                        if (accepted)
                        {
                            discGrid[math.mad(sampleY, indexInRow, sampleX)] = sample;
                            activePoint.Add(sample);
                            break;
                        }
                    }
                }
                if (!accepted) activePoint.RemoveAt(spawnIndex);
            }

            return discGrid;
        }











    
        public static List<float2> GeneratePoints(uint seed, float radius, float2 sampleRegionsSize, int numSampleBeforeReject = 30)
        {
            //Pythagore = r2 = size2 + size2(in our case)
            float _cellSize = radius / Mathf.Sqrt(2);

            //number of time a cell fit into a sampleRegionSize(sample region : ex. grid 3x3)
            int[,] grid = new int[Mathf.CeilToInt(sampleRegionsSize.x / _cellSize), Mathf.CeilToInt(sampleRegionsSize.y / _cellSize)];
            List<float2> points = new List<float2>();
            List<float2> spawnPoints = new List<float2>();
            Unity.Mathematics.Random prng = new Unity.Mathematics.Random(seed);

            spawnPoints.Add(sampleRegionsSize / 2);
            while (spawnPoints.Count > 0)
            {
                int _spawnIndex = prng.NextInt(spawnPoints.Count);
                float2 _spawnCenter = spawnPoints[_spawnIndex];
                bool _candidateAccepted = false;
                for (int i = 0; i < numSampleBeforeReject; i++)
                {
                    float _angle = prng.NextFloat(1f) * math.PI * 2; //CHeck values!
                    float2 _dir = new float2(Mathf.Sin(_angle), Mathf.Cos(_angle));

                    float2 _candidate = _spawnCenter + _dir * prng.NextFloat(radius, 2 * radius);

                    if (IsValid(_candidate, sampleRegionsSize, _cellSize, radius, points, grid))
                    {
                        points.Add(_candidate);
                        spawnPoints.Add(_candidate);
                        grid[(int) (_candidate.x / _cellSize), (int) (_candidate.y / _cellSize)] = points.Count;
                        _candidateAccepted = true;
                        break;
                    }
                }

                if (!_candidateAccepted)
                {
                    spawnPoints.RemoveAt(_spawnIndex);
                }
            }

            return points;
        }

        static bool IsValid(float2 candidate, float2 sampleRegionSize, float cellSize, float radius, List<float2> points, int[,] grid)
        {
            if (candidate.x >= 0 && candidate.x < sampleRegionSize.x && candidate.y >= 0 && candidate.y < sampleRegionSize.y)
            {
                int cellX = (int) (candidate.x / cellSize);
                int cellY = (int) (candidate.y / cellSize);

                int searchStartX = math.max(0, cellX - 2);
                int searchEndX = math.min(cellX + 2, grid.GetLength(0) - 1);

                int searchStartY = math.max(0, cellY - 2);
                int searchEndY = math.min(cellY + 2, grid.GetLength(1) - 1);

                for (int x = searchStartX; x <= searchEndX; x++)
                {
                    for (int y = searchStartY; y <= searchEndY; y++)
                    {
                        int pointIndex = grid[x, y] - 1;
                        if (pointIndex != -1)
                        {
                            float sqrDst = math.lengthsq(candidate - points[pointIndex]);
                            if (sqrDst < radius * radius)
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
}