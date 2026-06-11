using UnityEngine;

public class HearingDetectionSystem : MonoBehaviour
{
    [Header("Hearing")]
    [SerializeField] private float hearingRadius = 14f;
    [SerializeField] private float minimumSoundThreshold = 0.05f;
    [SerializeField] private float hearingBoost = 30f;

    [Header("Occlusion")]
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float wallMultiplier = 0.65f;

    private Transform ownerTransform;

    private void OnEnable()
    {
        ownerTransform = transform;
    }

    public float CalculateStimulus(Vector3 sourcePosition, float volume01)
    {
        if (volume01 < minimumSoundThreshold)
            return 0f;

        float distance = Vector3.Distance(ownerTransform.position, sourcePosition);
        if (distance > hearingRadius)
            return 0f;

        float distance01 = 1f - Mathf.Clamp01(distance / hearingRadius);
        float occlusion = CalculateOcclusion(sourcePosition);

        float result = volume01 * distance01 * occlusion * hearingBoost;
        return Mathf.Max(0f, result);
    }

    private float CalculateOcclusion(Vector3 source)
    {
        Vector3 target = ownerTransform.position;

        if (Physics.Linecast(
            source,
            target,
            wallLayer,
            QueryTriggerInteraction.Ignore))
        {
            return wallMultiplier;
        }

        return 1f;
    }
}