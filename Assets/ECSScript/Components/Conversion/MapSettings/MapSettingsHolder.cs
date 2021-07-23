using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace KaizerWaldCode.Data.Conversion
{
    [DisallowMultipleComponent]
    public class MapSettingsHolder : MonoBehaviour, IConvertGameObjectToEntity
    {
        private Entity _mapSettingsHolder;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            _mapSettingsHolder = entity;
        }

        public Entity GetMapSettings()
        {
            return _mapSettingsHolder;
        }
    }
}
