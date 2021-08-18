using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace KaizerWaldCode.Job
{
    [BurstCompile(CompileSynchronously = true)]
    public struct VoronoiDebugJob : IJobFor
    {
        [ReadOnly] public int MapSizeJob;

        [ReadOnly] public NativeArray<float2> SamplePointsJob;

        [WriteOnly] public NativeArray<Color> VoronoiColorJob;

        [BurstDiscard]
        public void GetXZ(float x, float z)
        {
            Debug.Log($"D0 = {x} D1 = {z}");
        }
        public void Execute(int index)
        {
            int y = (int)math.floor(index / (float)MapSizeJob);
            int x = index - math.mul(y, MapSizeJob);

            NativeArray<float> distances = new NativeArray<float>(SamplePointsJob.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < SamplePointsJob.Length; i++)
            {
                distances[i] = math.distance(new float2(x, y), SamplePointsJob[i]);
            }
            distances.Sort();
            float sample = distances[0]/ (20f);
            //VoronoiColorJob[index] = Color.Lerp(Color.white, Color.black, sample);
            VoronoiColorJob[index] = new Color(sample, sample, sample);
            distances.Dispose();
        }
    }


    [BurstCompile(CompileSynchronously = true)]
    public struct VoronoiInitSamplesJob : IJobFor
    {
        [ReadOnly] public NativeArray<float3> SamplePointsJob;
        [WriteOnly] public NativeArray<float4> SamplePointsGridJob;
        public void Execute(int index)
        {
            SamplePointsGridJob[index] = new float4(SamplePointsJob[index], index);
        }
    }


    /// <summary>
    /// There is a more efficient way to fin the closest we use in Poisson Disc
    /// To do Later: since we do it only once in the programme no eed to optimize
    /// </summary>
    [BurstCompile(CompileSynchronously = true)]
    public struct VoronoiJob : IJobFor
    {
        [ReadOnly] public NativeArray<float3> VerticesJob;
        [ReadOnly] public NativeArray<float4> SamplePointsGrid;
        [WriteOnly] public NativeArray<float4> VerticesGridJob;

        public void Execute(int index)
        {
            NativeArray<float> distance = new NativeArray<float>(SamplePointsGrid.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (int j = 0; j < SamplePointsGrid.Length; j++)
            {
                distance[j] = math.distancesq(VerticesJob[index], SamplePointsGrid[j].xyz);
            }
            VerticesGridJob[index] = new float4(VerticesJob[index], SamplePointsGrid[IndexMin(distance)].w);
            //distance.Dispose();
        }

        /// <summary>
        /// Find the index of the minimum value of the array
        /// </summary>
        /// <param name="dis"></param>
        /// <returns></returns>
        int IndexMin(NativeArray<float> dis)
        {
            float val = float.PositiveInfinity;
            int index = -1;

            for (int i = 0; i < dis.Length; i++)
            {
                if (dis[i] < val)
                {
                    index = i;
                    val = dis[i];
                }
            }
            return index;
        }
    }


    [BurstCompile(CompileSynchronously = true)]
    public struct VoronoiCellGridJob : IJobFor
    {
        [ReadOnly] public int MapSizeJob;
        [ReadOnly] public int NumCellJob;
        [ReadOnly] public int RadiusJob;
        [ReadOnly] public NativeArray<float3> VerticesCellGridJob;
        [ReadOnly] public NativeArray<float3> SampleCellGridJob;
        [WriteOnly] public NativeArray<float4> VoronoiVerticesJob;
        public void Execute(int index)
        {
            //Left : tleft:index +(numCell - 1) / tMid:index - 1 / tbot:index - (numCell + 1)
            //if (math.all(SampleCellGridJob[index])) return;

            int2 start = new int2(-1);
            int2 end = new int2(1);
            int numCell = 0;
            CellGridStartEnd((int)VerticesCellGridJob[index].z, ref start, ref end, ref numCell); // need cellGrid from vertex

            NativeArray<float2> cells = new NativeArray<float2>(numCell, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            int cellCount = 0;
            for (int y = start.y; y <= end.y; y++)
            {
                for (int x = start.x; x <= end.x; x++)
                {
                    float2 offset = new float2(x, y);
                    int indexCellOffset = (int)VerticesCellGridJob[index].z + math.mad(y, NumCellJob, x);
                    cells[cellCount] = SampleCellGridJob[indexCellOffset].xz;
                    cellCount++;
                }
            }

            NativeArray<float> distances = new NativeArray<float>(numCell, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < numCell; i++)
            {
                distances[i] = math.distancesq(VerticesCellGridJob[index].xy, SampleCellGridJob[i].xy);
            }

            float xPos = VerticesCellGridJob[index].x - (MapSizeJob / 2f);
            float yPos = VerticesCellGridJob[index].y - (MapSizeJob / 2f);
            //FAUX: now we dont iterate over all points!!
            VoronoiVerticesJob[index] = new float4(new float3(xPos,0, yPos), SampleCellGridJob[IndexMin(distances)].z);
        }

        void CellGridStartEnd(int cell, ref int2 start, ref int2 end, ref int numCell)
        {
            int y = (int)math.floor(cell / (float)NumCellJob);
            int x = cell - math.mul(y, NumCellJob);
            numCell = 4;
            if (y == 0)
            {
                if (x == 0)
                {
                    start = int2.zero;
                    end = new int2(1); // 2x*2y = 4
                }
                else if (x == NumCellJob-1)
                {
                    start = new int2(-1, 0);
                    end = new int2(0, 1); // 2x * 2y = 4
                }
                else
                {
                    start = new int2(-1, 0);
                    end = new int2(1); // 3x * 2y = 6
                    numCell = 6;
                }
            }
            else if (y == NumCellJob-1)
            {
                if (x == 0)
                {
                    start = new int2(0, -1);
                    end = new int2(1, 0); // 2x * 2y = 4
                }
                else if (x == NumCellJob-1)
                {
                    start = new int2(-1);
                    end = int2.zero; // 2x * 2y = 4
                }
                else
                {
                    start = new int2(-1);
                    end = new int2(1, 0); // 3x * 2y = 6
                    numCell = 6;
                }
            }
            else if (x == 0)
            {
                start = new int2(0, -1);
                end = new int2(1); // 2x * 3y = 6
                numCell = 6;
            }
            else if (x == NumCellJob-1)
            {
                start = new int2(-1);
                end = new int2(0, 1); // 2x * 3y = 6
                numCell = 6;
            }
            else
            {
                start = new int2(-1);
                end = new int2(1); // 3x * 3y = 9
                numCell = 9;
            }
        }

        /// <summary>
        /// Find the index of the minimum value of the array
        /// </summary>
        /// <param name="dis"></param>
        /// <returns></returns>
        int IndexMin(NativeArray<float> dis)
        {
            float val = float.PositiveInfinity;
            int index = -1;

            for (int i = 0; i < dis.Length; i++)
            {
                if (dis[i] < val)
                {
                    index = i;
                    val = dis[i];
                }
            }
            return index;
        }

    }
}