using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

namespace CharacterControllerECS.System
{
    public class GravitySystem : SystemBase
    {
        private EntityQueryDesc _eventDescription;
        private EntityManager _em;

        protected override void OnCreate()
        {
            _eventDescription = new EntityQueryDesc()
            {
                All = new ComponentType[] {typeof(Data.Tags.PlayerControllerTag)},
            };
            RequireForUpdate(GetEntityQuery(_eventDescription));
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        protected override void OnUpdate()
        {
            float deltaTime = Time.DeltaTime;

            Entities
            .WithName("PlayerControllerGravity")
            .WithAll<Data.Tags.PlayerControllerTag>()
            .ForEach((Entity ent, int entityInQueryIndex, ref Translation position, in Data.Move.Grounded grounded, in Data.Move.Velocity ccVelocity) =>
            {
                if (!grounded.Value)
                {
                    position.Value.y += math.mul(deltaTime, ccVelocity.Value.y);
                    //Debug.Log($"test Gravity {math.mul(deltaTime, ccVelocity.Value.y)}");
                }
                float g = math.select(0, 1f, grounded.Value == false);
                //SetComponent(ent, new Data.Move.Gravity { Value = g });
            })
            .WithBurst()
            .Run();

            //float3 epsilon = math.mul(new float3(0.0f, math.EPSILON, 0.0f), -math.normalize(ccComp.Gravity));
        }

    }
}