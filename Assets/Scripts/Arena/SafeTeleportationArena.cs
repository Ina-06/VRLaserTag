
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public class SafeTeleportationArea : TeleportationArea
{
    [Header("Destination Validation")]
    [SerializeField]
    private TeleportDestinationValidator validator;

    protected override bool GenerateTeleportRequest(
        IXRInteractor interactor,
        RaycastHit raycastHit,
        ref TeleportRequest teleportRequest)
    {
        // Reject missing collision information.
        if (raycastHit.collider == null)
        {
            return false;
        }

        // Reject teleportation if validator is missing.
        if (validator == null)
        {
            Debug.LogWarning(
                "SafeTeleportationArea: Validator not assigned."
            );

            return false;
        }

        Vector3 destination = raycastHit.point;

        // Check pentagon boundaries and obstacles.
        if (!validator.IsValidDestination(destination))
        {
            Debug.Log(
                "Teleport rejected: Destination is blocked."
            );

            return false;
        }

        // Destination passed our checks.
        // Allow XR Interaction Toolkit to generate the request.
        return base.GenerateTeleportRequest(
            interactor,
            raycastHit,
            ref teleportRequest
        );
    }
}
