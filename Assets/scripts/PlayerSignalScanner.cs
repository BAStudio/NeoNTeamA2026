using UnityEngine;

public class PlayerSignalScanner : MonoBehaviour
{
    [Header("Signal Emission")]
    [SerializeField] private float maxRadius = 12f;
    [SerializeField] private float minRadius = 4f;
    [SerializeField] private float expansionSpeed = 20f;
    [SerializeField] private float baseStrength = 1f;

    [Header("Velocity")]
    [SerializeField] private float velocityThreshold = 2f;
    [SerializeField] private float maxSpeedForFullSignal = 5f;
    [SerializeField] private float velocitySmoothing = 0.5f;
    [SerializeField, Range(0f, 1f)] private float radiusSpeedInfluence = 0.3f;

    [Header("Raycast Settings")]
    [SerializeField, Range(8, 128)] private int rayCount = 48;
    [SerializeField] private LayerMask detectionLayer;
    [SerializeField] private bool drawDebugRays = true;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;

    private Vector3 lastFramePosition;
    private Vector3 smoothedVelocity;

    private bool isEmitting;
    private float currentRadius;
    private float waveStrength;
    private float waveMaxRadius;
    private Vector3 waveOrigin;

    private void Start()
    {
        lastFramePosition = transform.position;
    }

    private void Update()
    {
        UpdateVelocity();
        HandleWave();
    }

    #region Velocity
    private void UpdateVelocity()
    {
        Vector3 rawVelocity = (transform.position - lastFramePosition) / Mathf.Max(Time.deltaTime, 0.0001f);
        smoothedVelocity = Vector3.Lerp(smoothedVelocity, rawVelocity, velocitySmoothing);
        lastFramePosition = transform.position;
    }

    private float GetXZSpeed()
    {
        return new Vector2(smoothedVelocity.x, smoothedVelocity.z).magnitude;
    }
    #endregion

    #region Wave Logic
    private void HandleWave()
    {
        float speed = GetXZSpeed();

        // Only start wave if speed is above threshold and no wave is active
        if (!isEmitting && speed >= velocityThreshold)
        {
            StartWave(speed);
        }

        // expand wave
        if (isEmitting)
        {
            float previousRadius = currentRadius;
            currentRadius += expansionSpeed * Time.deltaTime;

            CastWaveRing(previousRadius, currentRadius);

            if (currentRadius >= waveMaxRadius)
            {
                isEmitting = false;
            }
        }
    }

    private void StartWave(float speed)
    {
        float strengthMultiplier = Mathf.Clamp01(speed / maxSpeedForFullSignal);
        waveStrength = baseStrength * strengthMultiplier;

        float normalizedSpeed = Mathf.Clamp01(speed / maxSpeedForFullSignal);
        float adjustedMultiplier = Mathf.Pow(normalizedSpeed, 2f);
        waveMaxRadius = minRadius + (maxRadius - minRadius) * adjustedMultiplier;

        waveOrigin = transform.position;
        currentRadius = 0f;
        isEmitting = true;
    }

    private void CastWaveRing(float innerRadius, float outerRadius)
    {
        // Avoid zero radius problems
        innerRadius = Mathf.Max(0f, innerRadius);

        float angleStep = 360f / Mathf.Max(1, rayCount);

        for (int i = 0; i < rayCount; i++)
        {
            float angle = i * angleStep;
            Vector3 dir = DirFromAngle(angle);

            if (Physics.Raycast(waveOrigin, dir, out RaycastHit hit, outerRadius, detectionLayer))
            {
                float dist = hit.distance;

                if (dist >= innerRadius && dist <= outerRadius)
                {
                    // normalized relative to max wave radius (0..1)
                    float normalized = Mathf.Clamp01(dist / Mathf.Max(0.0001f, waveMaxRadius));

                    // smoother falloff (you can tweak this)
                    float strength = waveStrength * (1f - normalized * normalized);

                    OnSignalHit(hit, waveOrigin, Mathf.Clamp01(strength));

                    if (drawDebugRays)
                        Debug.DrawRay(waveOrigin, dir * dist, Color.cyan, 0.08f);
                }
            }
            else if (drawDebugRays)
            {
                Debug.DrawRay(waveOrigin, dir * outerRadius, Color.gray, 0.05f);
            }
        }
    }

    private void OnSignalHit(RaycastHit hit, Vector3 source, float strength)
    {
        ISignalReceiver receiver = null;

        receiver = hit.collider.GetComponent<ISignalReceiver>();
        if (receiver == null)
            receiver = hit.collider.GetComponentInParent<ISignalReceiver>();
        if (receiver == null)
            receiver = hit.collider.GetComponentInChildren<ISignalReceiver>();

        if (receiver != null)
        {
            receiver.OnSignalReceived(source, strength);
        }
    }
    #endregion

    #region Utility
    private Vector3 DirFromAngle(float angle)
    {
        float rad = angle * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
    }
    #endregion

    #region Debug
    private void OnDrawGizmos()
    {
        if (!drawGizmos || !isEmitting) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(waveOrigin, currentRadius);
    }
    #endregion
}