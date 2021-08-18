using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace CharacterControllerECS.Conversion
{
    [DisallowMultipleComponent]
    public class PlayerBodyConv : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponent<Data.Tags.PlayerBodyTag>(entity);
        }
    }
}