using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.InputSystem;
using UnityEngine;
/*
namespace CharacterControllerECS.Camera
{
    
    [UpdateAfter(typeof(TransformSystemGroup))]
    public class TPSCameraSystem : SystemBase, PlayerController.ICameraActions
    {
        private EntityQueryDesc _eventDescription;
        private EntityManager _em;
        private PlayerController _playerControl;

        private float2 _rotationCamera = float2.zero;
        public void OnMouseDelta(InputAction.CallbackContext context)
        {
            _rotationCamera = context.ReadValue<Vector2>();
            //Debug.Log($"Test camera callback {_rotationCamera}");
        }

        protected override void OnCreate()
        {
            _eventDescription = new EntityQueryDesc()
            {
                All = new ComponentType[] { typeof(TPSCameraTag) },
            };
            RequireForUpdate(GetEntityQuery(_eventDescription));
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;

            _playerControl = new PlayerController();
            _playerControl.Camera.SetCallbacks(this);
        }

        protected override void OnStartRunning() => _playerControl.Enable();
        protected override void OnStopRunning() => _playerControl.Disable();

        protected override void OnUpdate()
        {
            float2 camRotationCapture = _rotationCamera;
            float time = Time.DeltaTime;
            Entities
            .WithName("SmoothlyTrackCameraTargetsJob")
            .WithAll<TPSCameraTag>()
            .ForEach((ref CameraSmoothTrackSettings cameraSmoothTrack, ref Translation pos , ref Rotation rotation) =>
            {
                float3 playerPos = GetComponent<LocalToWorld>(cameraSmoothTrack.LookTo).Position;
                float3 playerOffset = new float3(0, 0.7f, -1);
                pos.Value = GetComponent<LocalToWorld>(cameraSmoothTrack.LookTo).Position + playerOffset * 3f;
                float2 currentRotation = camRotationCapture * 0.001f;
                rotation.Value.value.x = -currentRotation.y + rotation.Value.value.x;
                rotation.Value.value.y = currentRotation.x + rotation.Value.value.y;
            })
            .WithBurst()
            .Schedule();
        }

        /// <summary>
        /// Converts a quaternion to euler.
        /// </summary>
        /// <param name="quaternion">The quaternion.</param>
        /// <returns>Euler angles.</returns>
        public static float3 ToEuler(quaternion quaternion)
        {
            var q = quaternion.value;

            var sinRCosP = 2 * ((q.w * q.x) + (q.y * q.z));
            var cosRCosP = 1 - (2 * ((q.x * q.x) + (q.y * q.y)));
            var roll = math.atan2(sinRCosP, cosRCosP);

            // pitch (y-axis rotation)
            var sinP = 2 * ((q.w * q.y) - (q.z * q.x));
            var pitch = math.abs(sinP) >= 1 ? math.sign(sinP) * math.PI / 2 : math.asin(sinP);

            // yaw (z-axis rotation)
            var sinYCosP = 2 * ((q.w * q.z) + (q.x * q.y));
            var cosYCosP = 1 - (2 * ((q.y * q.y) + (q.z * q.z)));
            var yaw = math.atan2(sinYCosP, cosYCosP);

            return new float3(roll, pitch, yaw);
        }

        /// <summary>
        /// Converts quaternion representation to euler
        /// </summary>
        public static float3 ToEuler2(quaternion quaternion)
        {
            float4 q = quaternion.value;
            double3 res;

            double sinr_cosp = +2.0 * (q.w * q.x + q.y * q.z);
            double cosr_cosp = +1.0 - 2.0 * (q.x * q.x + q.y * q.y);
            res.x = math.atan2(sinr_cosp, cosr_cosp);

            double sinp = +2.0 * (q.w * q.y - q.z * q.x);
            if (math.abs(sinp) >= 1)
            {
                res.y = math.PI / 2 * math.sign(sinp);
            }
            else
            {
                res.y = math.asin(sinp);
            }

            double siny_cosp = +2.0 * (q.w * q.z + q.x * q.y);
            double cosy_cosp = +1.0 - 2.0 * (q.y * q.y + q.z * q.z);
            res.z = math.atan2(siny_cosp, cosy_cosp);

            return (float3)res;
        }

        public static float3 RotateAroundPoint(float3 position, float3 pivot, float3 axis, float delta) => math.mul(quaternion.AxisAngle(axis, delta), position - pivot) + pivot;

    }
}
    */