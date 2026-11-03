using UnityEngine;

namespace VRLaserTag.Combat
{
    /// <summary>
    /// Standalone target for the Combat test scene. The game-rules module must provide
    /// the production IDamageable implementation for network players.
    /// </summary>
    [AddComponentMenu("VR Laser Tag/Combat/Debug Damageable Target")]
    public sealed class DebugDamageableTarget : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1)] private int maximumHealth = 100;

        public int CurrentHealth { get; private set; }
        public bool IsAlive => CurrentHealth > 0;

        private void Awake()
        {
            ResetHealth();
        }

        public void ApplyDamage(DamageInfo damage)
        {
            if (!IsAlive || damage.Amount <= 0)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - damage.Amount);
            Debug.Log(
                $"{name} took {damage.Amount} damage from player {damage.AttackerId}. " +
                $"Health: {CurrentHealth}/{maximumHealth}",
                this);
        }

        [ContextMenu("Reset Health")]
        public void ResetHealth()
        {
            CurrentHealth = maximumHealth;
        }

        private void OnValidate()
        {
            maximumHealth = Mathf.Max(1, maximumHealth);
        }
    }
}
