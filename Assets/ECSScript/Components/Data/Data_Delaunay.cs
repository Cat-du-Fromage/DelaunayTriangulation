using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KaizerWaldCode.Data.Delaunay
{
    public struct Data_Delaunay : IComponentData
    {
        public int HashSize;
        public float2 Center;
        public int TrianglesLen;
        public int HullStart;
        public int HullSize;
    }

    public struct DelaunayTriangles : IBufferElementData
    {
        public int Value;
    }

    public struct HalfEdges : IBufferElementData
    {
        public int Value;
    }
    public struct Points : IBufferElementData
    {
        public float Value;
    }
    public struct HullPrev : IBufferElementData
    {
        public int Value;
    }

    public struct HullNext : IBufferElementData
    {
        public int Value;
    }
    public struct HullTri : IBufferElementData
    {
        public int Value;
    }
    public struct HullHash : IBufferElementData
    {
        public int Value;
    }
    public struct Coords : IBufferElementData
    {
        public float Value;
    }
    public struct Hull : IBufferElementData
    {
        public int Value;
    }
}