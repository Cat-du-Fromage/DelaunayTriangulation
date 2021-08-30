using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

using static Unity.Mathematics.math;
using int2 = Unity.Mathematics.int2;
namespace KaizerWaldCode.Job
{
    [BurstCompile(CompileSynchronously = true)]
    public struct VoronoiCellGridJob : IJobFor
    {
        [ReadOnly] public int NumCellJob;

        [ReadOnly] public NativeArray<float3> JNtArr_VerticesPos;
        [ReadOnly] public NativeArray<int> JNtArr_VerticesCellIndex;
        [ReadOnly] public NativeArray<float2> JNtArr_SamplesPos;

        [NativeDisableParallelForRestriction]
        [WriteOnly] public NativeArray<float4> JVoronoiVertices;
        public void Execute(int index)
        {
            int2 xRange;
            int2 yRange;
            int numCell;

            CellGridRanges(JNtArr_VerticesCellIndex[index], out xRange, out yRange, out numCell);
            NativeArray<float2> cells = new NativeArray<float2>(numCell, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            NativeArray<int> cellsIndex = new NativeArray<int>(numCell, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            int cellCount = 0;
            for (int y = yRange.x; y <= yRange.y; y++)
            {
                for (int x = xRange.x; x <= xRange.y; x++)
                {
                    int indexCellOffset = JNtArr_VerticesCellIndex[index] + mad(y, NumCellJob, x);
                    cells[cellCount] = JNtArr_SamplesPos[indexCellOffset];
                    cellsIndex[cellCount] = indexCellOffset;
                    cellCount++;
                }
            }

            NativeArray<float> distances = new NativeArray<float>(numCell, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < numCell; i++)
            {
                distances[i] = select(distancesq(JNtArr_VerticesPos[index].xz, JNtArr_SamplesPos[cellsIndex[i]]), float.MaxValue, JNtArr_SamplesPos[cellsIndex[i]].Equals(float2(-1)));
            }
            JVoronoiVertices[index] = float4(float3(JNtArr_VerticesPos[index].x, 0, JNtArr_VerticesPos[index].z), IndexMin(distances, cellsIndex));
        }

        /// <summary>
        /// Get both X/Y grid Range (neighbores around the cell)
        /// Get numCell to check (may be less if the cell checked is on a corner or on an edge of the grid)
        /// </summary>
        /// <param name="cell">index of the current cell checked</param>
        /// <param name="xRange"></param>
        /// <param name="yRange"></param>
        /// <param name="numCell"></param>
        void CellGridRanges(int cell, out int2 xRange, out int2 yRange, out int numCell)
        {
            int y = (int)floor(cell / (float)NumCellJob);
            int x = cell - mul(y, NumCellJob);
            
            bool corner = (x == 0 && y == 0) || (x == 0 && y == NumCellJob - 1) || (x == NumCellJob - 1 && y == 0) || (x == NumCellJob - 1 && y == NumCellJob - 1);
            bool yOnEdge = y == 0 || y == NumCellJob - 1;
            bool xOnEdge = x == 0 || x == NumCellJob - 1;

            //check if on edge 0 : int2(0, 1) ; if not NumCellJob - 1 : int2(-1, 0)
            int2 OnEdge(int e) => select(int2(-1, 0), int2(0, 1), e == 0);
            yRange = select(OnEdge(y), int2(-1, 1), !yOnEdge);
            xRange = select(OnEdge(x), int2(-1, 1), !xOnEdge);
            numCell = select(select(9, 6, yOnEdge || xOnEdge), 4, corner);
        }

        /// <summary>
        /// Find the index of the minimum value of the array
        /// </summary>
        /// <param name="dis">array containing float distance value from point to his neighbors</param>
        /// <param name="cellIndex">array storing index of float2 position of poissonDiscSamples </param>
        /// <returns>index of the closest point</returns>
        int IndexMin(NativeArray<float> dis, NativeArray<int> cellIndex)
        {
            float val = float.MaxValue;
            int index = 0;

            for (int i = 0; i < dis.Length; i++)
            {
                if (dis[i] < val)
                {
                    index = cellIndex[i];
                    val = dis[i];
                }
            }
            return index;
        }

    }
}