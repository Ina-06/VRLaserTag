using UnityEngine;

namespace VRLaserTag.Combat
{
    /// <summary>
    /// Offline development bridge. Replace this with the team's host-authoritative
    /// networking implementation during integration.
    /// </summary>
    public sealed class LocalCombatNetworkBridge : CombatNetworkBridge
    {
        [SerializeField] private ShotValidator validator;

        public override bool IsReady => validator != null;

        private void Awake()
        {
            if (validator == null)
            {
                validator = FindAnyObjectByType<ShotValidator>();
            }
        }

        public override void SubmitShot(LaserWeapon source, ShotRequest request)
        {
            ShotResult result = validator == null
                ? ShotResult.Rejected(request.Origin)
                : validator.Validate(source, request);

            source.ResolveAuthoritativeShot(request, result);
        }
    }
}
