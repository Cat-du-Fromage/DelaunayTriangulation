using KaizerWaldCode.Job;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace KaizerWaldCode.ECSSystem
{
    public class DelaunaySystem : SystemBase
    {
        private EntityQueryDesc _eventDescription;
        private EntityManager _em;

        protected override void OnCreate()
        {
            _eventDescription = new EntityQueryDesc()
            {
                All = new ComponentType[] { typeof(Data.Events.Event_Delaunay) },
            };
            RequireForUpdate(GetEntityQuery(_eventDescription));
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }
        protected override void OnUpdate()
        {
            Entity delaunayEntity = GetSingletonEntity<Data.Tag.Delaunay>();
            #region External parameter
            //Sample Points from Poisson Disc Sample : PoissonDiscSystem
            NativeArray<float2> samplePoints = GetBuffer<Data.Chunks.PoissonDiscGrid>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float2>().ToNativeArray(Allocator.Persistent);
            #endregion External parameter

            #region Init Coords
            NativeArray<float> coords = new NativeArray<float>(samplePoints.Length * 2, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            DelaunayCoordsJob delaunayCoordsJob = new DelaunayCoordsJob()
            {
                SamplesJob = samplePoints,
                CoordsJob = coords,
            };
            JobHandle delaunayCoordsJobHandle = delaunayCoordsJob.ScheduleParallel(samplePoints.Length, JobsUtility.JobWorkerCount - 1, Dependency);
            delaunayCoordsJobHandle.Complete();
            int n = coords.Length >> 1;
            #endregion Init Coords

            int maxTriangles = 0;
            float2 center = float2.zero;

            float minX = float.PositiveInfinity;
            float minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float maxY = float.NegativeInfinity;

            NativeArray<int> ids = new NativeArray<int>(n, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            //Process Min/Max coordXY and calcul center:(min+max)/2
            Job
            .WithBurst()
            .WithCode(() =>
            {
                maxTriangles = math.mad(2, n, -5);
                for (int i = 0; i < n; i++)
                {
                    var x = coords[math.mul(2, i)];
                    var y = coords[math.mad(2, i, 1)];
                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                    ids[i] = i;
                }
                center.x = (minX + maxX) / 2f;
                center.y = (minY + maxY) / 2f;
            }).Run();
            //Debug.Log($"centerX {centerX} ; centerY = {centerY}");
            //Debug.Log($"max tri {maxTriangles}");
            //Debug.Log($"minx = {minX}; miny = {minY}; maxx = {maxX}; maxy = {maxY}");

            int i0 = 0;
            float2 i0Pos = float2.zero;
            int i1 = 0;
            float2 i1Pos = float2.zero;
            int i2 = 0;
            float2 i2Pos = float2.zero;
            Job
            .WithBurst()
            .WithCode(() =>
            {
                float minDist = float.PositiveInfinity;
                // pick a seed point close to the center
                for (int i = 0; i < n; i++)
                {
                    float distance1 = math.distance(center, new float2(coords[math.mul(2, i)], coords[math.mad(2, i, 1)] ));
                    if (distance1 < minDist)
                    {
                        i0 = i;
                        minDist = distance1;
                    }
                }
                i0Pos.x = coords[math.mul(2, i0)];
                i0Pos.y = coords[math.mad(2, i0, 1)];

                // find the point closest to the seed
                minDist = float.PositiveInfinity; // reset min Distance
                for (int i = 0; i < n; i++)
                {
                    if (i == i0) continue;
                    float distance2 = math.distance(i0Pos, new float2(coords[math.mul(2, i)], coords[math.mad(2, i, 1)]));
                    if (distance2 < minDist && distance2 > 0)
                    {
                        i1 = i;
                        minDist = distance2;
                    }
                }
                i1Pos.x = coords[math.mul(2, i1)];
                i1Pos.y = coords[math.mad(2, i1, 1)];
            }).Run();
            // i2 Calcul
            //can't return a single scalar from Job so...
            NativeArray<int> i2Arr = new NativeArray<int>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NativeArray<float2> i2PosArr = new NativeArray<float2>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            DelaunayI2Job delaunayI2Job = new DelaunayI2Job()
            {
                NJob = n,
                I0Job = i0,
                I1Job = i1,
                CoordsJob = coords,
                I2Job = i2Arr,
                I2PosJob = i2PosArr,
            };
            delaunayI2Job.Run();
            i2 = i2Arr[0];
            i2Pos = i2PosArr[0];
            i2Arr.Dispose();
            i2PosArr.Dispose();
            //Debug.Log($"i0p = {i0Pos}; i1p = {i1Pos}; i2p = {i2Pos}");
            //Debug.Log($"i0 = {i0}; i1 = {i1}; i2 = {i2}");
            if (IsLeft(i0Pos,i1Pos,i2Pos))
            {
                int i = i1;
                float x = i1Pos.x;
                float y = i1Pos.y;
                i1 = i2;
                i1Pos.x = i2Pos.x;
                i1Pos.y = i2Pos.y;
                i2 = i;
                i2Pos.x = x;
                i2Pos.y = y;
            }
            //original :
            //float2 circumCenter = Circumcenter(i0x, i0y, i1x, i1y, i2x, i2y);
            //center = circumCenter;
            center = Circumcenter(i0Pos, i1Pos, i2Pos);

            Debug.Log($"center = {center}");
            #region Hulls
            int hashSize = (int)math.ceil(math.sqrt(n));
            NativeArray<int> trianglesLen = new NativeArray<int>(1, Allocator.TempJob); // need init value to 0
            NativeArray<int> hullSize = new NativeArray<int>(1, Allocator.TempJob); // need init value to 0
            NativeArray<int> hullStart = new NativeArray<int>(1, Allocator.TempJob); // need init value to 0

            NativeArray<int> EDGE_STACK = new NativeArray<int>(512, Allocator.TempJob);
            NativeArray<int> triangles = new NativeArray<int>(maxTriangles*3, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NativeArray<int> halfEdges = new NativeArray<int>(maxTriangles*3, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            NativeArray<int> hullPrev = new NativeArray<int>(n, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NativeArray<int> hullNext = new NativeArray<int>(n, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NativeArray<int> hullTriangles = new NativeArray<int>(n, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NativeArray<int> hullHash = new NativeArray<int>(hashSize, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            DelaunayHullsJob delaunayHullsJob = new DelaunayHullsJob()
            {
                NJob = n,
                HashSizeJob = hashSize,
                I0Job = i0,
                I1Job = i1,
                I2Job = i2,
                I0PosJob = i0Pos,
                I1PosJob = i1Pos,
                I2PosJob = i2Pos,
                CenterJob = center,
                CoordsJob = coords,

                IdsJob = ids,
                EDGE_STACK_JOB = EDGE_STACK,
                TrianglesLenJob = trianglesLen,
                TrianglesJob = triangles,
                HalfEdgesJob = halfEdges,
                HullPrevJob = hullPrev,
                HullNextJob = hullNext,
                HullTrianglesJob = hullTriangles,
                HullHashJob = hullHash,

                HullSizeJob = hullSize,
                HullStartJob = hullStart
            };
            delaunayHullsJob.Run();

            NativeArray<int> hulls = new NativeArray<int>(hullSize[0], Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            DelaunayHulls2Job delaunayHulls2Job = new DelaunayHulls2Job()
            {
                HullsJob = hulls,
                HullNextJob = hullNext,
                HullSizeJob = hullSize[0],
                HullStartJob = hullStart[0]
            };
            delaunayHulls2Job.Run();
            #endregion Hulls
            SetComponent(delaunayEntity, new Data.Delaunay.Data_Delaunay()
            {
                HashSize = hashSize,
                Center = center,
                TrianglesLen = trianglesLen[0],
                HullStart = hullStart[0],
                HullSize = hullSize[0]
            });

            #region SET BUFFERS

            #endregion SET BUFFERS


            samplePoints.Dispose();
            coords.Dispose();
            ids.Dispose();
            trianglesLen.Dispose();
            hullSize.Dispose();
            hullStart.Dispose();
            EDGE_STACK.Dispose();
            triangles.Dispose();
            halfEdges.Dispose();
            hullPrev.Dispose();
            hullNext.Dispose();
            hullTriangles.Dispose();
            hullHash.Dispose();
            hulls.Dispose();
            _em.RemoveComponent<Data.Events.Event_Delaunay>(GetSingletonEntity<Data.Tag.MapEventHolder>());
        }
        //Orient(float2A, float2B, M(X,Y))
        //Oritent  = sign((Bx - Ax) * (Y - Ay) - (By - Ay) * (X - Ax))
        private bool IsLeft(float2 a, float2 b, float2 c)
        {
            return ((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x)) > 0;
        }

        private float Circumradius(float2 a, float2 b, float2 c)
        {
            float dx = b.x - a.x;
            float dy = b.y - a.y;
            float ex = c.x - a.x;
            float ey = c.y - a.y;
            float bl = dx * dx + dy * dy;
            float cl = ex * ex + ey * ey;
            float d = 0.5f / (dx * ey - dy * ex);
            float x = (ey * bl - dy * cl) * d;
            float y = (dx * cl - ex * bl) * d;
            return x * x + y * y;
        }

        private float2 Circumcenter(float2 a, float2 b, float2 c)
        {
            float dx = b.x - a.x;
            float dy = b.y - a.y;
            float ex = c.x - a.x;
            float ey = c.y - a.y;
            float bl = dx * dx + dy * dy;
            float cl = ex * ex + ey * ey;
            float d = 0.5f / (dx * ey - dy * ex);
            float x = a.x + (ey * bl - dy * cl) * d;
            float y = a.y + (dx * cl - ex * bl) * d;

            return new float2(x, y);
        }
    }
}