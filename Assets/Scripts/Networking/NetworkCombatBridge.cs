using UnityEngine;
using VRLaserTag.Combat;

namespace VRLaserTag.Networking
{
    public sealed class NetworkCombatBridge : CombatNetworkBridge
    {
        [SerializeField] private NetworkWeaponRelay relay;
        public override bool IsReady => relay != null && relay.IsSpawned && relay.IsOwner;

        public override void SubmitShot(LaserWeapon source, ShotRequest request)
        {
            if (IsReady) relay.Submit(source, request);
            else source.ResolveAuthoritativeShot(request, ShotResult.Rejected(request.Origin));
        }
    }
}
