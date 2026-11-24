
using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using VRLaserTag.Networking;

public class MatchManager : NetworkBehaviour
{
    public enum MatchState
    {
        Waiting,
        Playing,
        Finished
    }

    public static MatchManager Instance { get; private set; }

    // Raised on every headset. Messages for the kill feed (eliminations, joins, leaves, match news).
    public static event Action<string> FeedMessage;
    // Raised on every headset for each validated hit: attacker ID, victim ID, whether it killed.
    public static event Action<ulong, ulong, bool> DamageEvent;

    [SerializeField] private float matchDuration = 300f;

    [Tooltip("Seconds the winner stays on screen before everyone returns to the lobby.")]
    [SerializeField] private float resultsDuration = 8f;

    private readonly Dictionary<ulong, PlayerScore> players =
        new Dictionary<ulong, PlayerScore>();

    private readonly NetworkVariable<MatchState> state = new();
    private readonly NetworkVariable<double> endTime = new();
    private readonly NetworkVariable<FixedString128Bytes> winnerMessage = new();
    // Server time at which a pending start countdown finishes; 0 = no countdown.
    private readonly NetworkVariable<double> countdownEnd = new();
    private MatchState offlineState;
    private double offlineEndTime;
    private double offlineCountdownEnd;
    private double finishedAt;
    private string offlineWinner = "";

    private bool Networked => NetworkObject != null;
    public bool CanManageMatch => !Networked || (IsSpawned && IsServer);
    private double Clock => IsSpawned ? NetworkManager.ServerTime.Time : Time.timeAsDouble;
    public MatchState CurrentState => Networked ? state.Value : offlineState;
    public float TimeRemaining => CurrentState == MatchState.Waiting ? matchDuration :
        CurrentState == MatchState.Finished ? 0f :
        Mathf.Max(0f, (float)((Networked ? endTime.Value : offlineEndTime) - Clock));

    public float MatchDuration => matchDuration;
    private double CountdownEndValue => Networked ? countdownEnd.Value : offlineCountdownEnd;
    public bool CountdownActive => CurrentState == MatchState.Waiting && CountdownEndValue > 0;
    public float CountdownRemaining =>
        CountdownActive ? Mathf.Max(0f, (float)(CountdownEndValue - Clock)) : 0f;
    private bool EnoughPlayers => !IsSpawned || players.Count >= 2;

    // True on the host while waiting with at least two players and no countdown running.
    public bool CanStartCountdown =>
        CanManageMatch && CurrentState == MatchState.Waiting && !CountdownActive && EnoughPlayers;

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

    private void SetCountdownEnd(double value)
    {
        if (Networked) countdownEnd.Value = value;
        else offlineCountdownEnd = value;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        SetState(MatchState.Waiting);
        SetCountdownEnd(0);
        WinnerMessage = "";
        NetworkManager.OnConnectionEvent += OnConnectionEvent;
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager != null)
            NetworkManager.OnConnectionEvent -= OnConnectionEvent;
        players.Clear();
    }

    private void OnConnectionEvent(NetworkManager source, ConnectionEventData data)
    {
        if (data.EventType == ConnectionEvent.ClientConnected)
            Announce($"Player {data.ClientId + 1} joined");
        else if (data.EventType == ConnectionEvent.ClientDisconnected)
            Announce($"Player {data.ClientId + 1} disconnected");
    }

    // Host only: sends a kill-feed line to every headset.
    private void Announce(string message)
    {
        if (IsSpawned && IsServer) FeedRpc(new FixedString128Bytes(message));
        else if (!IsSpawned) FeedMessage?.Invoke(message);
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    private void FeedRpc(FixedString128Bytes message)
    {
        FeedMessage?.Invoke(message.ToString());
    }

    // Host only: called by PlayerHealth after a validated hit lands.
    public void ReportHit(ulong attackerId, ulong victimId, bool killed)
    {
        if (!CanManageMatch) return;
        if (IsSpawned) DamageEventRpc(attackerId, victimId, killed);
        else DamageEvent?.Invoke(attackerId, victimId, killed);
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    private void DamageEventRpc(ulong attackerId, ulong victimId, bool killed)
    {
        DamageEvent?.Invoke(attackerId, victimId, killed);
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
        if (!CanManageMatch) return;

        switch (CurrentState)
        {
            case MatchState.Waiting:
                if (CountdownEndValue <= 0) break;
                if (!EnoughPlayers) SetCountdownEnd(0); // someone left during the countdown
                else if (Clock >= CountdownEndValue) StartMatch();
                break;

            case MatchState.Playing:
                if (TimeRemaining <= 0f) EndMatch();
                break;

            case MatchState.Finished:
                if (resultsDuration >= 0f && Clock - finishedAt >= resultsDuration)
                    RestartMatch();
                break;
        }
    }

    // Host only: starts the shared pre-match countdown shown on every headset.
    public bool BeginCountdown(float seconds)
    {
        if (!CanStartCountdown) return false;
        SetCountdownEnd(Clock + Mathf.Max(0f, seconds));
        Debug.Log($"Match countdown started ({seconds:0} s).");
        return true;
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
            Announce($"Player {attackerId} eliminated Player {victimId}");
        }
        else
        {
            Announce($"Player {victimId} was eliminated");
        }
    }

    [ContextMenu("Start Match")]
    public void StartMatch()
    {
        if (!CanManageMatch || CurrentState != MatchState.Waiting)
            return;

        ResetAllPlayers();
        SendPlayersToSpawns();

        SetCountdownEnd(0);
        WinnerMessage = "";
        if (Networked) endTime.Value = Clock + matchDuration;
        else offlineEndTime = Clock + matchDuration;
        SetState(MatchState.Playing);
        Announce("Match started!");

        Debug.Log(
            $"Match started with {players.Count} players!"
        );
    }

    private void EndMatch()
    {
        DetermineWinner();
        finishedAt = Clock;
        SetState(MatchState.Finished);
        Announce(string.IsNullOrEmpty(WinnerMessage) ? "Match over" : WinnerMessage);

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
        SetCountdownEnd(0);
        SetState(MatchState.Waiting);
        WinnerMessage = "";

        ResetAllPlayers();
        SendPlayersToSpawns();
        Announce("Back to the lobby");

        Debug.Log("Match reset. Players returned to spawns.");
    }

    // Moves every player back to an arena spawn point (each owner moves its own rig).
    private void SendPlayersToSpawns()
    {
        foreach (PlayerScore score in players.Values)
        {
            if (score == null)
                continue;

            NetworkPlayerSetup setup = score.GetComponent<NetworkPlayerSetup>();
            if (setup != null)
                setup.RespawnOwner();
        }
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
