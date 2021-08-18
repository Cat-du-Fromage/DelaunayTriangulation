using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KaizerWaldCode.Data
{
    namespace Tag
    {
        public struct MapSettings : IComponentData { }
        public struct MapEventHolder : IComponentData { }
        public struct ChunksHolder : IComponentData { }
        public struct MapChunk : IComponentData { }
        public struct Delaunay : IComponentData { }
    }

    namespace Tag.Debug
    {
        public struct PrefabHolder : IComponentData { }
    }

    namespace Events
    {
        public struct Event_InitGrid : IComponentData { }
        public struct Event_RandomSamples : IComponentData { }

        public struct Event_PoissonDisc : IComponentData { }
        public struct Event_Voronoi : IComponentData { }
        public struct Event_Delaunay : IComponentData { }

        public struct Event_IslandCoast : IComponentData { }
        public struct Event_Noise : IComponentData { }

        public struct Event_CreateMapChunks : IComponentData { }
        public struct Event_ChunksSlice : IComponentData { }
        public struct Event_ChunksMeshData : IComponentData { }
        public struct Event_CreateMesh : IComponentData { }
    }
}
