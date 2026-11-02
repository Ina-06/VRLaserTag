using System.Collections;
using UnityEngine;

namespace VRLaserTag.Combat
{
    public sealed class WeaponEffects : MonoBehaviour
    {
        [SerializeField] private ParticleSystem muzzleFlash;
        [SerializeField] private LineRenderer tracer;
        [SerializeField, Min(0.01f)] private float tracerDuration = 0.06f;
        [SerializeField] private GameObject impactPrefab;
        [SerializeField, Min(0f)] private float impactLifetime = 1.5f;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip fireClip;
        [SerializeField] private AudioClip dryFireClip;
        [Tooltip("Assign a MonoBehaviour that implements IWeaponHaptics.")]
        [SerializeField] private MonoBehaviour hapticsSource;
        [SerializeField, Range(0f, 1f)] private float hapticAmplitude = 0.35f;
        [SerializeField, Min(0f)] private float hapticDuration = 0.06f;

        private IWeaponHaptics haptics;
        private Coroutine tracerRoutine;

        private void Awake()
        {
            haptics = hapticsSource as IWeaponHaptics;
            if (tracer != null)
            {
                tracer.enabled = false;
            }
        }

        public void PlayShot(Vector3 origin, ShotResult result)
        {
            muzzleFlash?.Play();
            PlayClip(fireClip);
            haptics?.Pulse(hapticAmplitude, hapticDuration);

            if (tracer != null)
            {
                tracer.positionCount = 2;
                tracer.SetPosition(0, origin);
                tracer.SetPosition(1, result.EndPoint);

                if (tracerRoutine != null)
                {
                    StopCoroutine(tracerRoutine);
                }

                tracerRoutine = StartCoroutine(ShowTracerBriefly());
            }

            if (result.Hit && impactPrefab != null)
            {
                Quaternion rotation = result.SurfaceNormal.sqrMagnitude > 0f
                    ? Quaternion.LookRotation(result.SurfaceNormal)
                    : Quaternion.identity;
                GameObject impact = Instantiate(impactPrefab, result.EndPoint, rotation);
                Destroy(impact, impactLifetime);
            }
        }

        public void PlayDryFire()
        {
            PlayClip(dryFireClip);
        }

        private void PlayClip(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private IEnumerator ShowTracerBriefly()
        {
            tracer.enabled = true;
            yield return new WaitForSeconds(tracerDuration);
            tracer.enabled = false;
            tracerRoutine = null;
        }

        private void OnValidate()
        {
            tracerDuration = Mathf.Max(0.01f, tracerDuration);
            impactLifetime = Mathf.Max(0f, impactLifetime);
            hapticDuration = Mathf.Max(0f, hapticDuration);
        }
    }
}
