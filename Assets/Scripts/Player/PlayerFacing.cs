using Babodayo.Input;
using UnityEngine;
namespace Babodayo.Player
{
    public sealed class PlayerFacing : MonoBehaviour
    {
        [Tooltip("Explicit gameplay camera for cursor projection.")] public Camera ViewCamera;
        [Tooltip("Optional visual only; never mirror the physics root.")] public SpriteRenderer Visual;
        [Tooltip("Optional marker showing the facing side.")] public Transform DirectionMarker;
        public int Resolve(InputFrame input, Vector3 position, int previous, bool moving)
        {
            if (moving && input.Horizontal != 0) return input.Horizontal < 0 ? -1 : 1;
            if (!input.HasPointer || ViewCamera == null) return previous;
            var ray = ViewCamera.ScreenPointToRay(input.PointerPosition);
            var plane = new Plane(Vector3.forward, position);
            if (!plane.Raycast(ray, out float distance)) return previous;
            float dx = ray.GetPoint(distance).x - position.x;
            return Mathf.Abs(dx) < .001f ? previous : dx < 0 ? -1 : 1;
        }
        public void Present(int facing)
        {
            if (Visual != null) Visual.flipX = facing < 0;
            if (DirectionMarker != null)
            {
                var p = DirectionMarker.localPosition;
                p.x = Mathf.Abs(p.x) * facing;
                DirectionMarker.localPosition = p;
            }
        }
    }
}
