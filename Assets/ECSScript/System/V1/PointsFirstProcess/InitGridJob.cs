using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace KaizerWaldCode.Job
{
    [BurstCompile(CompileSynchronously = true)]
    public struct InitGridJob : IJobFor
    {
        [ReadOnly] public int MapSizeJob;
        [ReadOnly] public int ChunkPointPerMeterJob;
        [ReadOnly] public int PointPerMeterJob;
        [ReadOnly] public int MapPointPerAxis;
        [ReadOnly] public float SpacingJob;
        [WriteOnly] public NativeArray<float3> VerticesJob;
        public void Execute(int index)
        {
            int z = (int)math.floor(index / (float)MapPointPerAxis);
            int x = index - math.mul(z, MapPointPerAxis);

            float3 pointPosition = math.mad(new float3(x, 0, z), new float3(SpacingJob, SpacingJob, SpacingJob), new float3(MapSizeJob / -2f, 0, MapSizeJob / -2f) );
            VerticesJob[index] = pointPosition;
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    public struct PointGridCellJob : IJobFor
    {
        [ReadOnly] public int MapSizeJob;
        [ReadOnly] public int MapPointPerAxis;
        [ReadOnly] public uint NumCellMap;
        [ReadOnly] public uint Radius;
        [ReadOnly] public NativeArray<float3> VerticesJob;
        [WriteOnly] public NativeArray<float3> VerticesCellGrid;
        public void Execute(int index)
        {
            //int z = (int)math.floor(index / (float)MapPointPerAxis);
            //int x = index - math.mul(z, MapPointPerAxis);

            float2 cellGrid = float2.zero;
            float2 currVertPos = VerticesJob[index].xz + new float2(MapSizeJob / 2f);

            FindCell(ref cellGrid, currVertPos);
            VerticesCellGrid[index] = new float3(currVertPos.x, currVertPos.y, math.mad(cellGrid.y, NumCellMap, cellGrid.x));
        }

        void FindCell(ref float2 cellGrid, float2 vertPos)
        {
            cellGrid.y = NumCellMap;
            for (int yG = 0; yG < NumCellMap; yG++)
            {
                if (vertPos.y <= yG * Radius + Radius)
                {
                    cellGrid.y = yG;
                    break;
                }
            }
            cellGrid.x = NumCellMap;
            for (int xG = 0; xG < NumCellMap; xG++)
            {
                if (vertPos.x <= xG * Radius + Radius)
                {
                    cellGrid.x = xG;
                    break;
                }
            }
        }
    }
}
