using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace CharacterControllerECS.Camera
{
    public class CineMachineFollowPlayer : MonoBehaviour
    {
        public Entity entityToFollow;

        private EntityManager _em;

        void Awake()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        // Update is called once per frame
        void LateUpdate()
        {
            if (entityToFollow == Entity.Null) return;
            Translation entPos = _em.GetComponentData<Translation>(entityToFollow);

            transform.position = entPos.Value;
        }
    }
}
