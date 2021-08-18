using Unity.Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using Material = UnityEngine.Material;

namespace KaizerWaldCode.System
{
    public class CreateMeshesSystem : SystemBase
    {
        private EntityQueryDesc _eventDescription;
        private EntityManager _em;
        private BeginInitializationEntityCommandBufferSystem BI_Ecb;
        protected override void OnCreate()
        {
            _eventDescription = new EntityQueryDesc()
            {
                All = new ComponentType[] {typeof(Data.Events.Event_CreateMesh)},
            };
            RequireForUpdate(GetEntityQuery(_eventDescription));
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;

            BI_Ecb = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            NativeArray<Entity> chunks = GetEntityQuery(typeof(Data.Tag.MapChunk)).ToEntityArray(Allocator.Temp);
            Entity singleChunk = chunks[0];
            chunks.Dispose();

            NativeArray<Vector2> uvs = GetBuffer<Data.Chunks.Uvs>(singleChunk).Reinterpret<Vector2>().ToNativeArray(Allocator.TempJob);
            NativeArray<int> triangles = GetBuffer<Data.Chunks.Triangles>(singleChunk).Reinterpret<int>().ToNativeArray(Allocator.TempJob);

            Material mat = _em.GetComponentObject<Data.Authoring.Authoring_ChunksMaterial>(GetSingletonEntity<Data.Tag.ChunksHolder>()).mat;

            Entities
            .WithoutBurst()
            .WithStructuralChanges()
            .WithAll<Data.Tag.MapChunk>()
            .WithReadOnly(mat)
            .WithReadOnly(uvs)
            .WithReadOnly(triangles)
            .WithDisposeOnCompletion(uvs)
            .ForEach((Entity ent, int entityInQueryIndex, in DynamicBuffer<Data.Chunks.Vertices> vertices) =>
            {
                Mesh mesh = new Mesh();
                mesh.name = $"mesh{entityInQueryIndex}";
                mesh.indexFormat = IndexFormat.UInt32;
                
                mesh.vertices = vertices.Reinterpret<Vector3>().AsNativeArray().ToArray();
                mesh.triangles = triangles.ToArray();
                mesh.uv = uvs.ToArray();
                mesh.Optimize();
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();

                _em.SetComponentData(ent, new RenderBounds {Value = mesh.bounds.ToAABB()});
                
                _em.SetSharedComponentData(ent, new RenderMesh() { material = mat, mesh = mesh });
                
                _em.SetComponentData(ent, new WorldRenderBounds() {Value = mesh.bounds.ToAABB()});
                
                _em.RemoveComponent<DisableRendering>(ent);
            }).Run();

            //Add Collider to the Meshes
            EntityCommandBuffer.ParallelWriter ecbBegin = BI_Ecb.CreateCommandBuffer().AsParallelWriter();
            Entities
            .WithBurst()
            .WithAll<Data.Tag.MapChunk>()
            .WithReadOnly(triangles)
            .WithDisposeOnCompletion(triangles)
            .ForEach((Entity ent, int entityInQueryIndex, in DynamicBuffer<Data.Chunks.Vertices> vertices) =>
            {
                int index = 0;
                NativeArray<int3> meshTris = new NativeArray<int3>(triangles.Length / 3, Allocator.Temp);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    meshTris[index++] = new int3(triangles[i], triangles[i + 1], triangles[i + 2]);
                }
                Unity.Physics.Material physMat = Unity.Physics.Material.Default;
                physMat.CollisionResponse = CollisionResponsePolicy.CollideRaiseCollisionEvents;
                BlobAssetReference<Unity.Physics.Collider> meshColliderReference = Unity.Physics.MeshCollider.Create(vertices.Reinterpret<float3>().AsNativeArray(), meshTris, new CollisionFilter() {CollidesWith = ~0u, BelongsTo = 1<<0}, physMat);
                ecbBegin.AddComponent(entityInQueryIndex, ent, new PhysicsCollider() { Value = meshColliderReference });
                meshTris.Dispose();
            })
            .WithName("CreateMeshes")
            .ScheduleParallel();
            BI_Ecb.AddJobHandleForProducer(Dependency);

            _em.RemoveComponent<Data.Events.Event_CreateMesh>(GetSingletonEntity<Data.Tag.MapEventHolder>());
        }
    }
}