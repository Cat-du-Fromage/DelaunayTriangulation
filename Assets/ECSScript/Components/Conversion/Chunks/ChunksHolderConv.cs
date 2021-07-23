using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace KaizerWaldCode.Data.Conversion
{
    [UpdateAfter(typeof(MapSettingsConv))]
    [DisallowMultipleComponent]
    public class ChunksHolderConv : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            #region Tagging
            //Add Tag and remove unecessary default component
            dstManager.AddComponent<Tag.ChunksHolder>(entity);
            dstManager.RemoveComponent<LocalToWorld>(entity);
            dstManager.RemoveComponent<Translation>(entity);
            dstManager.RemoveComponent<Rotation>(entity);
            #endregion Tagging

            ComponentTypes chunkHolderComponents = new ComponentTypes
            (
                typeof(LinkedEntityGroup),
                typeof(Chunks.Vertices),
                typeof(Chunks.PoissonDiscSample),
                typeof(Chunks.PoissonDiscGrid)
            );

            dstManager.AddComponents(entity, chunkHolderComponents);
        }
    }
}