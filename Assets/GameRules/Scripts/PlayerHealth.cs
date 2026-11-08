
using System.Collections;
using UnityEngine;
using Unity.Netcode;
using VRLaserTag.Networking;
using VRLaserTag.Combat;

[RequireComponent(typeof(PlayerIdentity))]
[RequireComponent(typeof(PlayerScore))]
public class PlayerHealth : NetworkBehaviour,
    IDamageable, ICombatantState
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float respawnDelay = 3f;

    [Header("Game Rules")]
    [SerializeField] private MatchManager matchManager;

    [Header("Arena Respawning")]
    [SerializeField] private ArenaSpawnManager spawnManager;

    private PlayerIdentity identity;
    private Coroutine respawnCoroutine;

    private readonly NetworkVariable<int> health = new();
    private int offlineHealth;
    private bool Networked => NetworkObject != null;
    private bool CanMutate => !Networked || (IsSpawned && IsServer);
    public int CurrentHealth => Networked ? health.Value : offlineHealth;
    public bool IsDead => CurrentHealth <= 0;

    public override void OnNetworkSpawn()
    {
        if (IsServer) health.Value = maxHealth;
    }

    public override void OnNetworkDespawn()
    {
        CancelPendingRespawn();
    }

    private void SetHealth(int value)
    {
        if (Networked) health.Value = value;
        else offlineHealth = value;
    }
    public bool IsAlive => !IsDead;

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
        offlineHealth = maxHealth;
    }

    private MatchManager GetMatchManager()
    {
        if (matchManager == null)
            matchManager = MatchManager.Instance;

        return matchManager;
    }

    public void ApplyDamage(DamageInfo damage)
    {
        TakeDamage(damage.Amount, damage.AttackerId);
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, 0UL);
    }

    public void TakeDamage(int damage, ulong attackerId)
    {
        if (!CanMutate) return;
        MatchManager manager = GetMatchManager();

        if (manager == null ||
            manager.CurrentState !=
            MatchManager.MatchState.Playing)
            return;

        if (IsDead || damage <= 0)
            return;

        SetHealth(Mathf.Max(0, CurrentHealth - damage));

        Debug.Log(
            $"{name} took {damage} damage. HP: {CurrentHealth}"
        );

        if (CurrentHealth == 0)
            Die(attackerId);
    }

    private void Die(ulong attackerId)
    {
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
        if (!CanMutate) return;
        MatchManager manager = GetMatchManager();

        if (manager == null ||
            manager.CurrentState !=
            MatchManager.MatchState.Playing)
            return;

        if (!IsDead)
            return;

        CancelPendingRespawn();

        if (Networked)
        {
            var player = GetComponent<NetworkPlayerSetup>();
            if (player != null) player.RespawnOwner();
        }
        else if (spawnManager != null)
            spawnManager.MovePlayerToSpawn(transform);

        RestoreHealth();

        Debug.Log(
            $"{name} respawned with {CurrentHealth} HP."
        );
    }

    public void ResetForNewMatch()
    {
        if (!CanMutate) return;
        CancelPendingRespawn();
        RestoreHealth();

        Debug.Log(
            $"{name} reset to {CurrentHealth} HP."
        );
    }

    private void RestoreHealth()
    {
        SetHealth(maxHealth);
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
