using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace KaizerWaldCode.System
{
    
    public class CreateChunksSystem : SystemBase
    {
        EntityQueryDesc _eventDescription;
        EntityManager _em;
        private BeginInitializationEntityCommandBufferSystem BI_Ecb;
        protected override void OnCreate()
        {
            _eventDescription = new EntityQueryDesc()
            {
                All = new ComponentType[] { typeof(Data.Events.Event_CreateMapChunks) },
            };
            RequireForUpdate(GetEntityQuery(_eventDescription));
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;

            BI_Ecb = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        }
        protected override void OnStartRunning()
        {
            EntityCommandBuffer ecbBegin = BI_Ecb.CreateCommandBuffer();

            #region Chunks Creation

            Entity mapSetting = GetSingletonEntity<Data.Tag.MapSettings>();

            int numChunk = math.mul(GetComponent<Data.MapData>(mapSetting).NumChunk, GetComponent<Data.MapData>(mapSetting).NumChunk);
            /*
            EntityArchetype chunkArchetype = _em.CreateArchetype
            (
                typeof(Data.Tag.MapChunk),
                typeof(RenderMesh),
                typeof(LocalToWorld),
                typeof(Translation),
                typeof(RenderBounds),
                typeof(Data.Chunks.Vertices),
                typeof(Data.Chunks.Uvs),
                typeof(Data.Chunks.Triangles),
                typeof(DisableRendering)
            );
            */
            NativeArray<Entity> numChunksNativeArray = new NativeArray<Entity>(numChunk, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            
            Entity prefabChunk = GetComponent<Data.Authoring.Auth_ChunkPrefab>(GetSingletonEntity<Data.Tag.ChunksHolder>()).prefab;
            //_em.CreateEntity(chunkArchetype, numChunksNativeArray);
            
            _em.Instantiate(prefabChunk, numChunksNativeArray);

            _em.AddComponent<Data.Chunks.Vertices>(numChunksNativeArray);
            _em.AddComponent<Data.Chunks.Uvs>(numChunksNativeArray);
            _em.AddComponent<Data.Chunks.Triangles>(numChunksNativeArray);
            _em.AddComponent<Data.Tag.MapChunk>(numChunksNativeArray);
            
            ecbBegin.SetBuffer<LinkedEntityGroup>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<Entity>().AddRange(numChunksNativeArray);

            numChunksNativeArray.Dispose();


            ecbBegin.RemoveComponent<Data.Events.Event_CreateMapChunks>(GetSingletonEntity<Data.Tag.MapEventHolder>());
            ecbBegin.AddComponent<Data.Events.Event_ChunksSlice>(GetSingletonEntity<Data.Tag.MapEventHolder>());
            #endregion Chunks Creation
        }

        protected override void OnUpdate()
        {
            /*
            EntityCommandBuffer.ParallelWriter ecbBegin = BI_Ecb.CreateCommandBuffer().AsParallelWriter();

            int numChunks = GetComponent<Data.MapData>(GetSingletonEntity<Data.Tag.MapSettings>()).NumChunk;
            int chunkSize = GetComponent<Data.MapData>(GetSingletonEntity<Data.Tag.MapSettings>()).ChunkSize;
            int mapSize = GetComponent<Data.MapData>(GetSingletonEntity<Data.Tag.MapSettings>()).MapSize;

            Entities
                .WithBurst(synchronousCompilation: true)
                .WithAll<Data.Tag.MapChunk>()
                .ForEach((Entity ent, int entityInQueryIndex, ref Translation position) =>
                {
                    int z = (int)math.floor(entityInQueryIndex / (float)numChunks);
                    int x = entityInQueryIndex - math.mul(z, numChunks);

                    float xPos = (mapSize / -2f) + (chunkSize / 2f) + (x * chunkSize);
                    float zPos = (mapSize / -2f) + (chunkSize / 2f) + (z * chunkSize);

                    float xOffset = x * chunkSize;
                    float zOffset = z * chunkSize;

                    float3 newPos = new float3(xPos, 0, zPos);
                    ecbBegin.SetComponent(entityInQueryIndex, ent, new Translation() {Value = newPos});

                    Debug.Log($"{x},{z}  newPos : {newPos}");
                    //Debug.Log($"x{x} : {xPos} // y{z} : {zPos}");
                }).ScheduleParallel();
            BI_Ecb.AddJobHandleForProducer(Dependency);
            */
        }
    }
}
