using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace CharacterControllerECS.Camera
{
    [DisallowMultipleComponent]
    public class TPSCameraConv : MonoBehaviour, IConvertGameObjectToEntity
    {
        public float distanceFromPlayer = 2f;
        public GameObject Target;
        public GameObject LookTo;
        [Range(0, 1)] public float LookToInterpolateFactor = 0.9f;

        public GameObject LookFrom;
        [Range(0, 1)] public float LookFromInterpolateFactor = 0.9f;
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponent<TPSCameraTag>(entity);
            dstManager.AddComponentData(entity, new CameraSmoothTrackSettings
            {
                DistanceFromPlayer = distanceFromPlayer,
                Target = conversionSystem.GetPrimaryEntity(Target),
                LookTo = conversionSystem.GetPrimaryEntity(LookTo),
                LookToInteroplateFactor = LookToInterpolateFactor,
                LookFrom = conversionSystem.GetPrimaryEntity(LookFrom),
                LookFromInterpolateFactor = LookFromInterpolateFactor
            });
        }
    }

    public struct TPSCameraTag : IComponentData { }

    public struct CameraSmoothTrackSettings : IComponentData
    {
        public float DistanceFromPlayer;
        public Entity Target;
        public Entity LookTo;
        public float LookToInteroplateFactor;
        public Entity LookFrom;
        public float LookFromInterpolateFactor;
        public float3 OldPositionTo;
    }
}