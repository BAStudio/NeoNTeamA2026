using System;
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

    [Header("Raycast Settings")]
    [SerializeField, Range(8, 128)] private int rayCount = 48;
    [SerializeField] private LayerMask detectionLayer;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField, Range(0.1f, 1f)] private float wallAttenuationMultiplier = 0.7f;
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

            RaycastHit[] hits = Physics.RaycastAll(
                waveOrigin,
                dir,
                outerRadius,
                ~0,
                QueryTriggerInteraction.Ignore);

            if (hits == null || hits.Length == 0)
            {
                Debug.DrawRay(waveOrigin, dir * outerRadius, Color.gray, 0.05f);
                continue;
            }

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            int wallCount = 0;
            bool drewAnyHit = false;

            for (int hitIndex = 0; hitIndex < hits.Length; hitIndex++)
            {
                RaycastHit hit = hits[hitIndex];
                float dist = hit.distance;

                if (dist < innerRadius || dist > outerRadius)
                    continue;

                if (IsWall(hit.collider))
                {
                    wallCount++;
                    drewAnyHit = true;

                    if (drawDebugRays)
                        Debug.DrawRay(waveOrigin, dir * dist, Color.gray, 0.05f);

                    continue;
                }

                ISignalReceiver receiver = FindReceiver(hit.collider);
                if (receiver == null)
                    continue;

                float normalized = Mathf.Clamp01(dist / Mathf.Max(0.0001f, waveMaxRadius));
                float strength = waveStrength * (1f - normalized * normalized);

                for (int wallIndex = 0; wallIndex < wallCount; wallIndex++)
                    strength *= wallAttenuationMultiplier;

                strength = Mathf.Clamp01(strength);

                receiver.OnSignalReceived(waveOrigin, strength);

                drewAnyHit = true;

                if (drawDebugRays)
                    Debug.DrawRay(waveOrigin, dir * dist, Color.cyan, 0.08f);
            }

            if (!drewAnyHit && drawDebugRays)
                Debug.DrawRay(waveOrigin, dir * outerRadius, Color.gray, 0.05f);
        }
    }

    private ISignalReceiver FindReceiver(Collider hitCollider)
    {
        if (hitCollider == null)
            return null;

        ISignalReceiver receiver = hitCollider.GetComponent<ISignalReceiver>();
        if (receiver == null)
            receiver = hitCollider.GetComponentInParent<ISignalReceiver>(true);
        if (receiver == null)
            receiver = hitCollider.GetComponentInChildren<ISignalReceiver>(true);
        if (receiver == null && hitCollider.transform.root != null)
            receiver = hitCollider.transform.root.GetComponentInChildren<ISignalReceiver>(true);

        return receiver;
    }

    private bool IsWall(Collider hitCollider)
    {
        if (hitCollider == null)
            return false;

        return ((1 << hitCollider.gameObject.layer) & wallLayer.value) != 0;
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