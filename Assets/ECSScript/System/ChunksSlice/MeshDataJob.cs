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
    public struct MeshDataJob : IJobParallelFor
    {
        [ReadOnly] public int ChunkSizeJob;
        [ReadOnly] public int MapSizeJob;
        [ReadOnly] public int ChunkPointPerAxisJob;

        [NativeDisableParallelForRestriction] [WriteOnly] public NativeArray<int> TrianglesJob;
        [NativeDisableParallelForRestriction] [WriteOnly] public NativeArray<float2> UvsJob;
        /*
        [BurstDiscard]
        public void GetXZ(int x, int z, int realindex, int indexer)
        {
            Debug.Log($"x = {x} z = {z}");
            Debug.Log($"realIndex = {realindex} // normalIndex = {indexer}");
        }
        */
        public void Execute(int index)
        {
            int z = (int)math.floor(index / (float)(ChunkPointPerAxisJob-1) );
            int x = index - math.mul(z, ChunkPointPerAxisJob-1);

            if (z < ChunkPointPerAxisJob && x < ChunkPointPerAxisJob)
            {
            
                //int triangleIndex = ( index-(int)math.floor((float)index / (float)(ChunkPointPerAxisJob * z)) ) * 6;

                int triangleIndex = index * 6;
                int z1 = (int)math.floor(index / (float)ChunkPointPerAxisJob);
                int x1 = index - math.mul(z1, ChunkPointPerAxisJob);
                UvsJob[index] = new float2((float)x1 / (float)ChunkPointPerAxisJob, (float)z1 / (float)ChunkPointPerAxisJob);

                if (z < (ChunkPointPerAxisJob-1) && x < (ChunkPointPerAxisJob-1) )
                {
                    //CAREFUL z/x are offseted by -1 so index must be recalculated
                    int realIndex = math.mad(z, ChunkPointPerAxisJob, x);
                    int4 trianglesVertex = new int4(realIndex, realIndex + ChunkPointPerAxisJob + 1, realIndex + ChunkPointPerAxisJob, realIndex + 1);

                    TrianglesJob[triangleIndex] = trianglesVertex.z;
                    TrianglesJob[triangleIndex + 1] = trianglesVertex.y;
                    TrianglesJob[triangleIndex + 2] = trianglesVertex.x;
                    triangleIndex += 3;
                    TrianglesJob[triangleIndex] = trianglesVertex.w;
                    TrianglesJob[triangleIndex + 1] = trianglesVertex.x;
                    TrianglesJob[triangleIndex + 2] = trianglesVertex.y;
                }
            }
        }
    }
}