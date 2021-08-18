using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace CharacterControllerECS.Data
{
    namespace Tags
    {
       public struct PlayerControllerTag : IComponentData{}
       public struct PlayerBodyTag : IComponentData { }
    }

    namespace Inputs
    {
        public struct InputHV : IComponentData
        {
            public float InputH;
            public float InputV;
        }
    }

    namespace Move
    {
        public struct Direction : IComponentData
        {
            public float3 Value;
        }
        public struct Speed : IComponentData
        {
            public float Value;
        }

        public struct Gravity : IComponentData
        {
            public float Value;
        }

        public struct Velocity : IComponentData
        {
            public float3 Value;
        }

        public struct Grounded : IComponentData
        {
            public bool Value;
        }

        public struct CharacterControllerComponent : IComponentData
        {
            // -------------------------------------------------------------------------------------
            // Current Movement
            // -------------------------------------------------------------------------------------

            /// <summary>
            /// The current direction that the character is moving.
            /// </summary>
            public float3 CurrentDirection { get; set; }

            /// <summary>
            /// The current magnitude of the character movement.
            /// If <c>0.0</c>, then the character is not being directly moved by the controller but residual forces may still be active.
            /// </summary>
            public float CurrentMagnitude { get; set; }

            /// <summary>
            /// Is the character requesting to jump?
            /// Used in conjunction with <see cref="IsGrounded"/> to determine if the <see cref="JumpStrength"/> should be used to make the entity jump.
            /// </summary>
            public int Jump;

            // -------------------------------------------------------------------------------------
            // Control Properties
            // -------------------------------------------------------------------------------------

            /// <summary>
            /// Gravity force applied to the character.
            /// </summary>
            public float3 Gravity;

            /// <summary>
            /// The maximum speed at which this character moves.
            /// </summary>
            public float MaxSpeed;

            /// <summary>
            /// The current speed at which the player moves.
            /// </summary>
            public float Speed;

            /// <summary>
            /// The jump strength which controls how high a jump is, in conjunction with <see cref="Gravity"/>.
            /// </summary>
            public float JumpStrength { get; set; }

            /// <summary>
            /// The maximum height the character can step up, in world units.
            /// </summary>
            public float MaxStep { get; set; }

            /// <summary>
            /// Drag value applied to reduce the <see cref="VerticalVelocity"/>.
            /// </summary>
            public float Drag { get; set; }

            // -------------------------------------------------------------------------------------
            // Control State
            // -------------------------------------------------------------------------------------
            /// <summary>
            /// The current horizontal velocity of the character.
            /// </summary>
            //public float3 HorizontalVelocity { get; set; }

            /// <summary>
            /// The current jump velocity of the character.
            /// </summary>
            //public float3 VerticalVelocity { get; set; }

            //public float3 Velocity;
        }
    }
}

