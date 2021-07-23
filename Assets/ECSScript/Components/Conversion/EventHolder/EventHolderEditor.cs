using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace KaizerWaldCode.Data.Conversion
{
    [UpdateAfter(typeof(MapSettingsConv))]
    [DisallowMultipleComponent]
    public class EventHolderEditor : MonoBehaviour, IConvertGameObjectToEntity
    {
        private Entity _eventHolder;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            _eventHolder = entity;
        }

        public Entity GetEventHolder()
        {
            return _eventHolder;
        }
    }
}
