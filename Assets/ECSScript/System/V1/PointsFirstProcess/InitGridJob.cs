using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace KaizerWaldCode.Job
{
    /*
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
    */
    [BurstCompile(CompileSynchronously = true)]
    public struct VerticesPositionInitJob : IJobFor
    {
        [ReadOnly] public int JMapPointPerAxis;
        [ReadOnly] public float JSpacing;

        [NativeDisableParallelForRestriction]
        [WriteOnly] public NativeArray<float3> JVertices;
        public void Execute(int index)
        {
            int z = (int)math.floor(index / (float)JMapPointPerAxis);
            int x = index - math.mul(z, JMapPointPerAxis);

            float3 pointPosition = math.mul(new float3(x, 0, z), new float3(JSpacing, JSpacing, JSpacing));
            JVertices[index] = pointPosition;
        }
    }
    /*
    [BurstCompile(CompileSynchronously = true)]
    public struct PointGridCellJob : IJobFor
    {
        [ReadOnly] public int MapSizeJob;
        [ReadOnly] public int MapPointPerAxis;
        [ReadOnly] public int NumCellMap;
        [ReadOnly] public int Radius;
        [ReadOnly] public NativeArray<float3> VerticesJob;
        [WriteOnly] public NativeArray<float3> VerticesCellGrid;
        public void Execute(int index)
        {
            //int z = (int)math.floor(index / (float)MapPointPerAxis);
            //int x = index - math.mul(z, MapPointPerAxis);

            float2 cellGrid = new float2(NumCellMap, NumCellMap);
            float2 currVertPos = VerticesJob[index].xz + new float2(MapSizeJob / 2f);

            FindCell(ref cellGrid, currVertPos);
            VerticesCellGrid[index] = new float3(currVertPos.x, currVertPos.y, math.mad(cellGrid.y, NumCellMap, cellGrid.x));
        }

        void FindCell(ref float2 cellGrid, float2 vertPos)
        {
            //cellGrid.y = NumCellMap;
            for (int yG = 0; yG < NumCellMap; yG++)
            {
                if (vertPos.y <= math.mad(yG, Radius, Radius))
                {
                    cellGrid.y = yG;
                    break;
                }
            }
            //cellGrid.x = NumCellMap;
            for (int xG = 0; xG < NumCellMap; xG++)
            {
                if (vertPos.x <= math.mad(xG, Radius, Radius))
                {
                    cellGrid.x = xG;
                    break;
                }
            }
        }
    }
    */
    [BurstCompile(CompileSynchronously = true)]
    public struct VerticesCellIndexJob : IJobFor
    {
        [ReadOnly] public int JNumCellMap;
        [ReadOnly] public int JRadius;
        [ReadOnly] public NativeArray<float3> JVertices;

        [NativeDisableParallelForRestriction]
        [WriteOnly] public NativeArray<int> JVerticesCellGrid;
        public void Execute(int index)
        {
            float2 cellGrid = new float2(JNumCellMap);
            float2 currVertPos = JVertices[index].xz;

            FindCell(ref cellGrid, currVertPos);
            JVerticesCellGrid[index] = (int)math.mad(cellGrid.y, JNumCellMap, cellGrid.x);
        }

        void FindCell(ref float2 cellGrid, float2 vertPos)
        {
            /*
            for (int yG = 0; yG < JNumCellMap; yG++)
            {
                if (vertPos.y <= math.mad(yG, JRadius, JRadius))
                {
                    cellGrid.y = yG;
                    break;
                }
            }
            for (int xG = 0; xG < JNumCellMap; xG++)
            {
                if (vertPos.x <= math.mad(xG, JRadius, JRadius))
                {
                    cellGrid.x = xG;
                    break;
                }
            }
            */
            
            //TRY THIS
            for (int i = 0; i < JNumCellMap; i++)
            {
                if ((int)cellGrid.y == JNumCellMap) cellGrid.y = math.select(JNumCellMap, i, vertPos.y <= math.mad(i, JRadius, JRadius));
                if ((int)cellGrid.x == JNumCellMap) cellGrid.x = math.select(JNumCellMap, i, vertPos.x <= math.mad(i, JRadius, JRadius));
                if((int)cellGrid.x != JNumCellMap && (int)cellGrid.y != JNumCellMap) break;
            }
            
        }
    }
}
