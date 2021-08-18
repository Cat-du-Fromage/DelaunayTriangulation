using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KaizerWaldCode.Data.Debug.Authoring
{
    [GenerateAuthoringComponent]
    public struct Auth_VoronoiPrefab : IComponentData
    {
        public Entity prefab;
    }
}