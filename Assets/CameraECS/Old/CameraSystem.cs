using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using System.Diagnostics;
using CamMove = CameraECS.Data.Move;
using CamInput = CameraECS.Data.Inputs;
/*
namespace CameraECS.CameraSystem
{
    [BurstCompile]
    [UpdateAfter(typeof(CameraInputSystem))]
    public class CameraMoveSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireSingletonForUpdate<Data.Tag.CameraHolderTag>();
        }
        protected override void OnUpdate()
        {
            bool didMove = !GetComponent<CamMove.Direction>(GetSingletonEntity<Data.Tag.CameraHolderTag>()).Value.Equals(float3.zero);
            //bool isRotating = Input.GetMouseButton(2);
            float deltaTime = Time.DeltaTime;

            if (didMove) //CARFUL WITH THIS THING
            {
                Entities
                    .WithBurst()
                    .WithAll<Data.Tag.CameraHolderTag>()
                    .ForEach((ref Translation position,
                              //ref Rotation rotation,
                              ref CamMove.Speed speed,
                              ref CamMove.SpeedZoom speedZoom,
                              //ref CamMove.MouseDragPosition mouseDragPos,
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
                             }).Run();
            }
        }
    }
    
    
    [BurstCompile]
    [UpdateAfter(typeof(CameraInputSystem))]
    public class CameraRotationSystem : SystemBase
    {
        EntityManager _em;
        protected override void OnCreate()
        {
            RequireSingletonForUpdate<Data.Tag.CameraHolderTag>();
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }
        protected override void OnUpdate()
        {
            float deltaTime = Time.DeltaTime;

            bool isRotating = Input.GetMouseButton(2);

            if (isRotating)
            {
                Entities
                    //.WithBurst()
                    .WithoutBurst()
                    .WithAll<Data.Tag.CameraHolderTag>()
                    .ForEach((ref Rotation rotation,
                              ref CamMove.MouseDragPosition mouseDragPos,
                              in DynamicBuffer<Child> camera,
                              in CamMove.Speed speed) =>
                             {
                                #region Rotation
                                float _rotationSpeed = math.mul(speed.Value*10, deltaTime);
                                float3 _distanceRadian = new float3(math.radians(mouseDragPos.DragLength.x), math.radians(mouseDragPos.DragLength.y), math.radians(mouseDragPos.DragLength.z));

                                float _distanceX = math.mul(_rotationSpeed, _distanceRadian.x);
                                float _distanceY = math.mul(_rotationSpeed, _distanceRadian.y);


                                //when dragging top Y Higher => math.cmax(math.normalize(mouseDragPos.DragLength))
                                if (!mouseDragPos.DragLength.Equals(float3.zero))
                                {
                                    float3 _absDragVectorNromalize = math.normalize(math.abs(mouseDragPos.DragLength));
                                    if (math.cmax(_absDragVectorNromalize) == _absDragVectorNromalize.x)
                                    {
                                        rotation.Value = math.mul(rotation.Value, quaternion.RotateY(_distanceX).value);
                                    }
                                    else
                                    {   //TO DO : find a way to get rid of entitymanager
                                        quaternion _cameraNewRotation = math.mul(_em.GetComponentData<Rotation>(camera[0].Value).Value, quaternion.RotateX(-_distanceY).value);
                                        _em.SetComponentData(camera[0].Value, new Rotation {Value = _cameraNewRotation });
                                    }

                                    mouseDragPos.Start = mouseDragPos.End;
                                }

                                 #endregion Rotation
                             }).Run();
            }
        }
    }
    
}
    */