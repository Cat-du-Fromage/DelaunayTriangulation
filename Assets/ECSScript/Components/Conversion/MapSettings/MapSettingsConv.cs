using System;
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

        public PoissonDiscData poissonDiscSample;

        public NoiseData Noise;


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
                PointPerMeter = math.max(2, this.PointPerMeter),

                //Try(NumChunks*this.ChunkSize - 1) * PointPerMeter - (NumChunks * this.ChunkSize - 2)
                
                PointSpacing = 1f / (PointPerMeter - 1f),
                //ChunkPointPerAxis = (this.ChunkSize - 1) * PointPerMeter - (this.ChunkSize - 2),
                ChunkPointPerAxis = (this.ChunkSize * PointPerMeter) - (this.ChunkSize - 1),
                MapPointPerAxis = (NumChunks * this.ChunkSize) * PointPerMeter - (NumChunks * this.ChunkSize - 1),
                //MapPointPerAxis = (NumChunks * this.ChunkSize - 1) * PointPerMeter - (NumChunks * this.ChunkSize - 2),
                
                /*
                PointSpacing = (float)this.ChunkSize / ((this.ChunkSize * PointPerMeter) - 1f),
                ChunkPointPerAxis = this.ChunkSize * PointPerMeter,
                MapPointPerAxis = math.max(this.ChunkSize * PointPerMeter, NumChunks * (this.ChunkSize * PointPerMeter) - (NumChunks-1)),
                */
            });
            UnityEngine.Debug.Log($"Old method = {math.max(this.ChunkSize * PointPerMeter, NumChunks * (this.ChunkSize * PointPerMeter) - (NumChunks - 1))}");
            UnityEngine.Debug.Log($"New method = {(NumChunks * this.ChunkSize) * PointPerMeter - (NumChunks * this.ChunkSize - 1)}");
            dstManager.AddComponentData(entity, new Data.NoiseData()
            {
                Seed = math.max(1, Noise.Seed),
                Octaves = math.max(1, Noise.Octaves),
                Scale = math.max(0.001f, Noise.Scale),
                Persistance = math.min(math.max(0, Noise.Persistance),1),
                Lacunarity = math.max(1f, Noise.Lacunarity),
                Offset = Noise.Offset,
                HeightMultiplier = math.max(1f,Noise.HeightMultiplier),
            });

            dstManager.AddComponentData(entity, new Data.PoissonDiscData()
            {
                Seed = math.max(1u, poissonDiscSample.Seed),
                Radius = math.max(1, poissonDiscSample.Radius),
                SampleBeforeReject = math.max(1, poissonDiscSample.SampleBeforeReject),
                //NumCellMap = (int)math.ceil( math.mul(this.ChunkSize, NumChunks) / (float)math.max(1, poissonDiscSample.Radius) ),
                NumCellMap = (int)math.ceil(this.ChunkSize/ (float)math.max(1, poissonDiscSample.Radius) * NumChunks),
                CellSize = math.max(1f, poissonDiscSample.Radius)/math.SQRT2,
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
    [Serializable]
    public struct PoissonDiscData
    {
        public uint Seed;
        public int Radius;
        public int SampleBeforeReject;
        //public uint CellMapAxis;
    }

    [Serializable]
    public struct NoiseData
    {
        public uint Seed;
        public int Octaves;
        public float Scale;
        public float Persistance;
        public float Lacunarity;
        public float2 Offset;
        public float HeightMultiplier;
    }
}