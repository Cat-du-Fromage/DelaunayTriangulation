using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using CamMove = CameraECS.Data.Move;
namespace CameraECS.CameraSystem
{
    /// <summary>
    /// Rotate Camera on X axis
    /// By seperating rotation Y from cameraHolder we prevent rotation axis XY to cross and thus messing with order
    /// </summary>
    [BurstCompile]
    [UpdateAfter(typeof(CameraInputSystem))]
    [UpdateBefore(typeof(CameraHolderMovesSystem))]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class CameraRotationSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireSingletonForUpdate<Data.Tag.CameraTag>();
        }
        protected override void OnUpdate()
        {
            bool isRotating = Input.GetMouseButton(2);
            float deltaTime = Time.DeltaTime;

            if (isRotating)
            {
                Entities
                    .WithBurst()
                    .WithAll<Data.Tag.CameraTag>()
                    .ForEach((ref Rotation rotation,
                              ref Parent camHolder) =>
                                {
                                    CamMove.Speed _speed = GetComponent<CamMove.Speed>(camHolder.Value);
                                    CamMove.MouseDragPosition _mouseDragPos = GetComponent<CamMove.MouseDragPosition>(camHolder.Value);

                                    float _rotationSpeed = math.mul(math.mul(_speed.Value, 10), deltaTime);
                                    float _dstRadiansY = math.radians(_mouseDragPos.DragLength.y);

                                    float _distanceY = math.mul(_rotationSpeed, _dstRadiansY);

                                    //when dragging top Y Higher => math.cmax(math.normalize(mouseDragPos.DragLength))
                                    if (!_mouseDragPos.DragLength.Equals(float3.zero))
                                    {
                                        float3 _absDragVectorNromalize = math.normalize(math.abs(_mouseDragPos.DragLength));
                                        if (math.cmax(_absDragVectorNromalize) == _absDragVectorNromalize.y)
                                        {
                                            rotation.Value = math.mul(rotation.Value, quaternion.RotateX(-_distanceY).value);
                                            //mouseDragPos.Start = mouseDragPos.End;
                                        }
                                    }
                                }
                             ).Run();
            }
        }
    }
}