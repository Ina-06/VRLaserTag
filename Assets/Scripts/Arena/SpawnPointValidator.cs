
using UnityEngine;
using System.Collections.Generic;

public class SpawnPointValidator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform spawnPointsParent;

    [SerializeField]
    private TeleportDestinationValidator validator;

    [Header("Spawn Safety")]
    [SerializeField]
    private int expectedSpawnCount = 5;

    [SerializeField]
    private float minimumSpawnSeparation = 1.5f;

    [SerializeField]
    private float floorHeight = 0f;

    [SerializeField]
    private float floorHeightTolerance = 0.15f;

    [ContextMenu("Validate All Spawn Points")]
    public void ValidateAllSpawnPoints()
    {
        AutoFindReferences();

        if (spawnPointsParent == null)
        {
            Debug.LogError(
                "[Spawn Validation] SpawnPoints object missing."
            );
            return;
        }

        if (validator == null)
        {
            Debug.LogError(
                "[Spawn Validation] Teleport validator missing."
            );
            return;
        }

        List<Transform> spawns =
            new List<Transform>();

        foreach (Transform child in spawnPointsParent)
        {
            if (child.name.StartsWith("Spawn_"))
            {
                spawns.Add(child);
            }
        }

        spawns.Sort(
            (a, b) => string.CompareOrdinal(
                a.name,
                b.name
            )
        );

        int passed = 0;
        int failed = 0;

        if (spawns.Count != expectedSpawnCount)
        {
            Debug.LogError(
                "[Spawn Validation] Expected " +
                expectedSpawnCount +
                " spawn points, found " +
                spawns.Count
            );

            failed++;
        }

        HashSet<string> names =
            new HashSet<string>();

        foreach (Transform spawn in spawns)
        {
            bool valid = true;

            if (!names.Add(spawn.name))
            {
                Debug.LogError(
                    "[Spawn Validation] Duplicate name: " +
                    spawn.name
                );

                valid = false;
            }

            Vector3 position = spawn.position;

            if (Mathf.Abs(
                position.y - floorHeight
            ) > floorHeightTolerance)
            {
                Debug.LogError(
                    "[Spawn Validation] " +
                    spawn.name +
                    " is not at the expected floor height."
                );

                valid = false;
            }

            if (!validator.IsValidDestination(
                position,
                false))
            {
                Debug.LogError(
                    "[Spawn Validation] " +
                    spawn.name +
                    " has insufficient clearance or " +
                    "is outside the arena. Position: " +
                    position
                );

                valid = false;
            }

            foreach (Transform other in spawns)
            {
                if (spawn == other)
                    continue;

                Vector2 a = new Vector2(
                    spawn.position.x,
                    spawn.position.z
                );

                Vector2 b = new Vector2(
                    other.position.x,
                    other.position.z
                );

                float separation =
                    Vector2.Distance(a, b);

                if (separation <
                    minimumSpawnSeparation)
                {
                    Debug.LogError(
                        "[Spawn Validation] " +
                        spawn.name +
                        " is too close to " +
                        other.name
                    );

                    valid = false;
                }
            }

            if (valid)
            {
                Debug.Log(
                    "[PASS] " + spawn.name +
                    " | Position: " + position
                );

                passed++;
            }
            else
            {
                Debug.LogError(
                    "[FAIL] " + spawn.name
                );

                failed++;
            }
        }

        Debug.Log(
            "[Spawn Validation] Complete. " +
            passed + " passed, " +
            failed + " failed."
        );
    }

    private void AutoFindReferences()
    {
        if (spawnPointsParent == null)
        {
            GameObject found =
                GameObject.Find("SpawnPoints");

            if (found != null)
            {
                spawnPointsParent =
                    found.transform;
            }
        }

        if (validator == null)
        {
            validator =
                FindFirstObjectByType<
                    TeleportDestinationValidator
                >();
        }
    }

    private void OnDrawGizmos()
    {
        if (spawnPointsParent == null)
            return;

        Gizmos.color = Color.green;

        foreach (Transform spawn in spawnPointsParent)
        {
            if (!spawn.name.StartsWith("Spawn_"))
                continue;

            Gizmos.DrawWireSphere(
                spawn.position +
                Vector3.up * 0.4f,
                0.4f
            );

            Gizmos.DrawLine(
                spawn.position,
                spawn.position +
                spawn.forward * 1.5f
            );
        }
    }
}
