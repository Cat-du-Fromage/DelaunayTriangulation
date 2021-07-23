using Unity.Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace KaizerWaldCode.System
{
    public class CreateMeshesSystem : SystemBase
    {
        EntityQueryDesc _eventDescription;
        EntityManager _em;

        protected override void OnCreate()
        {
            _eventDescription = new EntityQueryDesc()
            {
                All = new ComponentType[] {typeof(Data.Events.Event_CreateMesh)},
            };
            RequireForUpdate(GetEntityQuery(_eventDescription));
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
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
                .WithDisposeOnCompletion(triangles)
                .ForEach((Entity ent, int entityInQueryIndex, in DynamicBuffer<Data.Chunks.Vertices> vertices) =>
                {

                    Mesh mesh = new Mesh();
                    mesh.name = $"mesh{entityInQueryIndex}";
                    mesh.indexFormat = IndexFormat.UInt32;
                    
                    mesh.vertices = vertices.Reinterpret<Vector3>().AsNativeArray().ToArray();
                    mesh.triangles = triangles.ToArray();
                    mesh.uv = uvs.ToArray();
                    mesh.RecalculateNormals();
                    mesh.RecalculateBounds();
                    mesh.Optimize();

                    _em.SetComponentData(ent, new RenderBounds
                    {
                        Value = mesh.bounds.ToAABB()/*new AABB
                        {
                            Center = new float3(mesh.bounds.center.x, mesh.bounds.center.y, mesh.bounds.center.z),
                            Extents = new float3(mesh.bounds.extents.x, mesh.bounds.extents.y, mesh.bounds.extents.z)
                        }*/
                    });
                    
                    _em.SetSharedComponentData(ent, new RenderMesh() { material = mat, mesh = mesh });
                    
                    _em.SetComponentData(ent, new WorldRenderBounds() {Value = mesh.bounds.ToAABB()});
                    
                    //_em.RemoveComponent<DisableRendering>(ent);
                }).Run();

            _em.RemoveComponent<Data.Events.Event_CreateMesh>(GetSingletonEntity<Data.Tag.MapEventHolder>());
        }
    }
}