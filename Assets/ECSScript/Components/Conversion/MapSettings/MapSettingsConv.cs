using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace KaizerWaldCode.Data.Conversion
{
    [DisallowMultipleComponent]
    public class MapSettingsConv : MonoBehaviour, IConvertGameObjectToEntity
    {
        [Header("Map Data")]
        public int ChunkSize;
        public int NumChunks;
        [Range(2,10)]
        public int PointPerMeter;


        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            #region Tagging
            //Add Tag and remove unecessary default component
            dstManager.AddComponent<Tag.MapSettings>(entity);
            dstManager.RemoveComponent<LocalToWorld>(entity);
            dstManager.RemoveComponent<Translation>(entity);
            dstManager.RemoveComponent<Rotation>(entity);
            #endregion Tagging

            dstManager.AddComponentData(entity, new Data.MapData()
            {
                ChunkSize = math.max(1, this.ChunkSize),
                NumChunk = math.max(1, NumChunks),
                MapSize = math.mul(this.ChunkSize, NumChunks),
                PointPerMeter = math.max(1, this.PointPerMeter),
                PointSpacing = (float)this.ChunkSize / ((this.ChunkSize*PointPerMeter) - 1f),
                ChunkPointPerAxis = this.ChunkSize * PointPerMeter,
                MapPointPerAxis = math.max(this.ChunkSize * PointPerMeter, NumChunks * (this.ChunkSize * PointPerMeter) - (NumChunks-1)),
            });
        }
    }

    public struct MapData
    {
        public int ChunkSize;
        public int NumChunk;
        public int MapSize;
        public int PointPerMeter;
        public float PointSpacing;
    }
}