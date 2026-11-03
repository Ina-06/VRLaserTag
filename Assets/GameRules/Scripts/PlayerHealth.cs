
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

    private void Awake()
    {
        CurrentHealth = maxHealth;
        IsDead = false;
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, null);
    }

    public void TakeDamage(int damage, PlayerScore attacker)
    {
        if (matchManager == null ||
            matchManager.CurrentState != MatchManager.MatchState.Playing)
            return;

        if (IsDead || damage <= 0)
            return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);

        Debug.Log($"{gameObject.name} took {damage} damage. HP: {CurrentHealth}");

        if (CurrentHealth == 0)
        {
            if (attacker != null && attacker.gameObject != gameObject)
                attacker.AddKill();

            Die();
        }
    }

    private void Die()
    {
        IsDead = true;

        PlayerScore score = GetComponent<PlayerScore>();

        if (score != null)
            score.AddDeath();

        Debug.Log($"{gameObject.name} has died!");

        respawnCoroutine = StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        respawnCoroutine = null;

        if (matchManager != null &&
            matchManager.CurrentState == MatchManager.MatchState.Playing)
        {
            Respawn();
        }
    }

    public void Respawn()
    {
        if (matchManager == null ||
            matchManager.CurrentState != MatchManager.MatchState.Playing)
            return;

        RestoreHealth();
        Debug.Log($"{gameObject.name} respawned with {CurrentHealth} HP.");
    }

    public void ResetForNewMatch()
    {
        if (respawnCoroutine != null)
        {
            StopCoroutine(respawnCoroutine);
            respawnCoroutine = null;
        }

        RestoreHealth();

        Debug.Log($"{gameObject.name} reset to {CurrentHealth} HP.");
    }

    private void RestoreHealth()
    {
        CurrentHealth = maxHealth;
        IsDead = false;
    }
}
