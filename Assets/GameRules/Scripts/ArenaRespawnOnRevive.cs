
using UnityEngine;

public class ArenaRespawnOnRevive : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private ArenaSpawnManager spawnManager;

    [Tooltip("XR Origin root or dummy player root")]
    [SerializeField] private Transform playerRoot;

    private bool wasDead;
    private bool initialized;

    private void Start()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        if (playerRoot == null)
            playerRoot = transform;

        if (playerHealth == null || spawnManager == null)
        {
            Debug.LogError(
                "ArenaRespawnOnRevive: Missing required references.",
                this);
            enabled = false;
            return;
        }

        wasDead = playerHealth.IsDead;
        initialized = true;
    }

    private void Update()
    {
        if (!initialized)
            return;

        bool currentlyDead = playerHealth.IsDead;

        if (wasDead && !currentlyDead)
        {
            // Move only when a player revives during
            // an active match, not on normal scene load.
            MatchManager manager = MatchManager.Instance;

            if (manager != null &&
                manager.CurrentState ==
                MatchManager.MatchState.Playing)
            {
                spawnManager.MovePlayerToSpawn(playerRoot);
            }
        }

        wasDead = currentlyDead;
    }
}
