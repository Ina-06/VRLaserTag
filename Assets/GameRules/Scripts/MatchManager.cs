
using System.Collections.Generic;
using UnityEngine;

public class MatchManager : MonoBehaviour
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

    public MatchState CurrentState { get; private set; } =
        MatchState.Waiting;

    public float TimeRemaining { get; private set; }
    public string WinnerMessage { get; private set; } = "";

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
        TimeRemaining = matchDuration;
    }

    private void Update()
    {
        if (CurrentState != MatchState.Playing)
            return;

        TimeRemaining = Mathf.Max(
            0f, TimeRemaining - Time.deltaTime
        );

        if (TimeRemaining <= 0f)
            EndMatch();
    }

    public bool RegisterPlayer(PlayerScore score)
    {
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
        if (CurrentState != MatchState.Waiting)
            return;

        ResetAllPlayers();

        WinnerMessage = "";
        TimeRemaining = matchDuration;
        CurrentState = MatchState.Playing;

        Debug.Log(
            $"Match started with {players.Count} players!"
        );
    }

    private void EndMatch()
    {
        CurrentState = MatchState.Finished;
        TimeRemaining = 0f;

        DetermineWinner();

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
                $"WINNER: {winner.name} ({highestKills} kills)";
    }

    [ContextMenu("Restart Match")]
    public void RestartMatch()
    {
        CurrentState = MatchState.Waiting;
        TimeRemaining = matchDuration;
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

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
