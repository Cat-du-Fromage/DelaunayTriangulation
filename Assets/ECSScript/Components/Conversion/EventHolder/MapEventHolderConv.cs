using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace KaizerWaldCode.Data.Conversion
{
    [UpdateAfter(typeof(ChunksHolderConv))]
    [DisallowMultipleComponent]
    public class MapEventHolderConv : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            #region Tagging
            //Add Tag and remove unecessary default component
            dstManager.AddComponent<Tag.MapEventHolder>(entity);
            dstManager.RemoveComponent<LocalToWorld>(entity);
            dstManager.RemoveComponent<Translation>(entity);
            dstManager.RemoveComponent<Rotation>(entity);
            #endregion Tagging
            dstManager.AddComponent<Events.Event_InitGrid>(entity);
            dstManager.AddComponent<Events.Event_PoissonDisc>(entity);
        }
    }
}
