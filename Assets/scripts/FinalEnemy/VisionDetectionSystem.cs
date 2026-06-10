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

    public Transform GetPlayerTransform() => playerTransform;
    public float GetViewRadius() => viewRadius;
    public float GetViewAngle() => viewAngle;

    public bool CanSeeTarget(Vector3 targetPosition)
    {
        Vector3 eyePos = ownerTransform.position + Vector3.up * eyeHeight;
        Vector3 targetPos = targetPosition + Vector3.up * eyeHeight;
        Vector3 dirToTarget = targetPos - eyePos;
        float distance = dirToTarget.magnitude;

        if (distance > viewRadius)
            return false;

        if (Vector3.Angle(ownerTransform.forward, dirToTarget.normalized) > viewAngle / 2f)
            return false;

        if (Physics.Linecast(eyePos, targetPos, wallLayer))
            return false;

        return true;
    }

    public float CalculateStimulus(Vector3 targetPosition)
    {
        Vector3 eyePos = ownerTransform.position + Vector3.up * eyeHeight;
        Vector3 targetPos = targetPosition + Vector3.up * eyeHeight;
        Vector3 dirToTarget = targetPos - eyePos;
        float distance = dirToTarget.magnitude;

        if (distance > viewRadius)
            return 0f;

        Vector3 dir = dirToTarget.normalized;

        if (Vector3.Angle(ownerTransform.forward, dir) > viewAngle / 2f)
            return 0f;

        if (Physics.Linecast(eyePos, targetPos, wallLayer))
            return 0f;

        float distanceFactor = 1f - Mathf.Clamp01(distance / viewRadius);
        return distanceFactor;
    }

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
                bool hitWall = ((1 << hit.collider.gameObject.layer) & wallLayer) != 0;

                if (drawVisionRays)
                    Debug.DrawRay(eyePos, dir * hit.distance, hitWall ? Color.red : Color.green);

                if (hitWall)
                    continue;

                if (IsHitPlayer(hit))
                    hits++;
            }
            else if (drawVisionRays)
            {
                Debug.DrawRay(eyePos, dir * viewRadius, Color.gray);
            }
        }

        return hits;
    }

    private bool IsHitPlayer(RaycastHit hit)
    {
        if (hit.collider == null)
            return false;

        if (!string.IsNullOrEmpty(playerTag) && hit.collider.CompareTag(playerTag))
            return true;

        if (playerTransform != null &&
            (hit.transform == playerTransform || hit.transform.IsChildOf(playerTransform)))
            return true;

        return false;
    }

    private Vector3 DirFromAngle(float angleInDegrees)
    {
        float rad = angleInDegrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
    }
}