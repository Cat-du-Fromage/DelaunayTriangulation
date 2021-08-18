using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace CharacterControllerECS.Camera
{
    [DisallowMultipleComponent]
    public class LeaderAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public GameObject follower;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            CineMachineFollowPlayer followEntity = follower.GetComponent<CineMachineFollowPlayer>();

            if (followEntity == null) followEntity = follower.AddComponent<CineMachineFollowPlayer>();

            followEntity.entityToFollow = entity;
        }
    }
}