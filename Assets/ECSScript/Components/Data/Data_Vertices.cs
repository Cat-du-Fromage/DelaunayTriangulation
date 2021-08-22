using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KaizerWaldCode.Data
{
    namespace Vertices
    {
        public struct VertexPosition : IBufferElementData
        {
            public float3 Value;
        }
        public struct VertexCellIndex : IBufferElementData
        {
            public int Value;
        }
    }

    namespace PoissonDiscSamples
    {
        public struct PoissonDiscPosition : IBufferElementData
        {
            public float2 Value;
        }
        public struct PoissonDiscCellIndex : IBufferElementData
        {
            public int Value;
        }
    }
}