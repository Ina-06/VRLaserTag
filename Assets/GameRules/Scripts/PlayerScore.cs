using UnityEngine;

public class PlayerScore : MonoBehaviour
{
    public int Kills { get; private set; }
    public int Deaths { get; private set; }

    public void AddKill()
    {
        Kills++;
        Debug.Log($"{gameObject.name} scored a kill! Total kills: {Kills}");
    }

    public void AddDeath()
    {
        Deaths++;
        Debug.Log($"{gameObject.name} died. Total deaths: {Deaths}");
    }

    public void ResetScore()
    {
        Kills = 0;
        Deaths = 0;
        Debug.Log($"{gameObject.name}'s score has been reset.");
    }
}
