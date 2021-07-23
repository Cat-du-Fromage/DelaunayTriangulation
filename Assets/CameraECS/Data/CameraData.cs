using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace CameraECS.Data.Tag
{
    public readonly struct CameraHolderTag : IComponentData { }
    public readonly struct CameraTag : IComponentData { }
}

namespace CameraECS.Data.Inputs
{
    public struct Up : IComponentData
    {
        public KeyCode UpKey;
    }
    public struct Down : IComponentData
    {
        public KeyCode DownKey;
    }
    public struct Right : IComponentData
    {
        public KeyCode RightKey;
    }
    public struct Left : IComponentData
    {
        public KeyCode LeftKey;
    }
    public struct LeftShift : IComponentData
    {
        public KeyCode LeftShiftKey;
    }
    public struct MouseMiddle : IComponentData
    {
        public int MiddleMouseKey;
    }
}

namespace CameraECS.Data.Move
{
    public struct Direction : IComponentData
    {
        public float3 Value;
    }
    public struct Speed : IComponentData
    {
        public float Value;
    }
    public struct SpeedZoom : IComponentData
    {
        public float Value;
    }
    public struct MouseDragPosition : IComponentData
    {
        public float3 Start;
        public float3 End;
        public float3 DragLength;
    }
}
