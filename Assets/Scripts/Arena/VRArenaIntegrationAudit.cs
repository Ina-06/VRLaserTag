
using UnityEngine;

public class VRArenaIntegrationAudit : MonoBehaviour
{
    [Header("Optional References")]
    [SerializeField]
    private PentagonArena pentagonArena;

    [SerializeField]
    private TeleportDestinationValidator validator;

    [SerializeField]
    private SafeTeleportationArea safeTeleportArea;

    [SerializeField]
    private Transform xrRig;

    [SerializeField]
    private Transform spawnPoints;

    [SerializeField]
    private Transform arenaCover;

    [ContextMenu("Run Full VR Arena Audit")]
    public void RunAudit()
    {
        AutoFindReferences();

        int passed = 0;
        int failed = 0;

        Debug.Log(
            "========== VR ARENA AUDIT =========="
        );

        Check(
            "Pentagon arena exists",
            pentagonArena != null,
            ref passed,
            ref failed
        );

        if (pentagonArena != null)
        {
            Check(
                "Arena radius is 18",
                Mathf.Approximately(
                    pentagonArena.radius,
                    18f
                ),
                ref passed,
                ref failed
            );

            MeshFilter meshFilter =
                pentagonArena.GetComponent<MeshFilter>();

            MeshCollider meshCollider =
                pentagonArena.GetComponent<MeshCollider>();

            Check(
                "Floor mesh exists",
                meshFilter != null &&
                meshFilter.sharedMesh != null,
                ref passed,
                ref failed
            );

            Check(
                "Floor collider enabled",
                meshCollider != null &&
                meshCollider.enabled &&
                meshCollider.sharedMesh != null,
                ref passed,
                ref failed
            );

            Check(
                "Floor and collider use same mesh",
                meshFilter != null &&
                meshCollider != null &&
                meshFilter.sharedMesh ==
                    meshCollider.sharedMesh,
                ref passed,
                ref failed
            );

            int wallCount = 0;

            foreach (Transform child
                in pentagonArena.transform)
            {
                if (child.name.StartsWith("Wall_"))
                {
                    wallCount++;
                }
            }

            Check(
                "Five boundary walls exist",
                wallCount == 5,
                ref passed,
                ref failed
            );
        }

        Check(
            "Teleport validator exists",
            validator != null,
            ref passed,
            ref failed
        );

        Check(
            "Safe teleport area exists",
            safeTeleportArea != null,
            ref passed,
            ref failed
        );

        Check(
            "Safe teleport area enabled",
            safeTeleportArea != null &&
            safeTeleportArea.enabled,
            ref passed,
            ref failed
        );

        Check(
            "XR Rig exists",
            xrRig != null,
            ref passed,
            ref failed
        );

        Camera[] cameras =
            FindObjectsByType<Camera>(
                FindObjectsSortMode.None
            );

        int activeMainCameras = 0;

        foreach (Camera camera in cameras)
        {
            if (camera != null &&
                camera.enabled &&
                camera.gameObject.activeInHierarchy &&
                camera.CompareTag("MainCamera"))
            {
                activeMainCameras++;
            }
        }

        Check(
            "Exactly one active MainCamera",
            activeMainCameras == 1,
            ref passed,
            ref failed
        );

        Check(
            "SpawnPoints exists",
            spawnPoints != null,
            ref passed,
            ref failed
        );

        if (spawnPoints != null)
        {
            int spawnCount = 0;

            foreach (Transform child in spawnPoints)
            {
                if (child.name.StartsWith("Spawn_"))
                {
                    spawnCount++;
                }
            }

            Check(
                "Exactly five spawn points",
                spawnCount == 5,
                ref passed,
                ref failed
            );
        }

        Check(
            "ArenaCover exists",
            arenaCover != null,
            ref passed,
            ref failed
        );

        int obstacleLayer =
            LayerMask.NameToLayer(
                "TeleportObstacle"
            );

        int surfaceLayer =
            LayerMask.NameToLayer(
                "TeleportSurface"
            );

        Check(
            "TeleportObstacle physics layer exists",
            obstacleLayer >= 0,
            ref passed,
            ref failed
        );

        Check(
            "TeleportSurface physics layer exists",
            surfaceLayer >= 0,
            ref passed,
            ref failed
        );

        if (pentagonArena != null &&
            surfaceLayer >= 0)
        {
            Check(
                "Floor uses TeleportSurface layer",
                pentagonArena.gameObject.layer ==
                    surfaceLayer,
                ref passed,
                ref failed
            );
        }

        if (pentagonArena != null &&
            safeTeleportArea != null)
        {
            Check(
                "Safe teleport component is on floor",
                safeTeleportArea.gameObject ==
                    pentagonArena.gameObject,
                ref passed,
                ref failed
            );
        }

        Debug.Log(
            "========== AUDIT RESULT =========="
        );

        Debug.Log(
            "Passed: " + passed
        );

        Debug.Log(
            "Failed: " + failed
        );

        if (failed == 0)
        {
            Debug.Log(
                "[AUDIT] Structural checks passed. " +
                "Hardware testing is still required."
            );
        }
        else
        {
            Debug.LogWarning(
                "[AUDIT] Some checks failed. " +
                "Review the Console."
            );
        }
    }

    private void Check(
        string name,
        bool success,
        ref int passed,
        ref int failed)
    {
        if (success)
        {
            Debug.Log("[PASS] " + name);
            passed++;
        }
        else
        {
            Debug.LogError("[FAIL] " + name);
            failed++;
        }
    }

    private void AutoFindReferences()
    {
        if (pentagonArena == null)
        {
            pentagonArena =
                FindFirstObjectByType<
                    PentagonArena
                >();
        }

        if (validator == null)
        {
            validator =
                FindFirstObjectByType<
                    TeleportDestinationValidator
                >();
        }

        if (safeTeleportArea == null)
        {
            safeTeleportArea =
                FindFirstObjectByType<
                    SafeTeleportationArea
                >();
        }

        if (xrRig == null)
        {
            GameObject found =
                GameObject.Find("XR Origin (XR Rig)");

            if (found != null)
                xrRig = found.transform;
        }

        if (spawnPoints == null)
        {
            GameObject found =
                GameObject.Find("SpawnPoints");

            if (found != null)
                spawnPoints = found.transform;
        }

        if (arenaCover == null)
        {
            GameObject found =
                GameObject.Find("ArenaCover");

            if (found != null)
                arenaCover = found.transform;
        }
    }
}
