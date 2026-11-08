using UnityEngine;
using VRLaserTag.Combat;

namespace VRLaserTag.Networking
{
    public sealed class OwnerWeaponHaptics : MonoBehaviour, IWeaponHaptics
    {
        [SerializeField] private NetworkPlayerSetup player;
        [SerializeField] private MonoBehaviour source;

        public void Pulse(float amplitude, float durationSeconds)
        {
            if (player != null && player.IsSpawned && player.IsOwner && source is IWeaponHaptics haptics)
                haptics.Pulse(amplitude, durationSeconds);
        }
    }
}
