using UnityEngine;

public class HearingDetectionSystem : MonoBehaviour
{
    [Header("Hearing")]
    [SerializeField] private float hearingRadius = 8f;
    [SerializeField] private float hearingDetectionBoost = 20f;

    [Header("Occlusion")]
    [SerializeField] private LayerMask wallLayer;
    [SerializeField, Range(0.1f, 1f)] private float wallAttenuationMultiplier = 0.7f;

    private Transform ownerTransform;

    private void OnEnable()
    {
        ownerTransform = transform;
    }

    /// <summary>
    /// Converts a sound event into hidden detection value.
    /// volume01 is expected to be 0..1.
    /// </summary>
    public float CalculateStimulus(Vector3 sourcePosition, float volume01)
    {
        if (ownerTransform == null) return 0f;

        float distance = Vector3.Distance(ownerTransform.position, sourcePosition);
        if (distance > hearingRadius)
            return 0f;

        float proximityFactor = 1f - Mathf.Clamp01(distance / hearingRadius);
        float vol = Mathf.Max(0f, volume01);
        float wallMultiplier = GetWallMultiplier(sourcePosition);

        return vol * proximityFactor * wallMultiplier * hearingDetectionBoost;
    }

    private float GetWallMultiplier(Vector3 sourcePosition)
    {
        if (wallLayer.value == 0)
            return 1f;

        Vector3 targetPosition = ownerTransform.position;
        Vector3 direction = targetPosition - sourcePosition;
        float distance = direction.magnitude;

        if (distance <= 0.0001f)
            return 1f;

        direction /= distance;

        RaycastHit[] hits = Physics.RaycastAll(
            sourcePosition,
            direction,
            distance,
            wallLayer,
            QueryTriggerInteraction.Ignore);

        if (hits == null || hits.Length == 0)
            return 1f;

        float multiplier = 1f;
        for (int i = 0; i < hits.Length; i++)
            multiplier *= wallAttenuationMultiplier;

        return multiplier;
    }

    public float GetHearingRadius() => hearingRadius;
    public float GetHearingDetectionBoost() => hearingDetectionBoost;

    public void DrawDebugGizmos(Vector3 pos)
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pos, hearingRadius);
    }
}