using UnityEngine;

public class EnemyDoorDetector : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float detectionDistance = 1.4f;
    [SerializeField] private float detectionRadius = 0.35f;
    [SerializeField] private float eyeHeight = 1.0f;

    [Header("Door Detection")]
    [SerializeField] private string doorTag = "Door";

    [Header("Behaviour")]
    [SerializeField] private float closeDelay = 0.08f;

    [Header("Debug")]
    [SerializeField] private bool drawDebug = false;

    private Door currentDoor;
    private float noDoorTimer;

    public void Tick(Vector3 lookDirection)
    {
        Vector3 origin =
            transform.position + Vector3.up * eyeHeight;

        Vector3 dir =
            lookDirection.sqrMagnitude > 0.0001f
            ? lookDirection.normalized
            : transform.forward;

        bool foundDoor = false;

        RaycastHit[] hits = Physics.SphereCastAll(
            origin,
            detectionRadius,
            dir,
            detectionDistance,
            ~0,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i].collider;

            if (col == null)
                continue;

            if (!col.CompareTag(doorTag) &&
                !col.transform.root.CompareTag(doorTag))
                continue;

            Door hitDoor = col.GetComponentInParent<Door>();

            if (hitDoor == null)
                continue;

            if (hitDoor.IsLocked)
                continue;

            foundDoor = true;

            // Close previous door if switching
            if (currentDoor != null &&
                currentDoor != hitDoor)
            {
                currentDoor.CloseDoor();
            }

            currentDoor = hitDoor;

            if (!currentDoor.IsOpen)
                currentDoor.OpenDoor();

            noDoorTimer = 0f;

            break;
        }

        if (!foundDoor)
        {
            noDoorTimer += Time.deltaTime;

            if (currentDoor != null &&
                noDoorTimer >= closeDelay)
            {
                currentDoor.CloseDoor();
                currentDoor = null;
                noDoorTimer = 0f;
            }
        }

        if (drawDebug)
        {
            Debug.DrawRay(
                origin,
                dir * detectionDistance,
                foundDoor ? Color.green : Color.red);
        }
    }

    private void OnDisable()
    {
        if (currentDoor != null)
        {
            currentDoor.CloseDoor();
            currentDoor = null;
        }
    }
}