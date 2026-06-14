using UnityEngine;

public class FootstepAudioGenerator : MonoBehaviour
{
    [Header("Clip")]
    [SerializeField] private AudioClip footstepClip;

    [Header("Movement")]
    [SerializeField] private float minMoveSpeed = 0.15f;
    [SerializeField] private bool horizontalOnly = true;

    [Header("Cadence")]
    [SerializeField] private float maxStrideDistance = 1.25f;
    [SerializeField] private float minStrideDistance = 0.50f;
    [SerializeField] private float firstStepEarlyFactor = 0.45f;
    [SerializeField] private float strideJitter = 0.08f;

    [Header("Speed Mapping")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float speedSmoothing = 12f;

    [Header("Tone")]
    [SerializeField] private float minPitch = 0.92f;
    [SerializeField] private float maxPitch = 1.18f;
    [SerializeField] private float pitchJitter = 0.04f;
    [SerializeField] private float alternatingPitchBias = 0.03f;

    [Header("Volume")]
    [SerializeField] private float minVolume = 0.35f;
    [SerializeField] private float maxVolume = 1f;
    [SerializeField] private float volumeJitter = 0.06f;
    [SerializeField] private float alternatingVolumeBias = 0.03f;

    [Header("Overlap")]
    [SerializeField, Range(1, 2)] private int maxConcurrentSources = 2;

    private AudioSource[] sources;
    private int nextSourceIndex;

    private Vector3 lastPosition;
    private float smoothedSpeed;
    private float distanceAccumulated;
    private bool wasMoving;
    private bool stepSideToggle;

    private void Awake()
    {
        sources = GetComponents<AudioSource>();

        while (sources.Length < maxConcurrentSources)
        {
            gameObject.AddComponent<AudioSource>();
            sources = GetComponents<AudioSource>();
        }

        for (int i = 0; i < sources.Length; i++)
        {
            sources[i].playOnAwake = false;
            sources[i].loop = false;
            sources[i].spatialBlend = 1f;
        }

        lastPosition = transform.position;
    }

    private void Update()
    {
        Vector3 delta = transform.position - lastPosition;
        lastPosition = transform.position;

        if (horizontalOnly)
            delta.y = 0f;

        float frameDistance = delta.magnitude;
        float instantSpeed = frameDistance / Mathf.Max(Time.deltaTime, 0.0001f);

        float smoothing = 1f - Mathf.Exp(-speedSmoothing * Time.deltaTime);
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, instantSpeed, smoothing);

        if (smoothedSpeed < minMoveSpeed)
        {
            wasMoving = false;
            distanceAccumulated = 0f;
            return;
        }

        float speed01 = Mathf.InverseLerp(walkSpeed, runSpeed, smoothedSpeed);
        float strideDistance = GetStrideDistance(speed01);

        if (!wasMoving)
        {
            distanceAccumulated = strideDistance * firstStepEarlyFactor;
            wasMoving = true;
        }

        distanceAccumulated += frameDistance;

        while (distanceAccumulated >= strideDistance)
        {
            distanceAccumulated -= strideDistance;
            PlayFootstep(speed01);

            strideDistance = GetStrideDistance(speed01);
        }
    }

    private float GetStrideDistance(float speed01)
    {
        float cadence = Mathf.SmoothStep(0f, 1f, speed01);
        float stride = Mathf.Lerp(maxStrideDistance, minStrideDistance, cadence);

        float jitter = Random.Range(-strideJitter, strideJitter);
        stride *= 1f + jitter;

        return Mathf.Max(0.05f, stride);
    }

    private void PlayFootstep(float speed01)
    {
        if (footstepClip == null || sources == null || sources.Length == 0)
            return;

        AudioSource source = sources[nextSourceIndex];
        nextSourceIndex = (nextSourceIndex + 1) % sources.Length;

        if (source.isPlaying)
            source.Stop();

        stepSideToggle = !stepSideToggle;
        float side = stepSideToggle ? 1f : -1f;

        float pitch = Mathf.Lerp(minPitch, maxPitch, speed01);
        pitch += Random.Range(-pitchJitter, pitchJitter);
        pitch += side * alternatingPitchBias;

        float volume = Mathf.Lerp(minVolume, maxVolume, speed01);
        volume += Random.Range(-volumeJitter, volumeJitter);
        volume += side * alternatingVolumeBias;

        source.clip = footstepClip;
        source.pitch = Mathf.Clamp(pitch, 0.5f, 2f);
        source.volume = Mathf.Clamp01(volume);
        source.Play();
    }
}