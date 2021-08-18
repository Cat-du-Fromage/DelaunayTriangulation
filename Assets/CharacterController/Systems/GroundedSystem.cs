using CharacterControllerECS.Data.Move;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;
using ccMove = CharacterControllerECS.Data.Move;
using static CharacterControllerECS.Utils.RaycastUtils;

namespace CharacterControllerECS.System
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(EndFramePhysicsSystem))]
    public class GroundedSystem : SystemBase
    {
        private EntityQueryDesc _eventDescription;
        private EntityManager _em;
        BuildPhysicsWorld _buildPhysicsWorldSystem;
        StepPhysicsWorld _stepPhysicsWorldSystem;

        

        protected override void OnCreate()
        {
            _eventDescription = new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    typeof(Data.Tags.PlayerBodyTag),
                    typeof(PhysicsCollider),
                },

            };
            RequireForUpdate(GetEntityQuery(_eventDescription));
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;

            _buildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
            _stepPhysicsWorldSystem = World.GetOrCreateSystem<StepPhysicsWorld>();
        }



        protected override void OnUpdate()
        {
            Entity player = GetSingletonEntity<Data.Tags.PlayerControllerTag>();
            CollisionWorld collisionWorld = _buildPhysicsWorldSystem.PhysicsWorld.CollisionWorld;

            Entities
            .WithName("PlayerGroundCheck")
            .WithAll<Data.Tags.PlayerControllerTag>()
            .WithChangeFilter<ccMove.Velocity>()
            .ForEach((Entity ent, in Translation position, in Grounded grounded, in Data.Move.CharacterControllerComponent ccComp) =>
            {
                float g = math.select(0, 1f, grounded.Value == false);
                //SetComponent(ent, new Data.Move.Gravity { Value = g });
                /*
                Unity.Physics.RaycastHit backwardHit = new Unity.Physics.RaycastHit();
                RaycastInput input = new RaycastInput()
                {
                    Start = position.Value,
                    End = position.Value - math.mul(ccComp.Gravity, (float3)0.1f),
                    //Layer filter
                    Filter = new CollisionFilter
                    {
                        BelongsTo = 1 << 1, //belongs to all layers
                        CollidesWith = 1 << 0, //collides with all layers
                        GroupIndex = 0,
                    }
                };
                SingleRayCast(collisionWorld, input, ref backwardHit);
                */
            })
            .WithBurst()
            .Schedule();

            if (GetComponent<Grounded>(player).Value == false)
            {
                Dependency = new GroundCollisionJob()
                {
                    ChunkTagJob = GetComponentDataFromEntity<KaizerWaldCode.Data.Tag.MapChunk>(true),
                    PlayerTagJob = GetComponentDataFromEntity<Data.Tags.PlayerBodyTag>(true),

                    GroundedJob = GetComponentDataFromEntity<Grounded>(false),
                }.Schedule(_stepPhysicsWorldSystem.Simulation, ref _buildPhysicsWorldSystem.PhysicsWorld, Dependency);
                Dependency.Complete();
            }

            //groundCollisionJobHandle.Complete();
            //JobHandle groundCollisionJobHandle = groundCollisionJob.Schedule(_stepPhysicsWorldSystem, ref _buildPhysicsWorldSystem, Dependency);
        }

        [BurstCompile]
        struct GroundCollisionJob : ICollisionEventsJob
        {
            //CAREFUL burstDiscard method not needed(and it won't work btw..)
            [ReadOnly] public ComponentDataFromEntity<KaizerWaldCode.Data.Tag.MapChunk> ChunkTagJob;
            [ReadOnly] public ComponentDataFromEntity<Data.Tags.PlayerBodyTag> PlayerTagJob;

            public ComponentDataFromEntity<Grounded> GroundedJob;

            public void Execute(CollisionEvent collisionEvent)
            {
                Entity entityA = collisionEvent.EntityA;
                Entity entityB = collisionEvent.EntityB;
                //Debug.Log($"entityA = {entityA.Index} entityB = {entityB.Index}");
                bool entAplayer = PlayerTagJob.HasComponent(entityA);
                bool entBplayer = PlayerTagJob.HasComponent(entityB);

                bool entAchunk = ChunkTagJob.HasComponent(entityA);
                bool entBchunk = ChunkTagJob.HasComponent(entityB);

                //Grounded grounded = new Grounded() {Value = 1};
                if (entAplayer && entBchunk)
                {
                    GroundedJob[entityA] = new Grounded() { Value = true };
                }

                if (entBplayer && entAchunk)
                {
                    GroundedJob[entityB] = new Grounded() { Value = true };
                }

            }
        }
    }
    
}