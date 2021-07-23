using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KaizerWaldCode.Data.Authoring
{
    [GenerateAuthoringComponent]
    public struct Auth_ChunkPrefab : IComponentData
    {
        public Entity prefab;
    }
}