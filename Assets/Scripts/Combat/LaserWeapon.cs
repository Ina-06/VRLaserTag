using System.Collections.Generic;
using UnityEngine;

namespace VRLaserTag.Combat
{
    [RequireComponent(typeof(AmmoController))]
    public sealed class LaserWeapon : MonoBehaviour
    {
        [Header("Required references")]
        [SerializeField] private Transform muzzle;
        [SerializeField] private AmmoController ammo;
        [SerializeField] private CombatNetworkBridge networkBridge;
        [SerializeField] private WeaponEffects effects;

        [Header("Optional game-rules adapter")]
        [Tooltip("Assign a MonoBehaviour that implements ICombatantState.")]
        [SerializeField] private MonoBehaviour combatantStateSource;

        [Header("Weapon tuning")]
        [SerializeField, Min(0.01f)] private float secondsBetweenShots = 0.2f;
        [SerializeField, Min(0.1f)] private float range = 50f;

        private ICombatantState combatantState;
        private double nextAllowedShotTime;
        private uint nextSequence;
        private readonly HashSet<uint> pendingShotSequences = new HashSet<uint>();

        public Transform Muzzle => muzzle;
        public AmmoController Ammo => ammo;
        public float Range => range;

        private void Awake()
        {
            if (muzzle == null)
            {
                muzzle = transform;
            }

            if (ammo == null)
            {
                ammo = GetComponent<AmmoController>();
            }

            if (networkBridge == null)
            {
                networkBridge = GetComponent<CombatNetworkBridge>();
            }

            if (effects == null)
            {
                effects = GetComponent<WeaponEffects>();
            }

            combatantState = combatantStateSource as ICombatantState;
            if (combatantStateSource != null && combatantState == null)
            {
                Debug.LogError(
                    $"{combatantStateSource.name} must implement {nameof(ICombatantState)}.",
                    combatantStateSource);
            }
        }

        public bool TryFire()
        {
            if (networkBridge == null || !networkBridge.IsReady)
            {
                return false;
            }

            if (combatantState != null && !combatantState.CanFire)
            {
                return false;
            }

            double now = Time.timeAsDouble;
            if (now < nextAllowedShotTime)
            {
                return false;
            }

            if (!ammo.TryConsumeRound())
            {
                effects?.PlayDryFire();
                return false;
            }

            nextAllowedShotTime = now + secondsBetweenShots;
            nextSequence++;

            ShotRequest request = new ShotRequest
            {
                ShooterId = combatantState?.PlayerId ?? 0UL,
                Sequence = nextSequence,
                Origin = muzzle.position,
                Direction = muzzle.forward.normalized,
                RequestedRange = range,
                ClientTime = now
            };

            pendingShotSequences.Add(request.Sequence);
            networkBridge.SubmitShot(this, request);
            return true;
        }

        public void RequestReload()
        {
            ammo.BeginReload();
        }

        /// <summary>
        /// Called by the local bridge or by the networking implementation after host validation.
        /// </summary>
        public void ResolveAuthoritativeShot(ShotRequest request, ShotResult result)
        {
            if (!pendingShotSequences.Remove(request.Sequence))
            {
                return;
            }

            if (!result.Accepted)
            {
                ammo.RestoreRejectedRound();
                return;
            }

            effects?.PlayShot(request.Origin, result);
        }

        private void OnValidate()
        {
            secondsBetweenShots = Mathf.Max(0.01f, secondsBetweenShots);
            range = Mathf.Max(0.1f, range);
        }
    }
}
