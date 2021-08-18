using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace KaizerWaldCode.System
{
    public class IslandCoastSystem : SystemBase
    {
        private EntityQueryDesc _eventDescription;
        private EntityManager _em;

        protected override void OnCreate()
        {
            _eventDescription = new EntityQueryDesc()
            {
                All = new ComponentType[] {typeof(Data.Events.Event_IslandCoast) },
            };
            RequireForUpdate(GetEntityQuery(_eventDescription));
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        protected override void OnUpdate()
        {
            int mapSize = GetComponent<Data.MapData>(GetSingletonEntity<Data.Tag.MapSettings>()).MapSize;

            /*
            using NativeArray<float2> samplePoints = GetBuffer<Data.Chunks.PoissonDiscGrid>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float2>().ToNativeArray(Allocator.TempJob);
            NativeArray<float4> islandPoints = new NativeArray<float4>(samplePoints.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            //1235
            for (int i = 0; i < samplePoints.Length; i++)
            {
                if (RedBlobImplementation(69, samplePoints[i], mapSize))
                {
                    islandPoints[i] = new float4(samplePoints[i].x - (mapSize/2f),0, samplePoints[i].y - (mapSize / 2f), 1f);
                }
                else
                {
                    islandPoints[i] = new float4(samplePoints[i].x - (mapSize / 2f), 0, samplePoints[i].y - (mapSize / 2f), 0);
                }
            }
            */
            NativeArray<float3> samplePoints = GetBuffer<Data.Chunks.PDiscGrid>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float3>().ToNativeArray(Allocator.TempJob);
            NativeArray<float4> islandPoints = new NativeArray<float4>(GetBuffer<Data.Chunks.PoissonDiscGrid>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            //NewVoronoiIsland(ref samplePoints, ref islandPoints, mapSize);

            int realCount = 0;
            for (int i = 0; i < samplePoints.Length; i++)
            {
                if (math.all(samplePoints[i].xy))
                {
                    Debug.Log($"index = {i} // real index = {realCount}");
                    if (RedBlobImplementation(69, samplePoints[i].xy, mapSize))
                    {
                        islandPoints[realCount] = new float4(samplePoints[i].x - (mapSize / 2f), 0, samplePoints[i].y - (mapSize / 2f), 1f);
                        realCount++;
                    }
                    else
                    {
                        islandPoints[realCount] = new float4(samplePoints[i].x - (mapSize / 2f), 0, samplePoints[i].y - (mapSize / 2f), 0);
                        realCount++;
                    }
                }
            }

            GetBuffer<Data.Chunks.IslandPoissonDisc>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float4>().CopyFrom(islandPoints);
            samplePoints.Dispose();
            islandPoints.Dispose();
            _em.RemoveComponent<Data.Events.Event_IslandCoast>(GetSingletonEntity<Data.Tag.MapEventHolder>());
            _em.AddComponent<Data.Events.Event_Noise>(GetSingletonEntity<Data.Tag.MapEventHolder>());
        }

        void NewVoronoiIsland(ref NativeArray<float3> samplePoints, ref NativeArray<float4> islandPoints, int mapSize)
        {
            //1235
            int realCount = 0;
            for (int i = 0; i < samplePoints.Length; i++)
            {
                if (math.all(samplePoints[i].xy) == false) {break;}
                else
                {
                    if (RedBlobImplementation(69, samplePoints[i].xy, mapSize))
                    {
                        islandPoints[realCount] = new float4(samplePoints[i].x, 0, samplePoints[i].y, 1f);
                        realCount++;
                    }
                    else
                    {
                        islandPoints[realCount] = new float4(samplePoints[i].x, 0, samplePoints[i].y, 0);
                        realCount++;
                    }
                }
            }
        }

        bool RedBlobImplementation(uint seed, float2 sampleDisc, int mapSize)
        {
            float ISLAND_FACTOR = 1.27f; // 1.0 means no small islands; 2.0 leads to a lot
            float PI2 = math.PI*2;

            float x = 2f * (sampleDisc.x / mapSize - 0.5f);
            float z = 2f * (sampleDisc.y / mapSize - 0.5f);

            float3 point = new float3(x, 0, z);
            //Debug.Log($"x = {x}// z = {z}");
            Unity.Mathematics.Random islandRandom = new Unity.Mathematics.Random(seed);

            int bumps = islandRandom.NextInt(1, 6);
            float startAngle = islandRandom.NextFloat(PI2); //radians 2 Pi = 360°
            float dipAngle = islandRandom.NextFloat(PI2);
            float dipWidth = islandRandom.NextFloat(0.2f, 0.7f); // = mapSize?

            float angle = math.atan2(point.z, point.x);
            float lengthMul = 0.5f; // 0.1f : big island 1.0f = small island // by increasing by 0.1 island size is reduced by 1
            float length = lengthMul * math.max(math.abs(point.x), math.abs(point.z)) + math.length(point);

            //Debug.Log($"angle = {angle}// length = {length}");
            //Sin val Range[-1,1]
            float radialsBase = math.mad(bumps, angle, startAngle); // bump(1-6) * angle(0.x) + startangle(0.x)

            float r1Sin = math.sin(radialsBase + math.cos((bumps + 3) * angle));
            float r2Sin = math.sin(radialsBase + math.sin((bumps + 2) * angle));

            //r1 = 0.5f // r2 = 0.7f
            float radial1 = 0.5f + 0.4f * r1Sin;
            float radial2 = 0.7f - 0.2f * r2Sin;

            if (math.abs(angle - dipAngle) < dipWidth || math.abs(angle - dipAngle + PI2) < dipWidth || math.abs(angle - dipAngle - PI2) < dipWidth)
            {
                radial1 = radial2 = 0.2f;
            }

            if (length < radial1 || (length > radial1 * ISLAND_FACTOR && length < radial2)) { return true; }
            return false;
        }
    }
}
