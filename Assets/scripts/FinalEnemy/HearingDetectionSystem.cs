using UnityEngine;

public class HearingDetectionSystem : MonoBehaviour
{
    [Header("Hearing")]
    [SerializeField] private float hearingRadius = 8f;
    [SerializeField] private float hearingDetectionBoost = 20f;

    private Transform playerTransform;
    private Transform ownerTransform;

    private void OnEnable()
    {
        ownerTransform = transform;
    }

    public void Initialize(Transform player)
    {
        playerTransform = player;
    }

    /// <summary>
    /// Checks if the player is within hearing range
    /// </summary>
    public bool CanHearPlayer()
    {
        if (playerTransform == null) return false;
        float dist = Vector3.Distance(ownerTransform.position, playerTransform.position);
        return dist <= hearingRadius;
    }

    /// <summary>
    /// Gets the detection meter boost value for hearing
    /// </summary>
    public float GetHearingDetectionBoost() => hearingDetectionBoost;

    public float GetHearingRadius() => hearingRadius;

    public void DrawDebugGizmos(Vector3 pos)
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pos, hearingRadius);
    }
}
