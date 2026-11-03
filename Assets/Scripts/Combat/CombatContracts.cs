using System;
using UnityEngine;

namespace VRLaserTag.Combat
{
    [Serializable]
    public struct ShotRequest
    {
        public ulong ShooterId;
        public uint Sequence;
        public Vector3 Origin;
        public Vector3 Direction;
        public float RequestedRange;
        public double ClientTime;
    }

    public readonly struct DamageInfo
    {
        public readonly ulong AttackerId;
        public readonly int Amount;
        public readonly uint ShotSequence;
        public readonly Vector3 Point;
        public readonly Vector3 Normal;

        public DamageInfo(
            ulong attackerId,
            int amount,
            uint shotSequence,
            Vector3 point,
            Vector3 normal)
        {
            AttackerId = attackerId;
            Amount = amount;
            ShotSequence = shotSequence;
            Point = point;
            Normal = normal;
        }
    }

    public readonly struct ShotResult
    {
        public readonly bool Accepted;
        public readonly bool Hit;
        public readonly Vector3 EndPoint;
        public readonly Vector3 SurfaceNormal;
        public readonly Collider HitCollider;

        private ShotResult(
            bool accepted,
            bool hit,
            Vector3 endPoint,
            Vector3 surfaceNormal,
            Collider hitCollider)
        {
            Accepted = accepted;
            Hit = hit;
            EndPoint = endPoint;
            SurfaceNormal = surfaceNormal;
            HitCollider = hitCollider;
        }

        public static ShotResult Rejected(Vector3 endPoint)
        {
            return new ShotResult(false, false, endPoint, Vector3.zero, null);
        }

        public static ShotResult Miss(Vector3 endPoint)
        {
            return new ShotResult(true, false, endPoint, Vector3.zero, null);
        }

        public static ShotResult Impact(RaycastHit hit)
        {
            return new ShotResult(true, true, hit.point, hit.normal, hit.collider);
        }
    }

    /// <summary>
    /// Implement this in the game-rules module. Combat reports validated damage;
    /// the rules system remains responsible for health, death, kills and scoring.
    /// </summary>
    public interface IDamageable
    {
        bool IsAlive { get; }
        void ApplyDamage(DamageInfo damage);
    }

    /// <summary>
    /// Optional player-state adapter used to stop dead, frozen or spectating players firing.
    /// </summary>
    public interface ICombatantState
    {
        ulong PlayerId { get; }
        bool CanFire { get; }
    }

    /// <summary>
    /// Adapter point for controller-specific haptics without coupling combat to an XR package.
    /// </summary>
    public interface IWeaponHaptics
    {
        void Pulse(float amplitude, float durationSeconds);
    }

    /// <summary>
    /// Networking owns the concrete implementation. A network bridge sends the request to
    /// the host, validates it there, then calls ResolveAuthoritativeShot on the source weapon.
    /// </summary>
    public abstract class CombatNetworkBridge : MonoBehaviour
    {
        public abstract bool IsReady { get; }
        public abstract void SubmitShot(LaserWeapon source, ShotRequest request);
    }
}
