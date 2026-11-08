
using UnityEngine;

public class PlayerIdentity : MonoBehaviour
{
    [SerializeField] private int offlinePlayerId = 1;

    public ulong PlayerId => (ulong)Mathf.Max(0, offlinePlayerId);

    private void Awake()
    {
        if (offlinePlayerId <= 0)
        {
            Debug.LogError(
                $"{gameObject.name} must have a positive Player ID!"
            );
        }
    }

    public void SetPlayerId(ulong newId)
    {
        if (newId == 0)
        {
            Debug.LogError("Player ID cannot be zero in offline testing.");
            return;
        }

        // Network IDs may be larger than int.MaxValue.
        // The networking system will eventually own these IDs.
        if (newId > int.MaxValue)
        {
            Debug.LogError(
                "Offline Player ID is too large. Use a network identity later."
            );
            return;
        }

        offlinePlayerId = (int)newId;

        Debug.Log(
            $"{gameObject.name} assigned Player ID: {PlayerId}"
        );
    }
}
