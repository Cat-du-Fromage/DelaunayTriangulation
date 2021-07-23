using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace CameraECS.Data.Conversion
{
    [DisallowMultipleComponent]
    public class CameraConversion : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponent<Tag.CameraTag>(entity);
        }
    }
}

