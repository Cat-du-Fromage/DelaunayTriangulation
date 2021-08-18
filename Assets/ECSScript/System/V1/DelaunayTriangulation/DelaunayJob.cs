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
    public struct DelaunayCoordsJob : IJobFor
    {
        [ReadOnly] public NativeArray<float2> SamplesJob;
        [NativeDisableParallelForRestriction][WriteOnly]public NativeArray<float> CoordsJob;

        public void Execute(int index)
        {
            if (SamplesJob.Length < 3) return;
            CoordsJob[math.mul(2, index)] = SamplesJob[index].x;
            CoordsJob[math.mad(2, index, 1)] = SamplesJob[index].y;
        }
    }

    /*
     [BurstDiscard]
        public void GetXZ(float x, float z)
        {
            Debug.Log($"x = {x} y = {z}");
        }
     */

    [BurstCompile(CompileSynchronously = true)]
    public struct DelaunayI2Job : IJob
    {
        [ReadOnly] public int NJob;
        [ReadOnly] public int I0Job;
        [ReadOnly] public int I1Job;
        [ReadOnly] public NativeArray<float> CoordsJob;

        //public int I2Job;
        public NativeArray<int> I2Job;
        [WriteOnly] public NativeArray<float2> I2PosJob;

        public void Execute()
        {
            float minRadius = float.PositiveInfinity;

            // find the third point which forms the smallest circumcircle with the first two
            for (int i = 0; i < NJob; i++)
            {
                if (i == I0Job || i == I1Job) continue;
                var r = Circumradius( I0Job, I1Job, new float2(CoordsJob[math.mul(2, i)], CoordsJob[math.mad(2, i, 1)]) );
                if (r < minRadius)
                {
                    I2Job[0] = i;
                    minRadius = r;
                }
            }
            I2PosJob[0] = new float2(CoordsJob[math.mul(2, I2Job[0])], CoordsJob[math.mad(2, I2Job[0], 1)]);
        }

        private float Circumradius(float2 a, float2 b, float2 c)
        {
            float dx = b.x - a.x;
            float dy = b.y - a.y;
            float ex = c.x - a.x;
            float ey = c.y - a.y;
            float bl = math.mul(dx, dx) + math.mul(dy, dy);
            float cl = math.mul(ex, ex) + math.mul(ey, ey);
            float d = 0.5f / (math.mul(dx, ey) - math.mul(dy, ex));
            float x = math.mul((math.mul(ey, bl) - math.mul(dy, cl)), d);
            float y = math.mul((math.mul(dx, cl) - math.mul(ex, bl)), d);
            return math.mul(x,x) + math.mul(y,y);
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    public struct DelaunayHullsJob : IJob
    {
        [ReadOnly] public int NJob;
        [ReadOnly] public int HashSizeJob;
        [ReadOnly] public int I0Job;
        [ReadOnly] public int I1Job;
        [ReadOnly] public int I2Job;
        [ReadOnly] public float2 I0PosJob;
        [ReadOnly] public float2 I1PosJob;
        [ReadOnly] public float2 I2PosJob;
        [ReadOnly] public float2 CenterJob;
        [ReadOnly] public NativeArray<float> CoordsJob;

        public NativeArray<int> IdsJob;
        public NativeArray<int> EDGE_STACK_JOB;
        public NativeArray<int> TrianglesLenJob;
        public NativeArray<int> TrianglesJob;
        public NativeArray<int> HalfEdgesJob;
        public NativeArray<int> HullPrevJob;
        public NativeArray<int> HullNextJob;
        public NativeArray<int> HullTrianglesJob;
        public NativeArray<int> HullHashJob;

        [WriteOnly] public NativeArray<int> HullSizeJob;
        [WriteOnly] public NativeArray<int> HullStartJob;
        public void Execute()
        {
            NativeArray<float> dists = new NativeArray<float>(NJob, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (var i = 0; i < NJob; i++)
            {
                dists[i] = math.distance(new float2(CoordsJob[math.mul(2, i)], CoordsJob[math.mad(2, i, 1)] ), CenterJob);
            }

            Quicksort(IdsJob, dists, 0, NJob - 1);

            int hullStart = I0Job;
            int hullSize = 3;

            HullNextJob[I0Job] = HullPrevJob[I2Job] = I1Job;
            HullNextJob[I1Job] = HullPrevJob[I0Job] = I2Job;
            HullNextJob[I2Job] = HullPrevJob[I1Job] = I0Job;

            HullTrianglesJob[I0Job] = 0;
            HullTrianglesJob[I1Job] = 1;
            HullTrianglesJob[I2Job] = 2;

            HullHashJob[HashKey(I0PosJob.x, I0PosJob.y)] = I0Job;
            HullHashJob[HashKey(I1PosJob.x, I1PosJob.y)] = I1Job;
            HullHashJob[HashKey(I2PosJob.x, I2PosJob.y)] = I2Job;

            TrianglesLenJob[0] = 0;
            AddTriangle(I0Job, I1Job, I2Job, -1, -1, -1);

            float xp = 0;
            float yp = 0;

            for (int k = 0; k < IdsJob.Length; k++)
            {
                int i = IdsJob[k];
                float x = CoordsJob[math.mul(2, i)];
                float y = CoordsJob[math.mad(2, i, 1)];

                // skip near-duplicate points
                if (k > 0 && math.abs(x - xp) <= math.EPSILON && math.abs(y - yp) <= math.EPSILON) continue;
                xp = x;
                yp = y;
                // skip seed triangle points
                if (i == I0Job || i == I1Job || i == I2Job) continue;

                // find a visible edge on the convex hull using edge hash
                int start = 0;
                for (var j = 0; j < HashSizeJob; j++)
                {
                    int key = HashKey(x, y);
                    start = HullHashJob[(int)math.fmod(key + j, HashSizeJob)];
                    if (start != -1 && start != HullNextJob[start]) break;
                }

                start = HullPrevJob[start];
                int e = start;
                int q = HullNextJob[e];

                while ( !IsLeft( new float2(x, y), new float2(CoordsJob[math.mul(2, e)], CoordsJob[math.mad(2, e, 1)]), new float2(CoordsJob[math.mul(2, q)], CoordsJob[math.mad(2, q, 1)]) ) )
                {
                    e = q;
                    if (e == start)
                    {
                        e = int.MaxValue;
                        break;
                    }

                    q = HullNextJob[e];
                }

                if (e == int.MaxValue) continue; // likely a near-duplicate point; skip it

                // add the first triangle from the point
                int t = AddTriangle(e, i, HullNextJob[e], -1, -1, HullTrianglesJob[e]);

                // recursively flip triangles from the point until they satisfy the Delaunay condition
                HullTrianglesJob[i] = Legalize(t + 2, hullStart);
                HullTrianglesJob[e] = t; // keep track of boundary triangles on the hull
                hullSize++;

                // walk forward through the hull, adding more triangles and flipping recursively
                int next = HullNextJob[e];
                q = HullNextJob[next];

                while( IsLeft(new float2(x, y),new float2(CoordsJob[math.mul(2 ,next)], CoordsJob[math.mad(2, next, 1)]), new float2(CoordsJob[math.mul(2, q)], CoordsJob[math.mad(2, q, 1)])) )
                {
                    t = AddTriangle(next, i, q, HullTrianglesJob[i], -1, HullTrianglesJob[next]);
                    HullTrianglesJob[i] = Legalize(t + 2, hullStart);
                    HullNextJob[next] = next; // mark as removed
                    hullSize--;
                    next = q;

                    q = HullNextJob[next];
                }

                // walk backward from the other side, adding more triangles and flipping
                if (e == start)
                {
                    q = HullPrevJob[e];
                    //coords[2 * q], coords[2 * q + 1], coords[2 * e], coords[2 * e + 1]
                    while (IsLeft(new float2(x, y), new float2(CoordsJob[math.mul(2, q)], CoordsJob[math.mad(2, q, 1)]), new float2(CoordsJob[math.mul(2, e)], CoordsJob[math.mad(2, e, 1)]) ))
                    {
                        t = AddTriangle(q, i, e, -1, HullTrianglesJob[e], HullTrianglesJob[q]);
                        Legalize(t + 2, hullStart);
                        HullTrianglesJob[q] = t;
                        HullNextJob[e] = e; // mark as removed
                        hullSize--;
                        e = q;

                        q = HullPrevJob[e];
                    }
                }
                // update the hull indices
                hullStart = HullPrevJob[i] = e;
                HullNextJob[e] = HullPrevJob[next] = i;
                HullNextJob[i] = next;

                // save the two new edges in the hash table
                HullHashJob[HashKey(x, y)] = i;
                HullHashJob[HashKey(CoordsJob[math.mul(2, e)], CoordsJob[math.mad(2, e, 1)])] = e;
            }

            HullSizeJob[0] = hullSize;
            HullStartJob[0] = hullStart;
            /*
             //TO DO! MAKE THIS PART IN A SECOND JOB
            //Need to retrieve
            //- hullSize
            //- hullStart
            //- trianglesLen
            //- HullNextJob
            hull = new int[hullSize];
            int s = hullStart;
            for (int i = 0; i < hullSize; i++)
            {
                hull[i] = s;
                s = hullNext[s];
            }
            
            //// trim typed triangle mesh arrays
            TrianglesJob = Triangles.Take(trianglesLen).ToArray();
            HalfEdgesJob = Halfedges.Take(trianglesLen).ToArray();
            */
            dists.Dispose();
        }

        private int Legalize(int a, int hullStart)
        {
            var i = 0;
            int ar;

            // recursion eliminated with a fixed-size stack
            while (true)
            {
                int b = HalfEdgesJob[a];

                /* if the pair of triangles doesn't satisfy the Delaunay condition
                 * (p1 is inside the circumcircle of [p0, pl, pr]), flip them,
                 * then do the same check/flip recursively for the new pair of triangles
                 *
                 *           pl                    pl
                 *          /||\                  /  \
                 *       al/ || \bl            al/    \a
                 *        /  ||  \              /      \
                 *       /  a||b  \    flip    /___ar___\
                 *     p0\   ||   /p1   =>   p0\---bl---/p1
                 *        \  ||  /              \      /
                 *       ar\ || /br             b\    /br
                 *          \||/                  \  /
                 *           pr                    pr
                 */
                int a0 = (int)(a - math.fmod(a, 3f));
                ar = (int)(a0 + math.fmod(a + 2f, 3f));

                if (b == -1)
                { // convex hull edge
                    if (i == 0) break;
                    a = EDGE_STACK_JOB[--i];
                    continue;
                }

                int b0 = (int)(b - math.fmod(b, 3));
                int al = (int)(a0 + math.fmod(a + 1, 3));
                int bl = (int)(b0 + math.fmod(b + 2, 3));

                int p0 = TrianglesJob[ar];
                int pr = TrianglesJob[a];
                int pl = TrianglesJob[al];
                int p1 = TrianglesJob[bl];

                bool illegal = InCircle(
                    new float2(CoordsJob[math.mul(2, p0)], CoordsJob[math.mad(2, p0, 1)]),
                    new float2(CoordsJob[math.mul(2, pr)], CoordsJob[math.mad(2, pr, 1)]),
                    new float2(CoordsJob[math.mul(2, pl)], CoordsJob[math.mad(2, pl, 1)]),
                    new float2(CoordsJob[math.mul(2, p1)], CoordsJob[math.mad(2, p1, 1)]));

                if (illegal)
                {
                    TrianglesJob[a] = p1;
                    TrianglesJob[b] = p0;

                    int hbl = HalfEdgesJob[bl];

                    // edge swapped on the other side of the hull (rare); fix the halfedge reference
                    if (hbl == -1)
                    {
                        int e = hullStart;
                        do
                        {
                            if (HullTrianglesJob[e] == bl)
                            {
                                HullTrianglesJob[e] = a;
                                break;
                            }
                            e = HullPrevJob[e];
                        } while (e != hullStart);
                    }
                    Link(a, hbl);
                    Link(b, HalfEdgesJob[ar]);
                    Link(ar, bl);

                    int br = (int)(b0 + math.fmod(b + 1, 3));

                    // don't worry about hitting the cap: it can only happen on extremely degenerate input
                    if (i < EDGE_STACK_JOB.Length)
                    {
                        EDGE_STACK_JOB[i++] = br;
                    }
                }
                else
                {
                    if (i == 0) break;
                    a = EDGE_STACK_JOB[--i];
                }
            }

            return ar;
        }

        private bool InCircle(float2 a, float2 b, float2 c, float2 p)
        {
            float dx = a.x - p.x;
            float dy = a.y - p.y;
            float ex = b.x - p.x;
            float ey = b.y - p.y;
            float fx = c.x - p.x;
            float fy = c.y - p.y;

            float ap = math.mul(dx, dx) + math.mul(dy, dy);
            float bp = math.mul(ex, ex) + math.mul(ey, ey);
            float cp = math.mul(fx, fx) + math.mul(fy, fy);

            return math.mul(dx, math.mul(ey, cp) - math.mul(bp, fy)) -
                   math.mul(dy, math.mul(ex, cp) - math.mul(bp, fx)) +
                   math.mul(ap, math.mul(ex, fy) - math.mul(ey, fx)) < 0;
        }

        private int AddTriangle(int i0, int i1, int i2, int a, int b, int c)
        {
            int t = TrianglesLenJob[0];

            TrianglesJob[t] = i0;
            TrianglesJob[t + 1] = i1;
            TrianglesJob[t + 2] = i2;

            Link(t, a);
            Link(t + 1, b);
            Link(t + 2, c);

            TrianglesLenJob[0] += 3;
            return t;
        }
        private void Link(int a, int b)
        {
            HalfEdgesJob[a] = b;
            if (b != -1) HalfEdgesJob[b] = a;
        }

        private void Quicksort(NativeArray<int> ids, NativeArray<float> dists, int left, int right)
        {
            if (right - left <= 20)
            {
                for (int i = left + 1; i <= right; i++)
                {
                    int temp = ids[i];
                    float tempDist = dists[temp];
                    int j = i - 1;
                    while (j >= left && dists[ids[j]] > tempDist) ids[j + 1] = ids[j--];
                    ids[j + 1] = temp;
                }
            }
            else
            {
                int median = (left + right) >> 1;
                int i = left + 1;
                int j = right;
                Swap(ids, median, i);
                if (dists[ids[left]] > dists[ids[right]]) Swap(ids, left, right);
                if (dists[ids[i]] > dists[ids[right]]) Swap(ids, i, right);
                if (dists[ids[left]] > dists[ids[i]]) Swap(ids, left, i);

                int temp = ids[i];
                float tempDist = dists[temp];
                while (true)
                {
                    do i++; while (dists[ids[i]] < tempDist);
                    do j--; while (dists[ids[j]] > tempDist);
                    if (j < i) break;
                    Swap(ids, i, j);
                }
                ids[left + 1] = ids[j];
                ids[j] = temp;

                if (right - i + 1 >= j - left)
                {
                    Quicksort(ids, dists, i, right);
                    Quicksort(ids, dists, left, j - 1);
                }
                else
                {
                    Quicksort(ids, dists, left, j - 1);
                    Quicksort(ids, dists, i, right);
                }
            }
        }

        private void Swap(NativeArray<int> arr, int i, int j)
        {
            int tmp = arr[i];
            arr[i] = arr[j];
            arr[j] = tmp;
        }

        private int HashKey(float x, float y) => (int)math.fmod( math.mul((int)math.floor(PseudoAngle(x - CenterJob.x, y - CenterJob.y)) , HashSizeJob), HashSizeJob);
        private float PseudoAngle(float dx, float dy)
        {
            float p = dx / (math.abs(dx) + math.abs(dy));
            //return (dy > 0 ? 3 - p : 1 + p) / 4; // [0..1]
            return math.select(1 + p, 3 - p, dy > 0);
        }

        private bool IsLeft(float2 a, float2 b, float2 c)
        {
            return ( math.mul(b.x - a.x, c.y - a.y) - math.mul(b.y - a.y, c.x - a.x) ) > 0;
        }
    }

    /*
    //TO DO! MAKE THIS PART IN A SECOND JOB
    //Need to retrieve
    //- hullSize
    //- hullStart
    //- trianglesLen
    //- HullNextJob
    hull = new int[hullSize];
    int s = hullStart;
    for (int i = 0; i < hullSize; i++)
    {
        hull[i] = s;
        s = hullNext[s];
    }
    
    //// trim typed triangle mesh arrays
    TrianglesJob = Triangles.Take(trianglesLen).ToArray();
    HalfEdgesJob = Halfedges.Take(trianglesLen).ToArray();
    */
    public struct DelaunayHulls2Job : IJob
    {
        public NativeArray<int> HullsJob;
        public NativeArray<int> HullNextJob;
        public int HullSizeJob;
        public int HullStartJob;

        public void Execute()
        {
            int s = HullStartJob;
            for (int i = 0; i < HullSizeJob; i++)
            {
                HullsJob[i] = s;
                s = HullNextJob[s];
            }
        }
    }
}