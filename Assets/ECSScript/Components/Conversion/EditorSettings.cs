using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEditor;

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

        public bool DebugVoronoi = false;
        private bool RealDebugVoronoi = false;

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
            if (DebugVoronoi) RealDebugVoronoi = true;
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

            if (!DebugVoronoi && RealDebugVoronoi == true) RealDebugVoronoi = false;
            if (DebugVoronoi && RealDebugVoronoi == false) RealDebugVoronoi = true;

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
            if (RealDebug && !RealDebugVoronoi)
            {
                float3[] points = _em.GetBuffer<Data.Chunks.Vertices>(_ChunksHolder).Reinterpret<float3>().AsNativeArray().ToArray();
                foreach (float3 point in points)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(point, 0.05f);
                }
            }

            if (RealDebugVoronoi)
            {
                float4[] pointsVoronoi = _em.GetBuffer<Data.Chunks.VoronoiGrid>(_ChunksHolder).Reinterpret<float4>().AsNativeArray().ToArray();

                //float3[] Poissons = _em.GetBuffer<Data.Chunks.PoissonDiscSample>(_ChunksHolder).Reinterpret<float3>().AsNativeArray().ToArray();
                //int mapSize = _em.GetComponentData<Data.MapData>(_mapSettings).MapSize;
                /*
                for (int i = 0; i < pointsVoronoi.Length; i++)
                {
                    
                    int x = (int)math.fmod(i, mapSize);
                    int y = ((int)math.fmod(i, (float)math.mul(mapSize, 1)) / mapSize); // need to test without floor
                    int z = (int)math.floor(i / (float)math.mul(mapSize, 1));
                    Debug.Log($"x: {x}; y: {y}; z: {z}");
                    
                }
                */
                
                foreach (float4 pointV in pointsVoronoi)
                {
                    Color32 color = new Color32((byte)math.min(255, pointV.w), (byte)math.min(255, pointV.w), (byte)math.min(255, pointV.w), 255);
                    Gizmos.color = color;
                    Gizmos.DrawWireSphere(pointV.xyz, 0.05f);
                }
                
            }

            if (RealDebugPoisson)
            {
                //float3[] Poissons = _em.GetBuffer<Data.Chunks.PoissonDiscSample>(_ChunksHolder).Reinterpret<float3>().AsNativeArray().ToArray();
                float4[] Poissons = _em.GetBuffer<Data.Chunks.IslandPoissonDisc>(_ChunksHolder).Reinterpret<float4>().AsNativeArray().ToArray();
                foreach (float4 poisson in Poissons)
                {
                    Gizmos.color = Color.blue;
                    if (poisson.w == 1)
                    {
                        Gizmos.color = Color.green;
                    }
                    else
                    {
                        Gizmos.color = Color.red;
                    }
                    Gizmos.DrawSphere(poisson.xyz, 0.1f);
                }

                //Handles.Label(new Vector3(0,0,0), "Text");
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
