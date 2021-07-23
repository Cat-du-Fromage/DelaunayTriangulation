using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace KaizerWaldCode.Data.Authoring
{
    [GenerateAuthoringComponent]
    public class Authoring_ChunksMaterial : IComponentData
    {
        public Material mat;
    }
}