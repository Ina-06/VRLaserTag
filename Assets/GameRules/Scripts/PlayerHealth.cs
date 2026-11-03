
using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float respawnDelay = 3f;
    [SerializeField] private MatchManager matchManager;

    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    private Coroutine respawnCoroutine;
    private PlayerIdentity identity;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        IsDead = false;

        identity = GetComponent<PlayerIdentity>();
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, 0UL);
    }

    public void TakeDamage(int damage, ulong attackerId)
    {
        if (matchManager == null ||
            matchManager.CurrentState !=
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

        if (identity != null)
        {
            matchManager.ReportElimination(
                attackerId, identity.PlayerId
            );
        }
        else
        {
            Debug.LogError(
                $"{name} has no PlayerIdentity!"
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

        if (IsDead &&
            matchManager != null &&
            matchManager.CurrentState ==
            MatchManager.MatchState.Playing)
        {
            Respawn();
        }
    }

    public void Respawn()
    {
        if (matchManager == null ||
            matchManager.CurrentState !=
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

        Debug.Log($"{name} reset to {CurrentHealth} HP.");
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
