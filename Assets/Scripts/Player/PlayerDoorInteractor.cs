using UnityEngine;

public class PlayerDoorInteractor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;

    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactDistance = 3f;

    [Header("Door")]
    [SerializeField] private string doorTag = "Door";

    [Header("Debug")]
    [SerializeField] private bool drawDebug = false;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Update()
    {
        if (Input.GetKeyDown(interactKey))
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        if (playerCamera == null)
            return;

        Ray ray = playerCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactDistance,
            ~0,
            QueryTriggerInteraction.Ignore))
        {
            if (drawDebug)
            {
                Debug.DrawLine(
                    ray.origin,
                    hit.point,
                    Color.green,
                    1f);
            }

            Collider col = hit.collider;

            if (col == null)
                return;

            bool validDoor =
                col.CompareTag(doorTag) ||
                col.transform.root.CompareTag(doorTag);

            if (!validDoor)
                return;

            Door door =
                col.GetComponentInParent<Door>();

            if (door == null)
                return;

            if (door.IsLocked)
                return;

            door.ToggleDoor();
        }
        else
        {
            if (drawDebug)
            {
                Debug.DrawRay(
                    ray.origin,
                    ray.direction * interactDistance,
                    Color.red,
                    1f);
            }
        }
    }
}