
using System.Collections;
using UnityEngine;
using VRLaserTag.Combat;

[RequireComponent(typeof(PlayerIdentity))]
[RequireComponent(typeof(PlayerScore))]
public class PlayerHealth : MonoBehaviour, IDamageable, ICombatantState
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float respawnDelay = 3f;

    [Header("Game Rules")]
    [SerializeField] private MatchManager matchManager;

    private PlayerIdentity identity;
    private Coroutine respawnCoroutine;

    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    // Required by Combat: IDamageable
    public bool IsAlive => !IsDead;

    // Required by Combat: ICombatantState
    public ulong PlayerId =>
        identity != null ? identity.PlayerId : 0UL;

    public bool CanFire =>
        !IsDead &&
        GetMatchManager() != null &&
        GetMatchManager().CurrentState ==
        MatchManager.MatchState.Playing;

    private void Awake()
    {
        identity = GetComponent<PlayerIdentity>();

        CurrentHealth = maxHealth;
        IsDead = false;
    }

    private MatchManager GetMatchManager()
    {
        if (matchManager == null)
            matchManager = MatchManager.Instance;

        return matchManager;
    }

    // Called by Combat's ShotValidator.
    public void ApplyDamage(DamageInfo damage)
    {
        TakeDamage(damage.Amount, damage.AttackerId);
    }

    // Existing local testing method.
    public void TakeDamage(int damage)
    {
        TakeDamage(damage, 0UL);
    }

    // ID-based damage handling.
    public void TakeDamage(int damage, ulong attackerId)
    {
        MatchManager manager = GetMatchManager();

        if (manager == null)
        {
            Debug.LogError(
                $"{name}: MatchManager not found!"
            );
            return;
        }

        if (manager.CurrentState !=
            MatchManager.MatchState.Playing)
            return;

        if (IsDead || damage <= 0)
            return;

        CurrentHealth = Mathf.Max(
            0, CurrentHealth - damage
        );

        Debug.Log(
            $"{name} took {damage} damage. " +
            $"HP: {CurrentHealth}"
        );

        if (CurrentHealth == 0)
            Die(attackerId);
    }

    private void Die(ulong attackerId)
    {
        if (IsDead)
            return;

        IsDead = true;

        MatchManager manager = GetMatchManager();

        if (manager != null && identity != null)
        {
            manager.ReportElimination(
                attackerId, identity.PlayerId
            );
        }

        Debug.Log($"{name} has died!");

        CancelPendingRespawn();

        respawnCoroutine =
            StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);

        respawnCoroutine = null;

        MatchManager manager = GetMatchManager();

        if (IsDead &&
            manager != null &&
            manager.CurrentState ==
            MatchManager.MatchState.Playing)
        {
            Respawn();
        }
    }

    public void Respawn()
    {
        MatchManager manager = GetMatchManager();

        if (manager == null ||
            manager.CurrentState !=
            MatchManager.MatchState.Playing)
            return;

        if (!IsDead)
            return;

        CancelPendingRespawn();
        RestoreHealth();

        Debug.Log(
            $"{name} respawned with {CurrentHealth} HP."
        );
    }

    public void ResetForNewMatch()
    {
        CancelPendingRespawn();
        RestoreHealth();

        Debug.Log(
            $"{name} reset to {CurrentHealth} HP."
        );
    }

    private void RestoreHealth()
    {
        CurrentHealth = maxHealth;
        IsDead = false;
    }

    private void CancelPendingRespawn()
    {
        if (respawnCoroutine == null)
            return;

        StopCoroutine(respawnCoroutine);
        respawnCoroutine = null;
    }

    private void OnDisable()
    {
        CancelPendingRespawn();
    }
}
