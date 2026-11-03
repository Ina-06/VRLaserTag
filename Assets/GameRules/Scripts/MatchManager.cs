
using UnityEngine;

public class MatchManager : MonoBehaviour
{
    public enum MatchState
    {
        Waiting,
        Playing,
        Finished
    }

    [SerializeField] private float matchDuration = 300f;
    [SerializeField] private PlayerScore[] players;

    public MatchState CurrentState { get; private set; } = MatchState.Waiting;
    public float TimeRemaining { get; private set; }
    public string WinnerMessage { get; private set; } = "";

    private void Awake()
    {
        TimeRemaining = matchDuration;
    }

    private void Update()
    {
        if (CurrentState != MatchState.Playing)
            return;

        TimeRemaining = Mathf.Max(0f, TimeRemaining - Time.deltaTime);

        if (TimeRemaining <= 0f)
            EndMatch();
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

        Debug.Log("Match started!");
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
        if (players == null || players.Length == 0)
        {
            WinnerMessage = "No players registered";
            return;
        }

        int highestKills = int.MinValue;
        PlayerScore winner = null;
        bool tie = false;

        foreach (PlayerScore player in players)
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
            WinnerMessage = "No valid players registered";
        else if (tie)
            WinnerMessage = $"TIE - {highestKills} kills";
        else
            WinnerMessage = $"WINNER: {winner.gameObject.name} ({highestKills} kills)";
    }

    [ContextMenu("Restart Match")]
    public void RestartMatch()
    {
        CurrentState = MatchState.Waiting;
        TimeRemaining = matchDuration;
        WinnerMessage = "";

        ResetAllPlayers();

        Debug.Log("Match reset. All players restored and scores cleared.");
    }

    private void ResetAllPlayers()
    {
        if (players == null)
            return;

        foreach (PlayerScore player in players)
        {
            if (player == null)
                continue;

            player.ResetScore();

            PlayerHealth health = player.GetComponent<PlayerHealth>();

            if (health != null)
                health.ResetForNewMatch();
        }
    }
}
