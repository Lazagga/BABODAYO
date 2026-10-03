using UnityEngine;
namespace Babodayo.Combat
{
    [CreateAssetMenu(menuName="Babodayo/Camera Impulse")]
    public sealed class CameraImpulseProfile : ScriptableObject
    {
        [Min(0), Tooltip("Camera displacement in units.")] public float Amplitude=.08f;
        [Min(0), Tooltip("Unscaled impulse duration in seconds.")] public float Duration=.12f;
    }
}
