using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace CharacterControllerECS.System
{
    [UpdateAfter(typeof(InputsSystem))]
    public class PlayerMovementSystem : SystemBase
    {
        private EntityQueryDesc _eventDescription;
        private EntityManager _em;
        protected override void OnCreate()
        {
            _eventDescription = new EntityQueryDesc()
            {
                All = new ComponentType[] { typeof(Data.Tags.PlayerControllerTag) },
            };
            RequireForUpdate(GetEntityQuery(_eventDescription));
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }
        protected override void OnUpdate()
        {
            //float deltaTime = Time.DeltaTime;
            Entities
            .WithName("PlayerMovementSystem")
            .WithAll<Data.Tags.PlayerControllerTag>()
            .ForEach((ref Translation translation, in Data.Move.Velocity ccVelocity, in Data.Move.CharacterControllerComponent ccComp) =>
            {
                if (math.any(ccVelocity.Value.xz))
                {
                    translation.Value = math.mad(ccVelocity.Value, 0.05f, translation.Value);
                }
            })
            .WithBurst()
            .Schedule();
        }
    }
}