
using UnityEngine;

public class HealthTester : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerIdentity attackerIdentity;

    [ContextMenu("Take 25 Damage")]
    public void TestDamage()
    {
        if (playerHealth == null)
            return;

        ulong attackerId =
            attackerIdentity != null
                ? attackerIdentity.PlayerId
                : 0UL;

        playerHealth.TakeDamage(25, attackerId);
    }

    [ContextMenu("Respawn Player")]
    public void TestRespawn()
    {
        if (playerHealth != null)
            playerHealth.Respawn();
    }
}
