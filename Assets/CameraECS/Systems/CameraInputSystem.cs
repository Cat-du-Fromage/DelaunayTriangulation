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
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class CameraInputSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireSingletonForUpdate<Data.Tag.CameraHolderTag>();
        }
        protected override void OnUpdate()
        {

            Entities
            .WithBurst()
            .WithAll<Data.Tag.CameraHolderTag>()
            .ForEach(
            (ref CamMove.MouseDragPosition mouseDragPos,
            ref CamMove.Direction direction,
            in LocalToWorld ltw,
            in CamInput.Up up,
            in CamInput.Down down,
            in CamInput.Right right,
            in CamInput.Left left) =>
            {
                //math.forward is always (1,0,0) so when camera is rotatint => input controller does not correspond to right/left.. anymore
                //by making "float3(ltw.Forward.x, 0, ltw.Forward.z)" we retrieve actual position/rotation of the camera and move accordingly to it's current "state"(rotation)
                //the issue was : whe the object was rotating around X axis
                float3 _moveZPositiv = new float3(ltw.Forward.x, 0, ltw.Forward.z);
                float3 _x = math.select(float3.zero, ltw.Right, Input.GetKey(right.RightKey)) + math.select(float3.zero, -ltw.Right, Input.GetKey(left.LeftKey));
                float3 _z = math.select(float3.zero, _moveZPositiv, Input.GetKey(up.UpKey)) + math.select(float3.zero, -_moveZPositiv, Input.GetKey(down.DownKey));
                // Y axe is a bit special
                float3 _y = float3.zero;
                if (!Input.mouseScrollDelta.Equals(float2.zero)) { _y = math.select(math.down(), math.up(), Input.mouseScrollDelta.y > 0); }

                direction.Value = _x + _y + _z;

                //Rotation Input

                if (Input.GetMouseButtonDown(2))
                {
                    mouseDragPos.Start = Input.mousePosition;
                }
                mouseDragPos.End = math.select(mouseDragPos.Start, (float3)Input.mousePosition, Input.GetMouseButton(2));
                mouseDragPos.DragLength = mouseDragPos.End - mouseDragPos.Start;
            }).Run();
        }
    }
}
