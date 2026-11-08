using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using VRLaserTag.Combat;

namespace VRLaserTag.Networking
{
    public sealed class NetworkWeaponRelay : NetworkBehaviour
    {
        [SerializeField] private LaserWeapon weapon;
        [SerializeField] private WeaponEffects effects;
        private readonly Dictionary<uint, ShotRequest> pending = new();
        private PlayerHealth health;
        private ShotValidator validator;
        private uint lastSequence;
        private double nextAllowedShot;
        private bool hasSequence;

        public override void OnNetworkSpawn()
        {
            health = GetComponent<PlayerHealth>();
            if (IsServer) validator = FindAnyObjectByType<ShotValidator>();
        }

        public override void OnNetworkDespawn()
        {
            foreach (ShotRequest request in pending.Values)
                if (weapon != null) weapon.ResolveAuthoritativeShot(request, ShotResult.Rejected(request.Origin));
            pending.Clear();
            hasSequence = false;
            nextAllowedShot = 0;
        }

        internal void Submit(LaserWeapon source, ShotRequest request)
        {
            if (!IsSpawned || !IsOwner || source != weapon)
            {
                source.ResolveAuthoritativeShot(request, ShotResult.Rejected(request.Origin));
                return;
            }
            pending.Add(request.Sequence, request);
            if (IsServer) ValidateShot(request.Sequence, request.Origin, request.Direction, request.RequestedRange);
            else SubmitShotRpc(request.Sequence, request.Origin, request.Direction, request.RequestedRange);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void SubmitShotRpc(uint sequence, Vector3 origin, Vector3 direction, float range, RpcParams rpcParams = default)
        {
            if (IsServer && rpcParams.Receive.SenderClientId == OwnerClientId)
                ValidateShot(sequence, origin, direction, range);
        }

        private void ValidateShot(uint sequence, Vector3 origin, Vector3 direction, float range)
        {
            if (hasSequence && unchecked((int)(sequence - lastSequence)) <= 0) return;
            hasSequence = true;
            lastSequence = sequence;
            double now = NetworkManager.ServerTime.Time;
            if (now < nextAllowedShot || health == null || !health.CanFire || validator == null || weapon == null)
            {
                ShotResolvedRpc(sequence, false, false, Vector3.zero, Vector3.zero, Vector3.zero);
                return;
            }

            nextAllowedShot = now + 0.2;
            Physics.SyncTransforms();
            ShotResult result = validator.Validate(weapon, new ShotRequest
            {
                ShooterId = health.PlayerId, Sequence = sequence, Origin = origin,
                Direction = direction, RequestedRange = range
            });
            ShotResolvedRpc(sequence, result.Accepted, result.Hit,
                result.Accepted ? origin : Vector3.zero,
                result.Accepted ? result.EndPoint : Vector3.zero, result.SurfaceNormal);
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        private void ShotResolvedRpc(uint sequence, bool accepted, bool hit, Vector3 origin, Vector3 end, Vector3 normal)
        {
            ShotResult result = !accepted ? ShotResult.Rejected(end) : hit
                ? ShotResult.Impact(new RaycastHit { point = end, normal = normal })
                : ShotResult.Miss(end);
            if (IsOwner)
            {
                if (pending.Remove(sequence, out ShotRequest request))
                    weapon.ResolveAuthoritativeShot(request, result);
            }
            else if (accepted && effects != null)
                effects.PlayShot(origin, result);
        }
    }
}
