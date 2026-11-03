
using UnityEngine;

public class TeleportDestinationValidator : MonoBehaviour
{
    [Header("Pentagonal Arena")]
    [SerializeField] private float arenaRadius = 18f;
    [SerializeField] private Vector3 arenaCenter = Vector3.zero;

    [Header("Player Clearance")]
    [SerializeField] private float playerRadius = 0.4f;
    [SerializeField] private float playerHeight = 1.8f;
    [SerializeField] private float minimumClearanceHeight = 1.8f;
    [SerializeField] private float wallMargin = 0.2f;

    [Header("Collision Detection")]
    [SerializeField] private LayerMask obstacleLayers;

    [Header("XR Tracking")]
    [SerializeField] private Transform headTransform;
    [SerializeField] private Transform xrOriginTransform;

    public float PlayerRadius => playerRadius;

    public bool IsValidDestination(Vector3 destination)
    {
        return IsValidDestination(destination, true);
    }

    public bool IsValidDestination(
        Vector3 destination,
        bool logRejection)
    {
        if (!IsInsidePentagon(destination))
        {
            if (logRejection)
                Debug.Log(
                    "Teleport rejected: Outside arena."
                );

            return false;
        }

        if (IsBlocked(destination))
        {
            if (logRejection)
                Debug.Log(
                    "Teleport rejected: Obstacle detected."
                );

            return false;
        }

        return true;
    }

    public bool CanTeleport(Vector3 destination)
    {
        return IsValidDestination(destination);
    }

    private bool IsInsidePentagon(Vector3 point)
    {
        Vector2 p = new Vector2(
            point.x - arenaCenter.x,
            point.z - arenaCenter.z
        );

        float clearance =
            playerRadius + wallMargin;

        for (int i = 0; i < 5; i++)
        {
            float angleA = Mathf.Deg2Rad *
                (90f + i * 72f);

            float angleB = Mathf.Deg2Rad *
                (90f + (i + 1) * 72f);

            Vector2 a = new Vector2(
                arenaRadius * Mathf.Cos(angleA),
                arenaRadius * Mathf.Sin(angleA)
            );

            Vector2 b = new Vector2(
                arenaRadius * Mathf.Cos(angleB),
                arenaRadius * Mathf.Sin(angleB)
            );

            Vector2 edge = b - a;

            Vector2 inward = new Vector2(
                -edge.y,
                edge.x
            ).normalized;

            float distance = Vector2.Dot(
                p - a,
                inward
            );

            if (distance < clearance)
                return false;
        }

        return true;
    }

    private bool IsBlocked(Vector3 destination)
    {
        float height = GetClearanceHeight();

        Vector3 up = Vector3.up;

        Vector3 bottom =
            destination + up * playerRadius;

        Vector3 top =
            destination +
            up * (height - playerRadius);

        Physics.SyncTransforms();

        return Physics.CheckCapsule(
            bottom,
            top,
            playerRadius,
            obstacleLayers,
            QueryTriggerInteraction.Ignore
        );
    }

    private float GetClearanceHeight()
    {
        float height = playerHeight;

        if (Application.isPlaying &&
            headTransform != null &&
            xrOriginTransform != null)
        {
            float trackedHeight =
                headTransform.position.y -
                xrOriginTransform.position.y;

            if (trackedHeight >= 0.3f &&
                trackedHeight <= 2.5f)
            {
                height = trackedHeight;
            }
        }

        return Mathf.Max(
            height,
            minimumClearanceHeight,
            playerRadius * 2f
        );
    }

    // Diagnostic helper:
    // Estimates where the XR Origin should be located
    // if XRI places the headset's floor position at
    // the requested landing point.
    public Vector3 GetPredictedOriginPosition(
        Vector3 landingPoint)
    {
        if (headTransform == null ||
            xrOriginTransform == null)
        {
            return landingPoint;
        }

        Vector3 headOffset =
            headTransform.position -
            xrOriginTransform.position;

        headOffset.y = 0f;

        return landingPoint - headOffset;
    }

    [ContextMenu("Log XR Tracking Diagnostics")]
    public void LogXRTrackingDiagnostics()
    {
        if (headTransform == null ||
            xrOriginTransform == null)
        {
            Debug.LogWarning(
                "XR tracking references are not assigned."
            );
            return;
        }

        Vector3 horizontalOffset =
            headTransform.position -
            xrOriginTransform.position;

        horizontalOffset.y = 0f;

        float distance = horizontalOffset.magnitude;

        Debug.Log(
            "[VR Tracking] Horizontal head offset: " +
            distance.ToString("F2") + " metres"
        );

        Debug.Log(
            "[VR Tracking] Clearance height: " +
            GetClearanceHeight().ToString("F2") +
            " metres"
        );
    }

    [ContextMenu("Run Basic Teleport Tests")]
    public void RunBasicTests()
    {
        CheckTest(
            "Open floor",
            new Vector3(0f, 0f, -4f),
            true
        );

        CheckTest(
            "Outside pentagon",
            new Vector3(25f, 0f, 0f),
            false
        );

        CheckTest(
            "Beyond north corner",
            new Vector3(0f, 0f, 19f),
            false
        );

        CheckTest(
            "Inside fountain",
            Vector3.zero,
            false
        );
    }

    private void CheckTest(
        string name,
        Vector3 position,
        bool expected)
    {
        bool result =
            IsValidDestination(position, false);

        if (result == expected)
        {
            Debug.Log("[PASS] " + name);
        }
        else
        {
            Debug.LogError(
                "[FAIL] " + name +
                " | Expected: " + expected +
                " | Actual: " + result
            );
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        for (int i = 0; i < 5; i++)
        {
            float angleA = Mathf.Deg2Rad *
                (90f + i * 72f);

            float angleB = Mathf.Deg2Rad *
                (90f + (i + 1) * 72f);

            Vector3 a = arenaCenter +
                new Vector3(
                    arenaRadius * Mathf.Cos(angleA),
                    0.05f,
                    arenaRadius * Mathf.Sin(angleA)
                );

            Vector3 b = arenaCenter +
                new Vector3(
                    arenaRadius * Mathf.Cos(angleB),
                    0.05f,
                    arenaRadius * Mathf.Sin(angleB)
                );

            Gizmos.DrawLine(a, b);
        }
    }
}
