
using UnityEngine;
using TMPro;

public class GameHUD : MonoBehaviour
{
    [SerializeField] private MatchManager matchManager;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerScore playerScore;
    [SerializeField] private TMP_Text scoreboardText;

    private void Update()
    {
        if (matchManager == null ||
            playerHealth == null ||
            playerScore == null ||
            scoreboardText == null)
            return;

        int seconds = Mathf.CeilToInt(matchManager.TimeRemaining);
        int minutes = seconds / 60;
        int remainingSeconds = seconds % 60;

        string text =
            $"MATCH: {matchManager.CurrentState}\n" +
            $"TIME: {minutes:00}:{remainingSeconds:00}\n" +
            $"HEALTH: {playerHealth.CurrentHealth} HP\n" +
            $"KILLS: {playerScore.Kills}\n" +
            $"DEATHS: {playerScore.Deaths}";

        if (matchManager.CurrentState == MatchManager.MatchState.Finished)
        {
            text += $"\n\n{matchManager.WinnerMessage}";
        }

        scoreboardText.text = text;
    }
}
