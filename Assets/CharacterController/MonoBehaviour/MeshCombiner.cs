using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CharacterControllerECS.Mono
{
    public class MeshCombiner : MonoBehaviour
    {
        void Awake()
        {
            AdvancedMerge();
        }
        // Start is called before the first frame update
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        void AdvancedMerge()
        {
            // All our children (and us)
            MeshFilter[] filters = GetComponentsInChildren<MeshFilter>(false);

            // All the meshes in our children (just a big list)
            List<Material> materials = new List<Material>();
            MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(false); // <-- you can optimize this
            foreach (MeshRenderer renderer in renderers)
            {
                if (renderer.transform == transform) continue;
                Material[] localMats = renderer.sharedMaterials;
                foreach (Material localMat in localMats)
                {
                    if (!materials.Contains(localMat)) materials.Add(localMat);
                }
            }

            // Each material will have a mesh for it.
            List<Mesh> submeshes = new List<Mesh>();
            foreach (Material material in materials)
            {
                // Make a combiner for each (sub)mesh that is mapped to the right material.
                List<CombineInstance> combiners = new List<CombineInstance>();
                foreach (MeshFilter filter in filters)
                {
                    if (filter.transform == transform) continue;
                    // The filter doesn't know what materials are involved, get the renderer.
                    MeshRenderer renderer = filter.GetComponent<MeshRenderer>();  // <-- (Easy optimization is possible here, give it a try!)
                    if (renderer == null) continue;

                    // Let's see if their materials are the one we want right now.
                    Material[] localMaterials = renderer.sharedMaterials;
                    for (int materialIndex = 0; materialIndex < localMaterials.Length; materialIndex++)
                    {
                        if (localMaterials[materialIndex] != material)
                            continue;
                        // This submesh is the material we're looking for right now.
                        CombineInstance ci = new CombineInstance();
                        ci.mesh = filter.sharedMesh;
                        ci.subMeshIndex = materialIndex;
                        ci.transform = Matrix4x4.identity;
                        //ci.transform = transform.GetComponentInParent<Transform>().localToWorldMatrix;
                        combiners.Add(ci);
                    }
                }
                // Flatten into a single mesh.
                Mesh mesh = new Mesh();
                mesh.CombineMeshes(combiners.ToArray(), true);
                submeshes.Add(mesh);
            }

            // The final mesh: combine all the material-specific meshes as independent submeshes.
            //List<CombineInstance> finalCombiners = new List<CombineInstance>();
            /*
            foreach (Mesh mesh in submeshes)
            {
                CombineInstance ci = new CombineInstance();
                ci.mesh = mesh;
                ci.subMeshIndex = 0;
                ci.transform = Matrix4x4.identity;
                Debug.Log(ci.transform);
                finalCombiners.Add(ci);
            }
            */
            MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
            CombineInstance[] finalCombiners = new CombineInstance[meshFilters.Length];
            for (int i = 0; i < meshFilters.Length; i++)
            {
                finalCombiners[i].mesh = meshFilters[i].sharedMesh;
                finalCombiners[i].transform = meshFilters[i].transform.localToWorldMatrix;
            }
            Mesh finalMesh = new Mesh();
            finalMesh.CombineMeshes(finalCombiners, true, true);
            GetComponent<MeshFilter>().sharedMesh = finalMesh;
            Debug.Log("Final mesh has " + submeshes.Count + " materials.");
        }
    }
}
