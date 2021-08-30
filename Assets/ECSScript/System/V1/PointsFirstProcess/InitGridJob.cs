using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
namespace KaizerWaldCode.Job
{
    // Use Compute Shader Now
    [BurstCompile(CompileSynchronously = true)]
    public struct VerticesPositionInitJob : IJobFor
    {
        [ReadOnly] public int JMapPointPerAxis;
        [ReadOnly] public float JSpacing;

        [NativeDisableParallelForRestriction]
        [WriteOnly] public NativeArray<float3> JVertices;
        public void Execute(int index)
        {
            int z = (int)floor(index / (float)JMapPointPerAxis);
            int x = index - mul(z, JMapPointPerAxis);

            float3 pointPosition = mul(new float3(x, 0, z), new float3(JSpacing, JSpacing, JSpacing));
            JVertices[index] = pointPosition;
        }
    }
    
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
            JVerticesCellGrid[index] = (int)mad(cellGrid.y, JNumCellMap, cellGrid.x);
        }

        void FindCell(ref float2 cellGrid, float2 vertPos)
        {
            for (int i = 0; i < JNumCellMap; i++)
            {
                if ((int)cellGrid.y == JNumCellMap) cellGrid.y = select(JNumCellMap, i, vertPos.y <= mad(i, JRadius, JRadius));
                if ((int)cellGrid.x == JNumCellMap) cellGrid.x = select(JNumCellMap, i, vertPos.x <= mad(i, JRadius, JRadius));
                if ((int)cellGrid.x != JNumCellMap && (int)cellGrid.y != JNumCellMap) break;
            }
        }
    }
}
