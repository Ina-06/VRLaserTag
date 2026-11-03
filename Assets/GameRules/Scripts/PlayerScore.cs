
using UnityEngine;

public class PlayerScore : MonoBehaviour
{
    public int Kills { get; private set; }
    public int Deaths { get; private set; }

    private bool isRegistered;

    private void Start()
    {
        RegisterWithMatch();
    }

    private void OnEnable()
    {
        RegisterWithMatch();
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
        Kills++;

        Debug.Log(
            $"{name} scored a kill! Total kills: {Kills}"
        );
    }

    public void AddDeath()
    {
        Deaths++;

        Debug.Log(
            $"{name} died. Total deaths: {Deaths}"
        );
    }

    public void ResetScore()
    {
        Kills = 0;
        Deaths = 0;

        Debug.Log($"{name}'s score has been reset.");
    }
}
