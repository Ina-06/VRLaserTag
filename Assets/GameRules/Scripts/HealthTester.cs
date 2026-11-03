
using UnityEngine;

public class HealthTester : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerScore attackerScore;

    [ContextMenu("Take 25 Damage")]
    public void TestDamage()
    {
        if (playerHealth == null)
            return;

        playerHealth.TakeDamage(25, attackerScore);
    }

    [ContextMenu("Respawn Player")]
    public void TestRespawn()
    {
        if (playerHealth != null)
            playerHealth.Respawn();
    }
}
