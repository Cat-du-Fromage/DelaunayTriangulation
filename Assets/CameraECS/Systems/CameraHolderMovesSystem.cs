using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using CamMove = CameraECS.Data.Move;
using CamInput = CameraECS.Data.Inputs;

namespace CameraECS.CameraSystem
{
    [BurstCompile]
    [UpdateAfter(typeof(CameraInputSystem))]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class CameraHolderMovesSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireSingletonForUpdate<Data.Tag.CameraHolderTag>();
        }
        protected override void OnUpdate()
        {
            bool didMove = !GetComponent<CamMove.Direction>(GetSingletonEntity<Data.Tag.CameraHolderTag>()).Value.Equals(float3.zero);
            bool isRotating = Input.GetMouseButton(2);
            float deltaTime = Time.DeltaTime;

            if (didMove || isRotating)
            {
                Entities
                    .WithBurst()
                    .WithAll<Data.Tag.CameraHolderTag>()
                    .ForEach((ref Translation position,
                              ref Rotation rotation,
                              ref CamMove.MouseDragPosition mouseDragPos,
                              in CamMove.Speed speed,
                              in CamMove.SpeedZoom speedZoom,
                              in CamMove.Direction direction,
                              in CamInput.LeftShift leftShift) =>
                                {
                                #region X/Z translation
                                    //Shift Key multiplicator
                                    float _speedXZ = Input.GetKey(leftShift.LeftShiftKey) ? math.mul(speed.Value, 2) : speed.Value; //speed
                                    float _speedZoomY = Input.GetKey(leftShift.LeftShiftKey) ? math.mul(speedZoom.Value, 2) : speedZoom.Value; //speedZoom

                                    //Speed depending on Y Position (min : default speed Value)
                                    _speedXZ = math.max(_speedXZ, math.mul(position.Value.y, _speedXZ));
                                    _speedZoomY = math.max(_speedZoomY, math.mul(math.log(position.Value.y), _speedZoomY));

                                    //Dependency with delta time
                                    float _speedXZDeltaTime = math.mul(_speedXZ, deltaTime);
                                    float _speedZoomYDeltaTime = math.mul(_speedZoomY, deltaTime);

                                    //calculate new position (both XZ and Y)
                                    float3 _horizontalMove = new float3(math.mad(direction.Value.x, _speedXZDeltaTime, position.Value.x), 0, math.mad(direction.Value.z, _speedXZDeltaTime, position.Value.z));
                                    float3 _zoomMove = new float3(0, math.mad(-direction.Value.y, _speedZoomYDeltaTime, position.Value.y), 0);

                                    position.Value = _horizontalMove + _zoomMove;
                                #endregion X/Z translation

                                #region Rotation
                                    float _rotationSpeed = math.mul(math.mul(speed.Value, 10), deltaTime);

                                    float _dstRadiansX = math.radians(mouseDragPos.DragLength.x); //Ecs work with radians

                                    float _distanceX = math.mul(_rotationSpeed, _dstRadiansX);

                                    //when dragging top Y Higher => math.cmax(math.normalize(mouseDragPos.DragLength))
                                    if (!mouseDragPos.DragLength.Equals(float3.zero))
                                    {
                                        float3 _absDragVectorNromalize = math.normalize(math.abs(mouseDragPos.DragLength));
                                        if (math.cmax(_absDragVectorNromalize) == _absDragVectorNromalize.x)
                                        {
                                            rotation.Value = math.mul(rotation.Value, quaternion.RotateY(_distanceX).value);
                                        }
                                        mouseDragPos.Start = mouseDragPos.End;
                                    }
                                #endregion Rotation
                                }
                             ).Run();
            }
        }
    }
}
