
using UnityEngine;
using Unity.Netcode;

public class PlayerScore : NetworkBehaviour
{
    private readonly NetworkVariable<int> kills = new();
    private readonly NetworkVariable<int> deaths = new();
    private int offlineKills;
    private int offlineDeaths;

    private bool Networked => NetworkObject != null;
    private bool CanMutate => !Networked || (IsSpawned && IsServer);
    public int Kills => Networked ? kills.Value : offlineKills;
    public int Deaths => Networked ? deaths.Value : offlineDeaths;

    protected override void OnNetworkPostSpawn()
    {
        if (IsServer) RegisterWithMatch();
    }

    public override void OnNetworkDespawn()
    {
        UnregisterFromMatch();
    }

    private bool isRegistered;

    private void Start()
    {
        if (!Networked) RegisterWithMatch();
    }

    private void OnEnable()
    {
        if (!Networked) RegisterWithMatch();
    }

    private void Update()
    {
        if (IsSpawned && IsServer && !isRegistered) RegisterWithMatch();
    }

    private void OnDisable()
    {
        UnregisterFromMatch();
    }

    private void RegisterWithMatch()
    {
        if (isRegistered || MatchManager.Instance == null)
            return;

        isRegistered =
            MatchManager.Instance.RegisterPlayer(this);
    }

    private void UnregisterFromMatch()
    {
        if (!isRegistered)
            return;

        if (MatchManager.Instance != null)
            MatchManager.Instance.UnregisterPlayer(this);

        isRegistered = false;
    }

    public void AddKill()
    {
        if (!CanMutate) return;
        if (Networked) kills.Value++;
        else offlineKills++;

        Debug.Log(
            $"{name} scored a kill! Total kills: {Kills}"
        );
    }

    public void AddDeath()
    {
        if (!CanMutate) return;
        if (Networked) deaths.Value++;
        else offlineDeaths++;

        Debug.Log(
            $"{name} died. Total deaths: {Deaths}"
        );
    }

    public void ResetScore()
    {
        if (!CanMutate) return;
        if (Networked) { kills.Value = 0; deaths.Value = 0; }
        else { offlineKills = 0; offlineDeaths = 0; }

        Debug.Log($"{name}'s score has been reset.");
    }
}
