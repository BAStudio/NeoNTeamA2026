using UnityEngine;

public class VisionDetectionSystem : MonoBehaviour
{
    [Header("Vision / FOV")]
    [SerializeField] private float viewRadius = 14f;
    [SerializeField, Range(0, 360)] private float viewAngle = 85f;
    [SerializeField] private float eyeHeight = 1.6f;

    [Header("Multi-ray detection")]
    [SerializeField, Range(1, 11)] private int rayCount = 5;
    [SerializeField] private bool drawVisionRays = true;

    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private string playerTag = "Player";

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
    /// Casts vision rays and counts how many hit the player.
    /// Useful for cameras or other watcher objects.
    /// </summary>
    public int CastVisionRaysAndCountHits(Vector3 lookDirection = default)
    {
        int hits = 0;
        Vector3 eyePos = ownerTransform.position + Vector3.up * eyeHeight;
        Vector3 centerDir = lookDirection.sqrMagnitude <= 0.0001f ? ownerTransform.forward : lookDirection.normalized;

        float baseAngle = Mathf.Atan2(centerDir.x, centerDir.z) * Mathf.Rad2Deg;

        for (int i = 0; i < rayCount; i++)
        {
            float t = (rayCount == 1) ? 0.5f : (float)i / (rayCount - 1);
            float angle = Mathf.Lerp(-viewAngle / 2f, viewAngle / 2f, t);
            Vector3 dir = DirFromAngle(baseAngle + angle);

            if (Physics.Raycast(eyePos, dir, out RaycastHit hit, viewRadius))
            {
                if (((1 << hit.collider.gameObject.layer) & wallLayer) != 0)
                {
                    if (drawVisionRays) Debug.DrawRay(eyePos, dir * hit.distance, Color.red);
                    continue;
                }

                bool hitPlayer = IsHitPlayer(hit);
                if (drawVisionRays) Debug.DrawRay(eyePos, dir * hit.distance, hitPlayer ? Color.green : Color.red);

                if (hitPlayer) hits++;
            }
            else if (drawVisionRays)
            {
                Debug.DrawRay(eyePos, dir * viewRadius, Color.gray);
            }
        }

        return hits;
    }

    /// <summary>
    /// Direct line-of-sight check against a target position.
    /// </summary>
    public bool CanSeeTarget(Vector3 targetPosition)
    {
        Vector3 eyePos = ownerTransform.position + Vector3.up * eyeHeight;
        Vector3 targetPos = targetPosition + Vector3.up * eyeHeight;
        Vector3 dirToTarget = targetPos - eyePos;
        float distance = dirToTarget.magnitude;

        if (distance > viewRadius) return false;
        if (Vector3.Angle(ownerTransform.forward, dirToTarget.normalized) > viewAngle / 2f) return false;
        if (Physics.Linecast(eyePos, targetPos, wallLayer)) return false;

        return true;
    }

    public Transform GetPlayerTransform() => playerTransform;
    public float GetViewRadius() => viewRadius;
    public float GetViewAngle() => viewAngle;

    private bool IsHitPlayer(RaycastHit hit)
    {
        if (hit.collider == null) return false;
        if (!string.IsNullOrEmpty(playerTag) && hit.collider.CompareTag(playerTag)) return true;
        if (playerTransform != null && (hit.transform == playerTransform || hit.transform.IsChildOf(playerTransform))) return true;
        return false;
    }

    private Vector3 DirFromAngle(float angleInDegrees)
    {
        float rad = angleInDegrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
    }

    public void DrawDebugGizmos(Vector3 pos, Vector3 centerDir)
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(pos, viewRadius);

        Vector3 left = Quaternion.AngleAxis(-viewAngle / 2f, Vector3.up) * centerDir;
        Vector3 right = Quaternion.AngleAxis(viewAngle / 2f, Vector3.up) * centerDir;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(pos, pos + left * viewRadius);
        Gizmos.DrawLine(pos, pos + right * viewRadius);
    }
}