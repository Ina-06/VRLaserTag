
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class MatchManager : NetworkBehaviour
{
    public enum MatchState
    {
        Waiting,
        Playing,
        Finished
    }

    public static MatchManager Instance { get; private set; }

    [SerializeField] private float matchDuration = 300f;

    private readonly Dictionary<ulong, PlayerScore> players =
        new Dictionary<ulong, PlayerScore>();

    private readonly NetworkVariable<MatchState> state = new();
    private readonly NetworkVariable<double> endTime = new();
    private readonly NetworkVariable<FixedString128Bytes> winnerMessage = new();
    private MatchState offlineState;
    private double offlineEndTime;
    private string offlineWinner = "";

    private bool Networked => NetworkObject != null;
    public bool CanManageMatch => !Networked || (IsSpawned && IsServer);
    private double Clock => IsSpawned ? NetworkManager.ServerTime.Time : Time.timeAsDouble;
    public MatchState CurrentState => Networked ? state.Value : offlineState;
    public float TimeRemaining => CurrentState == MatchState.Waiting ? matchDuration :
        CurrentState == MatchState.Finished ? 0f :
        Mathf.Max(0f, (float)((Networked ? endTime.Value : offlineEndTime) - Clock));

    public string WinnerMessage
    {
        get => Networked ? winnerMessage.Value.ToString() : offlineWinner;
        private set
        {
            if (Networked) winnerMessage.Value = new FixedString128Bytes(value);
            else offlineWinner = value;
        }
    }

    private void SetState(MatchState value)
    {
        if (Networked) state.Value = value;
        else offlineState = value;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        SetState(MatchState.Waiting);
        WinnerMessage = "";
    }

    public override void OnNetworkDespawn()
    {
        players.Clear();
    }

    public int RegisteredPlayerCount => players.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Duplicate MatchManager!");
            enabled = false;
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        if (CanManageMatch && CurrentState == MatchState.Playing && TimeRemaining <= 0f)
            EndMatch();
    }

    public bool RegisterPlayer(PlayerScore score)
    {
        if (!CanManageMatch) return false;
        if (score == null)
            return false;

        PlayerIdentity identity =
            score.GetComponent<PlayerIdentity>();

        if (identity == null || identity.PlayerId == 0)
        {
            Debug.LogError(
                $"{score.name} needs a valid PlayerIdentity!"
            );
            return false;
        }

        ulong id = identity.PlayerId;

        if (players.TryGetValue(id, out PlayerScore existing))
        {
            if (existing == score)
                return true;

            Debug.LogError($"Duplicate Player ID: {id}");
            return false;
        }

        players.Add(id, score);

        Debug.Log(
            $"{score.name} registered with ID {id}. " +
            $"Total players: {players.Count}"
        );

        return true;
    }

    public void UnregisterPlayer(PlayerScore score)
    {
        if (score == null)
            return;

        PlayerIdentity identity =
            score.GetComponent<PlayerIdentity>();

        if (identity == null)
            return;

        ulong id = identity.PlayerId;

        if (players.TryGetValue(id, out PlayerScore existing)
            && existing == score)
        {
            players.Remove(id);

            Debug.Log(
                $"Player {id} unregistered. " +
                $"Total players: {players.Count}"
            );
        }
    }

    public bool TryGetPlayer(
        ulong id, out PlayerScore score)
    {
        return players.TryGetValue(id, out score);
    }

    public void ReportElimination(
        ulong attackerId, ulong victimId)
    {
        if (!CanManageMatch) return;
        if (CurrentState != MatchState.Playing)
            return;

        if (!players.TryGetValue(victimId,
            out PlayerScore victim))
            return;

        victim.AddDeath();

        if (attackerId != victimId &&
            players.TryGetValue(attackerId,
                out PlayerScore attacker))
        {
            attacker.AddKill();
        }
    }

    [ContextMenu("Start Match")]
    public void StartMatch()
    {
        if (!CanManageMatch || CurrentState != MatchState.Waiting)
            return;

        ResetAllPlayers();

        WinnerMessage = "";
        if (Networked) endTime.Value = Clock + matchDuration;
        else offlineEndTime = Clock + matchDuration;
        SetState(MatchState.Playing);

        Debug.Log(
            $"Match started with {players.Count} players!"
        );
    }

    private void EndMatch()
    {
        DetermineWinner();
        SetState(MatchState.Finished);

        Debug.Log("Match finished! " + WinnerMessage);
    }

    private void DetermineWinner()
    {
        int highestKills = int.MinValue;
        PlayerScore winner = null;
        bool tie = false;

        foreach (PlayerScore player in players.Values)
        {
            if (player == null)
                continue;

            if (player.Kills > highestKills)
            {
                highestKills = player.Kills;
                winner = player;
                tie = false;
            }
            else if (player.Kills == highestKills)
            {
                tie = true;
            }
        }

        if (winner == null)
            WinnerMessage = "No players registered";
        else if (tie)
            WinnerMessage = $"TIE - {highestKills} kills";
        else
            WinnerMessage =
                $"WINNER: {(Networked ? $"Player {winner.GetComponent<PlayerIdentity>().PlayerId}" : winner.name)} ({highestKills} kills)";
    }

    [ContextMenu("Restart Match")]
    public void RestartMatch()
    {
        if (!CanManageMatch) return;
        SetState(MatchState.Waiting);
        WinnerMessage = "";

        ResetAllPlayers();

        Debug.Log("Match reset. Players restored.");
    }

    private void ResetAllPlayers()
    {
        foreach (PlayerScore score in players.Values)
        {
            if (score == null)
                continue;

            score.ResetScore();

            PlayerHealth health =
                score.GetComponent<PlayerHealth>();

            if (health != null)
                health.ResetForNewMatch();
        }
    }

    public override void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        base.OnDestroy();
    }
}
