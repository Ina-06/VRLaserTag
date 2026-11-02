using UnityEngine;
using UnityEngine.InputSystem;

namespace VRLaserTag.Combat
{
    public sealed class WeaponInput : MonoBehaviour
    {
        [SerializeField] private LaserWeapon weapon;
        [SerializeField] private InputActionReference fireAction;
        [SerializeField] private InputActionReference reloadAction;

        private InputAction fallbackFireAction;
        private InputAction fallbackReloadAction;

        private void Awake()
        {
            if (weapon == null)
            {
                weapon = GetComponent<LaserWeapon>();
            }
        }

        private void OnEnable()
        {
            if (fireAction != null)
            {
                Subscribe(fireAction.action, OnFirePerformed);
            }
            else
            {
                fallbackFireAction = CreateFallbackFireAction();
                Subscribe(fallbackFireAction, OnFirePerformed);
            }

            if (reloadAction != null)
            {
                Subscribe(reloadAction.action, OnReloadPerformed);
            }
            else
            {
                fallbackReloadAction = CreateFallbackReloadAction();
                Subscribe(fallbackReloadAction, OnReloadPerformed);
            }
        }

        private void OnDisable()
        {
            Unsubscribe(
                fireAction != null ? fireAction.action : fallbackFireAction,
                OnFirePerformed);
            Unsubscribe(
                reloadAction != null ? reloadAction.action : fallbackReloadAction,
                OnReloadPerformed);

            fallbackFireAction?.Dispose();
            fallbackReloadAction?.Dispose();
            fallbackFireAction = null;
            fallbackReloadAction = null;
        }

        private static void Subscribe(
            InputAction action,
            System.Action<InputAction.CallbackContext> callback)
        {
            if (action == null)
            {
                return;
            }

            action.performed += callback;
            action.Enable();
        }

        private static void Unsubscribe(
            InputAction action,
            System.Action<InputAction.CallbackContext> callback)
        {
            if (action == null)
            {
                return;
            }

            action.performed -= callback;
            action.Disable();
        }

        private void OnFirePerformed(InputAction.CallbackContext context)
        {
            weapon?.TryFire();
        }

        private void OnReloadPerformed(InputAction.CallbackContext context)
        {
            weapon?.RequestReload();
        }

        private static InputAction CreateFallbackFireAction()
        {
            InputAction action = new InputAction("Combat Fire", InputActionType.Button);
            action.AddBinding("<Mouse>/leftButton");
            action.AddBinding("<Keyboard>/space");
            action.AddBinding("<XRController>{RightHand}/triggerPressed");
            return action;
        }

        private static InputAction CreateFallbackReloadAction()
        {
            InputAction action = new InputAction("Combat Reload", InputActionType.Button);
            action.AddBinding("<Keyboard>/r");
            action.AddBinding("<XRController>{RightHand}/secondaryButton");
            return action;
        }
    }
}
