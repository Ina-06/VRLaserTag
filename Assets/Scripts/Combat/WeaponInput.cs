using UnityEngine;
using UnityEngine.InputSystem;

namespace VRLaserTag.Combat
{
    public sealed class WeaponInput : MonoBehaviour
    {
        [SerializeField] private LaserWeapon weapon;
        [SerializeField] private InputActionReference fireAction;
        [SerializeField] private InputActionReference reloadAction;

        private void Awake()
        {
            if (weapon == null)
            {
                weapon = GetComponent<LaserWeapon>();
            }
        }

        private void OnEnable()
        {
            Subscribe(fireAction, OnFirePerformed);
            Subscribe(reloadAction, OnReloadPerformed);
        }

        private void OnDisable()
        {
            Unsubscribe(fireAction, OnFirePerformed);
            Unsubscribe(reloadAction, OnReloadPerformed);
        }

        private static void Subscribe(
            InputActionReference reference,
            System.Action<InputAction.CallbackContext> callback)
        {
            if (reference == null)
            {
                return;
            }

            reference.action.performed += callback;
            reference.action.Enable();
        }

        private static void Unsubscribe(
            InputActionReference reference,
            System.Action<InputAction.CallbackContext> callback)
        {
            if (reference == null)
            {
                return;
            }

            reference.action.performed -= callback;
            reference.action.Disable();
        }

        private void OnFirePerformed(InputAction.CallbackContext context)
        {
            weapon?.TryFire();
        }

        private void OnReloadPerformed(InputAction.CallbackContext context)
        {
            weapon?.RequestReload();
        }
    }
}
