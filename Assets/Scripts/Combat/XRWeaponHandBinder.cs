using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace VRLaserTag.Combat
{
    /// <summary>
    /// Attaches the local weapon to the tracked right-hand controller without changing
    /// the arena's XR rig prefab. Network-owned remote weapons should leave this disabled.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public sealed class XRWeaponHandBinder : MonoBehaviour
    {
        [SerializeField] private Transform handAnchor;
        [SerializeField] private string controllerObjectName = "Right Controller";
        [SerializeField] private Vector3 localPosition = new Vector3(0.06f, -0.04f, 0.12f);
        [SerializeField] private Vector3 localEulerAngles = Vector3.zero;
        [SerializeField, Min(0.1f)] private float searchTimeoutSeconds = 10f;
        [SerializeField] private bool disableWeaponInputUntilAttached = true;

        private WeaponInput weaponInput;
        private Coroutine attachRoutine;

        public bool IsAttached { get; private set; }
        public Transform HandAnchor => handAnchor;

        private void Awake()
        {
            weaponInput = GetComponent<WeaponInput>();
            if (disableWeaponInputUntilAttached && weaponInput != null)
            {
                weaponInput.enabled = false;
            }
        }

        private void OnEnable()
        {
            attachRoutine = StartCoroutine(AttachWhenRigIsReady());
        }

        private void OnDisable()
        {
            if (attachRoutine != null)
            {
                StopCoroutine(attachRoutine);
                attachRoutine = null;
            }
        }

        public bool TryAttach()
        {
            if (IsAttached)
            {
                return true;
            }

            if (handAnchor == null)
            {
                handAnchor = FindRightController();
            }

            if (handAnchor == null)
            {
                return false;
            }

            transform.SetParent(handAnchor, false);
            transform.localPosition = localPosition;
            transform.localRotation = Quaternion.Euler(localEulerAngles);
            IsAttached = true;

            if (weaponInput != null)
            {
                weaponInput.enabled = true;
            }

            return true;
        }

        private IEnumerator AttachWhenRigIsReady()
        {
            float deadline = Time.unscaledTime + searchTimeoutSeconds;
            while (Time.unscaledTime <= deadline)
            {
                if (TryAttach())
                {
                    attachRoutine = null;
                    yield break;
                }

                yield return null;
            }

            Debug.LogWarning(
                $"{name} could not find '{controllerObjectName}' under an active XR Origin. " +
                "The weapon remains inactive until a local hand anchor is assigned.",
                this);
            attachRoutine = null;
        }

        private Transform FindRightController()
        {
            XROrigin origin = FindAnyObjectByType<XROrigin>();
            if (origin == null)
            {
                return null;
            }

            Transform[] children = origin.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < children.Length; index++)
            {
                if (children[index].name == controllerObjectName)
                {
                    return children[index];
                }
            }

            return null;
        }

        private void OnValidate()
        {
            searchTimeoutSeconds = Mathf.Max(0.1f, searchTimeoutSeconds);
        }
    }
}
