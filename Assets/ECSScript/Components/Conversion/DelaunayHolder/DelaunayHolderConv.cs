using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace KaizerWaldCode.Data.Conversion
{
    [DisallowMultipleComponent]
    public class DelaunayHolderConv : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            #region Tagging
            //Add Tag and remove unecessary default component
            dstManager.AddComponent<Tag.Delaunay>(entity);
            dstManager.RemoveComponent<LocalToWorld>(entity);
            dstManager.RemoveComponent<Translation>(entity);
            dstManager.RemoveComponent<Rotation>(entity);
            #endregion Tagging

            dstManager.AddComponent<Delaunay.Data_Delaunay>(entity);

            ComponentTypes triBuffer = new ComponentTypes
            (
                typeof(Delaunay.Coords),
                typeof(Delaunay.DelaunayTriangles),
                typeof(Delaunay.HalfEdges),
                typeof(Delaunay.Points)
            );

            ComponentTypes hullsBuffer = new ComponentTypes
            (
                typeof(Delaunay.Hull),
                typeof(Delaunay.HullHash),
                typeof(Delaunay.HullNext),
                typeof(Delaunay.HullPrev),
                typeof(Delaunay.HullTri)
            );

            dstManager.AddComponents(entity, triBuffer);
            dstManager.AddComponents(entity, hullsBuffer);
        }
    }
}