using UnityEngine;
namespace Babodayo.CameraSystem
{
    public sealed class CameraRoom : MonoBehaviour
    {
        [Tooltip("Room width/height in units.")] public Vector2 Size=new Vector2(18,10);
        [Range(0,1), Tooltip("Center influence; 1 is fully room centered.")] public float CenterWeight=.8f;
        [Tooltip("Clamp camera view inside the room when possible.")] public bool Constrain=true;
        public Bounds Bounds => new Bounds(transform.position,new Vector3(Size.x,Size.y,100));
        public bool Contains(Vector2 position) => Bounds.Contains(new Vector3(position.x,position.y,transform.position.z));
        private void OnDrawGizmos() { Gizmos.color=new Color(.5f,.4f,1); Gizmos.DrawWireCube(transform.position,new Vector3(Size.x,Size.y,0)); }
    }
}
