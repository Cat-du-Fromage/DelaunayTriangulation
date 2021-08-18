using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Physics;
using static Unity.Physics.PhysicsStep;

namespace CharacterControllerECS.Conversion
{
    [DisallowMultipleComponent]
    public class CharacterControllerConversion : MonoBehaviour, IConvertGameObjectToEntity
    {
        public float3 Gravity = Default.Gravity;
        public float MaxSpeed = 7.5f;
        public float Speed = 5.0f;
        public float JumpStrength = 9.0f;
        public float MaxStep = 0.35f;
        public float Drag = 0.2f;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            //CombineMeshes(this.gameObject.transform.GetChild(0).gameObject);

            dstManager.AddComponent<Data.Tags.PlayerControllerTag>(entity);
            dstManager.AddComponentData(entity, new Data.Move.CharacterControllerComponent()
            {
                Gravity = Gravity,
                MaxSpeed = MaxSpeed,
                Speed = this.Speed,
                JumpStrength = JumpStrength,
                MaxStep = MaxStep,
                Drag = Drag
            });

            dstManager.AddComponentData(entity, new Data.Move.Grounded() { Value = false });
            dstManager.AddComponentData(entity, new Data.Move.Velocity() { Value = 0 });
            /*
            dstManager.AddComponentData(entity, new Data.Move.Speed() {Value = Speed});
            dstManager.AddComponentData(entity, new Data.Move.Gravity() { Value = 0 });
            
            ComponentTypes Datas = new ComponentTypes
            (
                typeof(Data.Inputs.InputHV),
                typeof(Data.Move.Direction)
            );
            dstManager.AddComponents(entity, Datas);
            */
        }

        /*
        Mesh CombineMeshes(GameObject obj)
        {
            Vector3 position = obj.transform.position;
            obj.transform.position = Vector3.zero;

            MeshFilter[] meshFilters = obj.GetComponentsInChildren<MeshFilter>();
            CombineInstance[] combine = new CombineInstance[meshFilters.Length];
            for (int i = 0; i < meshFilters.Length; i++)
            {
                combine[i].mesh = meshFilters[i].sharedMesh;
                combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
                meshFilters[i].gameObject.SetActive(false);
            }

            obj.transform.GetComponent<MeshFilter>().mesh = new Mesh();
            obj.transform.GetComponent<MeshFilter>().mesh.CombineMeshes(combine, true, true);
            obj.transform.gameObject.SetActive(true);
            obj.transform.position = position;

            return obj.transform.GetComponent<MeshFilter>().mesh;
        }
        */

    }
}