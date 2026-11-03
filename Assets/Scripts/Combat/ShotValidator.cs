using System;
using UnityEngine;

namespace VRLaserTag.Combat
{
    public sealed class ShotValidator : MonoBehaviour
    {
        [SerializeField] private LayerMask collisionMask = ~0;
        [SerializeField, Min(1)] private int authoritativeDamage = 25;
        [SerializeField, Min(0.1f)] private float maximumRange = 50f;
        [SerializeField, Min(0f)] private float maximumOriginError = 0.35f;
        [SerializeField, Range(0f, 90f)] private float maximumAimErrorDegrees = 15f;
        [SerializeField] private QueryTriggerInteraction triggerInteraction =
            QueryTriggerInteraction.Ignore;

        public ShotResult Validate(LaserWeapon source, ShotRequest request)
        {
            if (source == null || source.Muzzle == null || !IsFinite(request.Origin) ||
                !IsFinite(request.Direction) || request.Direction.sqrMagnitude < 0.99f)
            {
                return ShotResult.Rejected(request.Origin);
            }

            Transform muzzle = source.Muzzle;
            Vector3 direction = request.Direction.normalized;

            bool originIsValid =
                Vector3.Distance(request.Origin, muzzle.position) <= maximumOriginError;
            bool aimIsValid =
                Vector3.Angle(direction, muzzle.forward) <= maximumAimErrorDegrees;
            bool rangeIsValid =
                request.RequestedRange > 0f && request.RequestedRange <= maximumRange;

            if (!originIsValid || !aimIsValid || !rangeIsValid)
            {
                return ShotResult.Rejected(request.Origin);
            }

            float rayDistance = Mathf.Min(request.RequestedRange, maximumRange);
            RaycastHit[] hits = Physics.RaycastAll(
                request.Origin,
                direction,
                rayDistance,
                collisionMask,
                triggerInteraction);

            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.transform.IsChildOf(source.transform.root))
                {
                    continue;
                }

                IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
                if (damageable != null && damageable.IsAlive)
                {
                    damageable.ApplyDamage(new DamageInfo(
                        request.ShooterId,
                        authoritativeDamage,
                        request.Sequence,
                        hit.point,
                        hit.normal));
                }

                // The first non-shooter collider always stops the laser. If it is not
                // damageable, it is cover and no damage is applied.
                return ShotResult.Impact(hit);
            }

            return ShotResult.Miss(request.Origin + direction * rayDistance);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private void OnValidate()
        {
            authoritativeDamage = Mathf.Max(1, authoritativeDamage);
            maximumRange = Mathf.Max(0.1f, maximumRange);
            maximumOriginError = Mathf.Max(0f, maximumOriginError);
            maximumAimErrorDegrees = Mathf.Clamp(maximumAimErrorDegrees, 0f, 90f);
        }
    }
}
