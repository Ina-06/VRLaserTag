
using UnityEngine;

public class ArenaSpawnManager : MonoBehaviour
{
    [Header("Existing Arena Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Optional Safety Check")]
    [SerializeField]
    private TeleportDestinationValidator destinationValidator;

    private int nextSpawn;

    public bool MovePlayerToSpawn(Transform player)
    {
        return MovePlayerToSpawn(player, nextSpawn);
    }

    public bool MovePlayerToSpawn(Transform player, int startIndex)
    {
        if (player == null ||
            spawnPoints == null ||
            spawnPoints.Length == 0)
        {
            Debug.LogWarning(
                "ArenaSpawnManager: Missing player or spawn points.");
            return false;
        }

        Transform selected = null;

        // Cycle through valid, configured spawn points.
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            int index = (startIndex + i) % spawnPoints.Length;
            Transform candidate = spawnPoints[index];

            if (candidate == null)
                continue;

            if (destinationValidator != null &&
                !destinationValidator.IsValidDestination(
                    candidate.position))
                continue;

            selected = candidate;
            nextSpawn = (index + 1) % spawnPoints.Length;
            break;
        }

        if (selected == null)
        {
            Debug.LogWarning(
                "ArenaSpawnManager: No valid spawn point found.");
            return false;
        }

        CharacterController controller =
            player.GetComponent<CharacterController>();

        bool controllerWasEnabled =
            controller != null && controller.enabled;

        if (controllerWasEnabled)
            controller.enabled = false;

        // XR rigs must move their root, not the
        // headset camera itself.
        Camera headCamera =
            player.GetComponentInChildren<Camera>(true);

        if (headCamera != null)
        {
            Vector3 headForward = headCamera.transform.forward;
            headForward.y = 0f;

            Vector3 spawnForward = selected.forward;
            spawnForward.y = 0f;

            if (headForward.sqrMagnitude > 0.001f &&
                spawnForward.sqrMagnitude > 0.001f)
            {
                float angle = Vector3.SignedAngle(
                    headForward,
                    spawnForward,
                    Vector3.up
                );

                player.Rotate(Vector3.up, angle, Space.World);
            }

            // Recalculate after rotation because the
            // headset may be offset from the rig center.
            Vector3 headOffset =
                headCamera.transform.position - player.position;

            headOffset.y = 0f;

            player.position =
                selected.position - headOffset;
        }
        else
        {
            player.SetPositionAndRotation(
                selected.position, selected.rotation);
        }

        if (controllerWasEnabled)
            controller.enabled = true;

        Debug.Log(
            $"ArenaSpawnManager: {player.name} respawned at " +
            selected.name);

        return true;
    }
}
