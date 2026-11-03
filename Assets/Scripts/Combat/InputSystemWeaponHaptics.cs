using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

namespace VRLaserTag.Combat
{
    /// <summary>
    /// Sends a short impulse through the Input System. OpenXR/Meta XR remains owned by
    /// the VR setup; this adapter starts working when that provider exposes a rumble-capable
    /// controller device.
    /// </summary>
    public sealed class InputSystemWeaponHaptics : MonoBehaviour, IWeaponHaptics
    {
        [SerializeField] private bool preferRightHand = true;

        public void Pulse(float amplitude, float durationSeconds)
        {
            XRControllerWithRumble controller = FindController();
            if (controller == null)
            {
                return;
            }

            controller.SendImpulse(
                Mathf.Clamp01(amplitude),
                Mathf.Max(0f, durationSeconds));
        }

        private XRControllerWithRumble FindController()
        {
            IReadOnlyList<InputDevice> devices = InputSystem.devices;
            XRControllerWithRumble fallback = null;

            for (int index = 0; index < devices.Count; index++)
            {
                if (!(devices[index] is XRControllerWithRumble controller))
                {
                    continue;
                }

                fallback ??= controller;
                bool isPreferredHand = false;
                var preferredUsage = preferRightHand
                    ? CommonUsages.RightHand
                    : CommonUsages.LeftHand;

                for (int usageIndex = 0; usageIndex < controller.usages.Count; usageIndex++)
                {
                    if (controller.usages[usageIndex] == preferredUsage)
                    {
                        isPreferredHand = true;
                        break;
                    }
                }

                if (isPreferredHand)
                {
                    return controller;
                }
            }

            return fallback;
        }
    }
}
