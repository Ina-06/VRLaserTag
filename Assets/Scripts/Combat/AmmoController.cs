using System;
using System.Collections;
using UnityEngine;

namespace VRLaserTag.Combat
{
    public sealed class AmmoController : MonoBehaviour
    {
        [SerializeField, Min(1)] private int magazineCapacity = 10;
        [SerializeField, Min(0f)] private float reloadDuration = 1.5f;
        [SerializeField] private bool autoReloadWhenEmpty = true;

        private Coroutine reloadRoutine;

        public int CurrentAmmo { get; private set; }
        public int MagazineCapacity => magazineCapacity;
        public bool IsReloading => reloadRoutine != null;

        public event Action<int, int> AmmoChanged;
        public event Action<bool> ReloadStateChanged;

        private void Awake()
        {
            CurrentAmmo = magazineCapacity;
        }

        private void OnDisable()
        {
            CancelReload();
        }

        public bool TryConsumeRound()
        {
            if (IsReloading || CurrentAmmo <= 0)
            {
                if (autoReloadWhenEmpty && CurrentAmmo <= 0)
                {
                    BeginReload();
                }

                return false;
            }

            CurrentAmmo--;
            AmmoChanged?.Invoke(CurrentAmmo, magazineCapacity);

            if (autoReloadWhenEmpty && CurrentAmmo == 0)
            {
                BeginReload();
            }

            return true;
        }

        public bool BeginReload()
        {
            if (!isActiveAndEnabled || IsReloading || CurrentAmmo >= magazineCapacity)
            {
                return false;
            }

            reloadRoutine = StartCoroutine(ReloadAfterDelay());
            ReloadStateChanged?.Invoke(true);
            return true;
        }

        public void RestoreRejectedRound()
        {
            CurrentAmmo = Mathf.Min(CurrentAmmo + 1, magazineCapacity);
            AmmoChanged?.Invoke(CurrentAmmo, magazineCapacity);
        }

        public void SetAuthoritativeAmmo(int amount)
        {
            CurrentAmmo = Mathf.Clamp(amount, 0, magazineCapacity);
            AmmoChanged?.Invoke(CurrentAmmo, magazineCapacity);
        }

        public void CancelReload()
        {
            if (reloadRoutine == null)
            {
                return;
            }

            StopCoroutine(reloadRoutine);
            reloadRoutine = null;
            ReloadStateChanged?.Invoke(false);
        }

        private IEnumerator ReloadAfterDelay()
        {
            if (reloadDuration > 0f)
            {
                yield return new WaitForSeconds(reloadDuration);
            }

            CurrentAmmo = magazineCapacity;
            reloadRoutine = null;
            AmmoChanged?.Invoke(CurrentAmmo, magazineCapacity);
            ReloadStateChanged?.Invoke(false);
        }

        private void OnValidate()
        {
            magazineCapacity = Mathf.Max(1, magazineCapacity);
            reloadDuration = Mathf.Max(0f, reloadDuration);
        }
    }
}
