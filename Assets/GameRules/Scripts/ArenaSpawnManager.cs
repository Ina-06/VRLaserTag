
using UnityEngine;

public class ArenaSpawnManager : MonoBehaviour
{
    [Header("Arena Spawn Locations")]
    [SerializeField] private Transform[] spawnPoints;

    private int nextSpawnIndex = 0;

    public void RespawnPlayer(Transform player)
    {
        if (player == null || spawnPoints == null ||
            spawnPoints.Length == 0)
        {
            Debug.LogWarning(
                "ArenaSpawnManager: Missing player or spawn points."
            );
            return;
        }

        Transform spawn = null;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            int index =
                (nextSpawnIndex + i) % spawnPoints.Length;

            if (spawnPoints[index] != null)
            {
                spawn = spawnPoints[index];
                nextSpawnIndex =
                    (index + 1) % spawnPoints.Length;
                break;
            }
        }

        if (spawn == null)
        {
            Debug.LogWarning(
                "ArenaSpawnManager: No valid spawn points."
            );
            return;
        }

        // Disable CharacterController temporarily
        // so it does not block repositioning.
        CharacterController controller =
            player.GetComponent<CharacterController>();

        bool wasEnabled =
            controller != null && controller.enabled;

        if (wasEnabled)
            controller.enabled = false;

        // For XR rigs, preserve the tracked headset's
        // horizontal offset from the rig root.
        Camera playerCamera =
            player.GetComponentInChildren<Camera>(true);

        if (playerCamera != null)
        {
            Vector3 cameraOffset =
                playerCamera.transform.position -
                player.position;

            float horizontalOffsetX = cameraOffset.x;
            float horizontalOffsetZ = cameraOffset.z;

            player.position = new Vector3(
                spawn.position.x - horizontalOffsetX,
                spawn.position.y,
                spawn.position.z - horizontalOffsetZ
            );
        }
        else
        {
            player.position = spawn.position;
        }

        // Align heading with the chosen spawn.
        // Headset-relative heading will be calibrated
        // during actual Quest testing.
        player.rotation = spawn.rotation;

        if (wasEnabled)
            controller.enabled = true;

        Debug.Log(
            $"{player.name} moved to spawn {spawn.name}"
        );
    }
}
