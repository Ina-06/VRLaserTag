using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace VRLaserTag.Combat
{
    /// <summary>
    /// Editor-only play-test aiming for the generated CombatTest scene. This is not
    /// used by the production XR rig.
    /// </summary>
    public sealed class CombatTestAimController : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float keyboardDegreesPerSecond = 75f;
        [SerializeField, Min(0.01f)] private float mouseSensitivity = 0.12f;
        [SerializeField] private float minimumPitch = -70f;
        [SerializeField] private float maximumPitch = 70f;

        private float yaw;
        private float pitch;

        private void Awake()
        {
            Vector3 euler = transform.eulerAngles;
            yaw = euler.y;
            pitch = NormalizeAngle(euler.x);
        }

        private void Update()
        {
            Vector2 aimDelta = Vector2.zero;
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;

            if (keyboard != null)
            {
                aimDelta.x += ReadAxis(keyboard.leftArrowKey, keyboard.rightArrowKey);
                aimDelta.y += ReadAxis(keyboard.downArrowKey, keyboard.upArrowKey);
                aimDelta *= keyboardDegreesPerSecond * Time.deltaTime;
            }

            if (mouse != null && mouse.rightButton.isPressed)
            {
                Vector2 mouseDelta = mouse.delta.ReadValue() * mouseSensitivity;
                aimDelta += mouseDelta;
            }

            yaw += aimDelta.x;
            pitch = Mathf.Clamp(pitch - aimDelta.y, minimumPitch, maximumPitch);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private static float ReadAxis(KeyControl negative, KeyControl positive)
        {
            return (positive.isPressed ? 1f : 0f) - (negative.isPressed ? 1f : 0f);
        }

        private static float NormalizeAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }
    }
}
