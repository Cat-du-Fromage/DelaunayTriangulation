using System;
using System.Linq;
using KaizerWaldCode.Utils;
using KaizerWaldCode.Data.Chunks;
using KaizerWaldCode.Data.Events;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace KaizerWaldCode.ECSSystem
{
    public class ChunksSliceSystem : SystemBase
    {
        EntityQueryDesc _eventDescription;
        EntityManager _em;
        private EndSimulationEntityCommandBufferSystem ES_Ecb;
        protected override void OnCreate()
        {
            ECSUtils.SystemEventRequire<Event_ChunksSlice>(ref _eventDescription, ref _em);
            RequireForUpdate(GetEntityQuery(_eventDescription));

            ES_Ecb = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            EntityCommandBuffer.ParallelWriter ecbEnd = ES_Ecb.CreateCommandBuffer().AsParallelWriter();

            NativeArray<Vertices> chunkHolderBuffer = GetBuffer<Data.Vertices.VertexPosition>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<Vertices>().ToNativeArray(Allocator.TempJob);

            int numChunks = GetComponent<Data.MapData>(GetSingletonEntity<Data.Tag.MapSettings>()).NumChunk;
            int chunkPoints = GetComponent<Data.MapData>(GetSingletonEntity<Data.Tag.MapSettings>()).ChunkPointPerAxis;
            int mapPoints = GetComponent<Data.MapData>(GetSingletonEntity<Data.Tag.MapSettings>()).MapPointPerAxis;

            Entities
                .WithBurst(synchronousCompilation:true)
                .WithAll<Data.Tag.MapChunk>()
                .WithReadOnly(chunkHolderBuffer)
                .WithDisposeOnCompletion(chunkHolderBuffer)
                .WithNativeDisableParallelForRestriction(chunkHolderBuffer)
                .ForEach((Entity ent, int entityInQueryIndex, ref DynamicBuffer<Vertices> vertices) =>
                {
                    int y = (int)math.floor((float)entityInQueryIndex / (float)numChunks);
                    int x = entityInQueryIndex - math.mul(y, numChunks);
                    NativeArray<Vertices> verticesSlice = new NativeArray<Vertices>(chunkPoints * chunkPoints, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                    for (int i = 0; i < chunkPoints; i++)
                    {
                        int startY = math.mul( math.mul(y, mapPoints) , chunkPoints-1 );
                        int startX = math.mul(x , chunkPoints-1);
                        int startYChunk = math.mul(i, mapPoints);

                        int start = startY + startX + startYChunk;

                        for (int j = 0; j < chunkPoints; j++)
                        {
                            verticesSlice[math.mad(i, chunkPoints, j)] = chunkHolderBuffer[start + j];
                        }
                    }
                    vertices.AddRange(verticesSlice);
                    verticesSlice.Dispose();
                }).ScheduleParallel();
            ES_Ecb.AddJobHandleForProducer(this.Dependency);

            ECSUtils.EndEventSystem<Event_ChunksSlice, Event_ChunksMeshData>(GetSingletonEntity<Data.Tag.MapEventHolder>(), _em);
        }
    }
}
