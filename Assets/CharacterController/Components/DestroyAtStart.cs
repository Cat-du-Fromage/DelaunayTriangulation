using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace CharacterControllerECS
{
    [DisallowMultipleComponent]
    public class DestroyAtStart : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.DestroyEntity(entity);
        }
    }
}