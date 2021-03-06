using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace KaizerWaldCode.Data.Conversion
{
    [DisallowMultipleComponent]
    public class DebugPrefabHolder : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            #region Tagging

            //Add Tag and remove unecessary default component
            dstManager.AddComponent<Tag.Debug.PrefabHolder>(entity);
            dstManager.RemoveComponent<LocalToWorld>(entity);
            dstManager.RemoveComponent<Translation>(entity);
            dstManager.RemoveComponent<Rotation>(entity);

            #endregion Tagging
        }
    }
}