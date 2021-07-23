using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace KaizerWaldCode
{
    public class EditorSettings : MonoBehaviour
    {
        [SerializeField] private Data.Conversion.MapSettingsHolder settings;
        [SerializeField] private Data.Conversion.EventHolderEditor eventHolder;
        [SerializeField] private Data.Conversion.ChunksHolderEditor chunksHolder;
        private EntityManager _em;
        private Entity _mapSettings;
        private Entity _eventHolder;
        private Entity _earthSettings;
        private Entity _ChunksHolder;

        [Header("Map Data")]
        public int ChunkSize;
        public int NumChunks;
        [Range(2, 10)]
        public int PointPerMeter;

        [Space(5)]
        public bool AutoUpdate;
        public bool ShowBoundsGizmo;
        public bool DebugMode;
        private bool RealDebug = false;

        public bool DebugPoisson = false;
        private bool RealDebugPoisson = false;

        void Start()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _mapSettings = settings.GetMapSettings();
            _eventHolder = eventHolder.GetEventHolder();
            _ChunksHolder = chunksHolder.GetChunksHolder();

            ChunkSize = _em.GetComponentData<Data.MapData>(_mapSettings).ChunkSize;
            NumChunks = _em.GetComponentData<Data.MapData>(_mapSettings).NumChunk;
            PointPerMeter = _em.GetComponentData<Data.MapData>(_mapSettings).PointPerMeter;
            if (DebugMode) RealDebug = true;
            if (DebugPoisson) RealDebugPoisson = true;
        }

        public void ProcessChanges()
        {
            if (CheckValueChanged())
            {
                SetGeneralSettings();
                _em.AddComponent<Data.Events.Event_InitGrid>(_eventHolder);
            }

            if (!DebugMode && RealDebug == true) RealDebug = false;
            if (DebugMode && RealDebug == false) RealDebug = true;

            if (!DebugPoisson && RealDebugPoisson == true) RealDebug = false;
            if (DebugPoisson && RealDebugPoisson == false) RealDebug = true;

        }

        void SetGeneralSettings()
        {
            _em.SetComponentData(_mapSettings, new Data.MapData()
            {
                ChunkSize = math.max(1, this.ChunkSize),
                NumChunk = math.max(1, NumChunks),
                MapSize = math.mul(this.ChunkSize, NumChunks),
                PointPerMeter = math.max(1, this.PointPerMeter),
                PointSpacing = (float)this.ChunkSize / ((this.ChunkSize * PointPerMeter) - 1f),
                ChunkPointPerAxis = this.ChunkSize * PointPerMeter,
                MapPointPerAxis = math.max(this.ChunkSize * PointPerMeter, NumChunks * (this.ChunkSize * PointPerMeter) - 1),
            });
        }

        void OnDrawGizmos()
        {
            if (RealDebug)
            {
                float3[] points = _em.GetBuffer<Data.Chunks.Vertices>(_ChunksHolder).Reinterpret<float3>().AsNativeArray().ToArray();
                foreach (float3 point in points)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(point, 0.05f);
                }
            }

            if (RealDebugPoisson)
            {
                float3[] Poissons = _em.GetBuffer<Data.Chunks.PoissonDiscSample>(_ChunksHolder).Reinterpret<float3>().AsNativeArray().ToArray();
                foreach (float3 poisson in Poissons)
                {
                    if (poisson.x != -1)
                    {
                        Gizmos.color = Color.blue;
                        Gizmos.DrawSphere(poisson, 0.1f);
                    }
                }
            }
        }

        bool CheckValueChanged()
        {
            if (
                ChunkSize != _em.GetComponentData<Data.MapData>(_mapSettings).ChunkSize ||
                NumChunks != _em.GetComponentData<Data.MapData>(_mapSettings).NumChunk ||
                PointPerMeter != _em.GetComponentData<Data.MapData>(_mapSettings).PointPerMeter
                )
            {
                return true;
            }
                return false;
        }
        
    }
}
