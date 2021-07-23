using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Animation;
using UnityEngine;
using AnimationCurve = Unity.Animation.AnimationCurve;


namespace KaizerWaldCode.Data
{

    public struct MapData : IComponentData
    {
        public int ChunkSize;
        public int NumChunk;
        public int PointPerMeter;


        public int MapSize;
        public float PointSpacing;
        public int ChunkPointPerAxis;
        public int MapPointPerAxis;
    }

    public struct PoissonDiscData : IComponentData
    {
        public int Seed;
        public float Radius;
        public int SampleBeforeReject;
    }

    public struct NoiseData : IComponentData
    {
        public uint Seed;
        public int Octaves;
        public float Scale;
        public float Persistance;
        public float Lacunarity;
        public float2 Offset;
        public float HeightMultiplier;
    }

    public struct LevelOfDetail : IComponentData
    {
        public int Value;
    }

    public struct HeightCurve : IComponentData
    {
        public AnimationCurve Value;
    }

    namespace Chunks
    {
        public struct Vertices : IBufferElementData
        {
            public float3 Value;
        }

        public struct Uvs : IBufferElementData
        {
            public float2 Value;
        }

        public struct Triangles : IBufferElementData
        {
            public int Value;
        }
        public struct PoissonDiscSample : IBufferElementData
        {
            public float3 Value;
        }

        public struct PoissonDiscGrid : IBufferElementData
        {
            public float2 Value;
        }

        public struct HeightMap : IBufferElementData
        {
            public float Value;
        }

        public struct ColorMap : IBufferElementData
        {
            public MaterialColor Value;
        }

        public struct Regions : IBufferElementData
        {
            public float Height;
            public MaterialColor Color;
        }
    }
}