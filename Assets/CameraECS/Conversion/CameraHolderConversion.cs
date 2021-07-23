using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace CameraECS.Data.Conversion
{
    [DisallowMultipleComponent]
    public class CameraHolderConversion : MonoBehaviour, IConvertGameObjectToEntity
    {
        public KeyCode Up;
        public KeyCode Down;
        public KeyCode Right;
        public KeyCode Left;

        public KeyCode LeftShift;

        public float Speed;
        public float ZoomSpeed;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            #region Default Values
            Up = Up == KeyCode.None? KeyCode.W : Up;
            Down = Down == KeyCode.None ? KeyCode.S : Down;
            Right = Right == KeyCode.None ? KeyCode.D : Right;
            Left = Left == KeyCode.None ? KeyCode.A : Left;

            LeftShift = LeftShift == KeyCode.None ? KeyCode.LeftShift : LeftShift;

            Speed = Speed == 0 ? 3 : Speed;
            ZoomSpeed = ZoomSpeed == 0 ? 100 : ZoomSpeed;
            #endregion Default Values

            dstManager.AddComponent<Tag.CameraHolderTag>(entity);
            dstManager.AddComponentData(entity, new Inputs.Up { UpKey = Up });
            dstManager.AddComponentData(entity, new Inputs.Down { DownKey = Down });
            dstManager.AddComponentData(entity, new Inputs.Right { RightKey = Right });
            dstManager.AddComponentData(entity, new Inputs.Left { LeftKey = Left });

            dstManager.AddComponentData(entity, new Inputs.LeftShift { LeftShiftKey = LeftShift });
            dstManager.AddComponentData(entity, new Inputs.MouseMiddle { MiddleMouseKey = 2 });

            dstManager.AddComponentData(entity, new Move.Direction { Value = new float3(0, 0, 0) });
            dstManager.AddComponentData(entity, new Move.Speed { Value = Speed });
            dstManager.AddComponentData(entity, new Move.SpeedZoom { Value = ZoomSpeed });
            dstManager.AddComponent<Move.MouseDragPosition>(entity);
        }
    }
}
