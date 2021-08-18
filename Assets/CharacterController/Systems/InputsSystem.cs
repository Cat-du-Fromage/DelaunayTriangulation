using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace CharacterControllerECS.System
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class InputsSystem : SystemBase, PlayerController.IPlayerActions
    {
        private EntityQueryDesc _eventDescription;
        private EntityManager _em;
        private PlayerController _playerControl;

        private bool _playerDidMove = false;
        private bool _playerJump = false;
        private float3 _playerMoves; //y = W+S; x = A+D 

        protected override void OnCreate()
        {
            _eventDescription = new EntityQueryDesc()
            {
                All = new ComponentType[] { typeof(Data.Tags.PlayerControllerTag) },
            };
            RequireForUpdate(GetEntityQuery(_eventDescription));
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;

            _playerControl = new PlayerController();
            _playerControl.Player.SetCallbacks(this);
        }

        protected override void OnStartRunning() => _playerControl.Enable();
        protected override void OnStopRunning() => _playerControl.Disable();
        protected override void OnUpdate()
        {
            //Debug.Log($"test OnMove update {_verticalMove}");
            if (_playerDidMove)
            {
                //need to make a "update" variable or unity throw an error
                bool capturePlayerJump = _playerJump;
                float3 capturePlayerMoves = _playerMoves;
                Entities
                .WithName("PlayerInputSystem")
                .WithReadOnly(capturePlayerMoves)
                .WithAll<Data.Tags.PlayerControllerTag>()
                .ForEach((ref Data.Move.CharacterControllerComponent ccComp, ref Data.Move.Velocity ccVelocity, in Data.Move.Grounded grounded) =>
                {
                    //ccVelocity.Value.xz = math.mul(capturePlayerMoves.xz, (float2)ccComp.Speed);//TO DO add condition for sprint(when leftShift)
                    ccVelocity.Value.x = math.mul(capturePlayerMoves.x, ccComp.Speed);
                    ccVelocity.Value.z = math.mul(capturePlayerMoves.z, ccComp.Speed);
                    ccVelocity.Value.y = math.select(0f, ccComp.Gravity.y, !grounded.Value);
                    ccComp.Jump = math.select(0, 1, capturePlayerJump);
                    //Debug.Log($"test OnMove {ccVelocity.Value}");
                })
                .WithBurst()
                .Run();
                _playerDidMove = false;
            }

        }

        public void OnMouse(InputAction.CallbackContext context)
        {

        }

        void PlayerController.IPlayerActions.OnJump(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                _playerDidMove = true;
                _playerJump = true;
                //Debug.Log("test Jump" + context);
            }
        }

        void PlayerController.IPlayerActions.OnMove(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                _playerMoves.xz = context.ReadValue<Vector2>();
                _playerDidMove = true;
                //Debug.Log($"test OnMove {_playerMoves}");
            }
            else if (context.canceled)
            {
                _playerMoves = float3.zero;
                //Debug.Log($"test context {_playerMoves}");
                _playerDidMove = true;
            }
        }
    }
}