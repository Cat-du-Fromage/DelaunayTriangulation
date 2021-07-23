using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace KaizerWaldCode.Data.Conversion
{
    [UpdateAfter(typeof(MapSettingsConv))]
    [DisallowMultipleComponent]
    public class ChunksHolderEditor : MonoBehaviour, IConvertGameObjectToEntity
    {
        private Entity _chunksHolder;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            _chunksHolder = entity;
        }

        public Entity GetChunksHolder()
        {
            return _chunksHolder;
        }
    }
}
