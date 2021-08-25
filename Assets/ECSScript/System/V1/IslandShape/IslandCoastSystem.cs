using KaizerWaldCode.Data.Events;
using KaizerWaldCode.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

using static Unity.Mathematics.math;
using Random = Unity.Mathematics.Random;

namespace KaizerWaldCode.ECSSystem
{
    public class IslandCoastSystem : SystemBase
    {
        private EntityQueryDesc _eventDescription;
        private EntityManager _em;

        protected override void OnCreate()
        {
            ECSUtils.SystemEventRequire<Event_IslandCoast>(ref _eventDescription, ref _em);
            RequireForUpdate(GetEntityQuery(_eventDescription));
        }

        protected override void OnUpdate()
        {
            int mapSize = GetComponent<Data.MapData>(GetSingletonEntity<Data.Tag.MapSettings>()).MapSize;
            NativeArray<float2> samplesPos = GetBuffer<Data.PoissonDiscSamples.PoissonDiscPosition>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float2>().ToNativeArray(Allocator.TempJob);
            NativeArray<float4> islandPoints = new NativeArray<float4>(samplesPos.Length, Allocator.TempJob);
            float2 val = new float2(-1);
            int realCount = 0;
            for (int i = 0; i < samplesPos.Length; i++)
            {
                if (!samplesPos[i].Equals(val))
                {
                    //Debug.Log($"index = {i} // real index = {realCount}");
                    if (RedBlobImplementation(123, samplesPos[i], mapSize))
                    {
                        islandPoints[i] = new float4(samplesPos[i].x, 0, samplesPos[i].y, 1f);
                        realCount++;
                    }
                    else
                    {
                        islandPoints[i] = new float4(samplesPos[i].x, 0, samplesPos[i].y, 0);
                        realCount++;
                    }
                }
            }

            GetBuffer<Data.Chunks.IslandPoissonDisc>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float4>().CopyFrom(islandPoints);
            samplesPos.Dispose();
            //samplesIndex.Dispose();
            islandPoints.Dispose();
            ECSUtils.EndEventSystem<Event_IslandCoast, Event_Noise>(GetSingletonEntity<Data.Tag.MapEventHolder>(), _em);
        }

        void VoronoiIsland(NativeArray<float2> samplesPosition, ref NativeArray<float4> islandPoints, int mapSize)
        {
            //1235
            int realCount = 0;
            for (int i = 0; i < samplesPosition.Length; i++)
            {
                if (all(samplesPosition[i]) == false) {break;}
                else
                {
                    if (RedBlobImplementation(156, samplesPosition[i], mapSize))
                    {
                        islandPoints[realCount] = new float4(samplesPosition[i].x, 0, samplesPosition[i].y, 1f);
                        realCount++;
                    }
                    else
                    {
                        islandPoints[realCount] = new float4(samplesPosition[i].x, 0, samplesPosition[i].y, 0);
                        realCount++;
                    }
                }
            }
        }

        bool RedBlobImplementation(uint seed, float2 sampleDisc, int mapSize)
        {
            float ISLAND_FACTOR = 1.27f; // 1.0 means no small islands; 2.0 leads to a lot
            float PI2 = UPi.Two;

            float x = 2f * (sampleDisc.x / mapSize - 0.5f);
            float z = 2f * (sampleDisc.y / mapSize - 0.5f);

            float3 point = new float3(x, 0, z);
            //Debug.Log($"x = {x}// z = {z}");
            Random islandRandom = new Random(seed);

            int bumps = islandRandom.NextInt(1, 6);
            float startAngle = islandRandom.NextFloat(PI2); //radians 2 Pi = 360°
            float dipAngle = islandRandom.NextFloat(PI2);
            float dipWidth = islandRandom.NextFloat(0.2f, 0.7f); // = mapSize?

            float angle = atan2(point.z, point.x);
            float lengthMul = 0.5f; // 0.1f : big island 1.0f = small island // by increasing by 0.1 island size is reduced by 1
            float totalLength = lengthMul * max(abs(point.x), abs(point.z)) + length(point);

            //Debug.Log($"angle = {angle}// length = {length}");
            //Sin val Range[-1,1]
            float radialsBase = mad(bumps, angle, startAngle); // bump(1-6) * angle(0.x) + startangle(0.x)
            //Debug.Log($"radialsBase = {radialsBase}");
            float r1Sin = sin(radialsBase + cos((bumps + 3) * angle));
            float r2Sin = sin(radialsBase + sin((bumps + 2) * angle));
            //Debug.Log($"math.cos = {radialsBase + math.cos((bumps + 3) * angle)} //math.sin = {radialsBase + math.sin((bumps + 2) * angle)} ");
            //Debug.Log($"r1Sin = {r1Sin} //r2Sin = {r2Sin} // length = {length}");
            //r1 = 0.5f // r2 = 0.7f
            float radial1 = 0.5f + 0.4f * r1Sin;
            float radial2 = 0.7f - 0.2f * r2Sin;

            if (abs(angle - dipAngle) < dipWidth || abs(angle - dipAngle + PI2) < dipWidth || abs(angle - dipAngle - PI2) < dipWidth)
            {
                radial1 = radial2 = 0.2f;
            }

            if (totalLength < radial1 || (totalLength > radial1 * ISLAND_FACTOR && totalLength < radial2)) { return true; }
            return false;
        }
    }
}
