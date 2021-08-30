using KaizerWaldCode.Job;
using KaizerWaldCode.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

using static KaizerWaldCode.Utils.KWmath;
using static KaizerWaldCode.Utils.NativeCollectionUtils;
using static KaizerWaldCode.ECSDelaunay.DelaunayUtils;
using static Unity.Mathematics.math;
using float2 = Unity.Mathematics.float2;

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
            Entity mapSettings = GetSingletonEntity<Data.Tag.MapSettings>();
            Entity chunkHolder = GetSingletonEntity<Data.Tag.ChunksHolder>();
            #region External parameter
            //Sample Points from Poisson Disc Sample : PoissonDiscSystem
            NativeArray<float2> samplePoints = GetBuffer<Data.PoissonDiscSamples.PoissonDiscPosition>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float2>().ToNativeArray(Allocator.Persistent);
            #endregion External parameter

            if (samplePoints.Length < 3)
            {
                _em.RemoveComponent<Data.Events.Event_Delaunay>(GetSingletonEntity<Data.Tag.MapEventHolder>());
            }
            else
            {
                JobHandle DelaunDependency;
                #region Init Coords
                //INIT COORDS
                //NativeArray<float> coords = AllocNtvAry<float>(samplePoints.Length * 2);
                //DelaunDependency = InitCoords(Dependency, samplePoints.Length, samplePoints, coords);
                //int n = coords.Length >> 1;
                NativeArray<float2> coords = GetBuffer<Data.PoissonDiscSamples.PoissonDiscPosition>(chunkHolder).Reinterpret<float2>().ToNativeArray(Allocator.TempJob);
                #endregion Init Coords

                int n = coords.Length;
                int maxTriangles = mad(2, n, -5);

                float2 center = float2(GetComponent<Data.MapData>(mapSettings).MapSize*0.5f);
                int centerCellId = FindCell(center, GetComponent<Data.PoissonDiscData>(mapSettings).NumCellMap, GetComponent<Data.PoissonDiscData>(mapSettings).Radius);

                //PROCESS IDS
                //we can just copy from poissonDiscIndexCell buffer....
                //maybe not needed since its only needed to get nearest point of circumcenter later
                NativeArray<int> ids = AllocNtvAry<int>(n);
                ProcessIds(n, ids, Dependency);

                int i0 = 0;
                int i1 = 0;
                int i2 = 0;

                float2 i0Pos = float2.zero;
                float2 i1Pos = float2.zero;
                float2 i2Pos = float2.zero;

                (i0, i0Pos) = InitI0(coords, centerCellId, GetComponent<Data.PoissonDiscData>(mapSettings).NumCellMap);
                Debug.Log($"i0 {i0} ; i0Pos = {i0Pos}");

                (i1, i1Pos) = InitI1(i0, i0Pos, coords, GetComponent<Data.PoissonDiscData>(mapSettings).NumCellMap);
                Debug.Log($"i1 {i1} ; i1Pos = {i1Pos}");

                // i2 Calcul
                float minRadius = float.MaxValue;
                // find the third point which forms the smallest circumcircle with the first two
                for (int i = 0; i < n; i++)
                {
                    if (i == i0 || i == i1) continue;
                    float r = Circumradius(i0Pos, i1Pos, coords[i]);
                    if (r < minRadius)
                    {
                        i2 = i;
                        minRadius = r;
                    }
                }
                i2Pos = coords[i2];

                //can't return a single scalar from Job so...
                NativeArray<int> i2Arr = new NativeArray<int>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                NativeArray<float2> i2PosArr = new NativeArray<float2>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                /*
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
                */
                i2 = i2Arr[0];
                i2Pos = i2PosArr[0];
                i2Arr.Dispose();
                i2PosArr.Dispose();
                //Debug.Log($"i0p = {i0Pos}; i1p = {i1Pos}; i2p = {i2Pos}");
                //Debug.Log($"i0 = {i0}; i1 = {i1}; i2 = {i2}");
                if (KWmath.IsLeft(i0Pos, i1Pos, i2Pos))
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
                center = KWmath.GetCircumcenter(i0Pos, i1Pos, i2Pos);

                //Debug.Log($"center = {center}");
                #region Hulls
                int hashSize = (int)math.ceil(math.sqrt(n));
                NativeArray<int> trianglesLen = new NativeArray<int>(1, Allocator.TempJob); // need init value to 0
                NativeArray<int> hullSize = new NativeArray<int>(1, Allocator.TempJob); // need init value to 0
                NativeArray<int> hullStart = new NativeArray<int>(1, Allocator.TempJob); // need init value to 0

                NativeArray<int> EDGE_STACK = new NativeArray<int>(512, Allocator.TempJob);
                NativeArray<int> triangles = new NativeArray<int>(maxTriangles * 3, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                NativeArray<int> halfEdges = new NativeArray<int>(maxTriangles * 3, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

                NativeArray<int> hullPrev = new NativeArray<int>(n, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                NativeArray<int> hullNext = new NativeArray<int>(n, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                NativeArray<int> hullTriangles = new NativeArray<int>(n, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                NativeArray<int> hullHash = new NativeArray<int>(hashSize, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                /*
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
                */
                NativeArray<int> hulls = new NativeArray<int>(hullSize[0], Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                /*
                DelaunayHulls2Job delaunayHulls2Job = new DelaunayHulls2Job()
                {
                    HullsJob = hulls,
                    HullNextJob = hullNext,
                    HullSizeJob = hullSize[0],
                    HullStartJob = hullStart[0]
                };
                delaunayHulls2Job.Run();
                */
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
        }
        //Orient(float2A, float2B, M(X,Y))
        //Oritent  = sign((Bx - Ax) * (Y - Ay) - (By - Ay) * (X - Ax))
        private bool IsLeft(float2 a, float2 b, float2 c)
        {
            return ((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x)) > 0;
        }
    }
}