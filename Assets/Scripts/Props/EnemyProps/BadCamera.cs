using UnityEngine;
using UnityEngine.UI;

public class BadCamera : MonoBehaviour
{
    [Header("Rotation")]
    public float rotationSpeed = 60f;

    [Header("Ray Settings")]
    public float rayDistance = 2f;
    public LayerMask wallLayer;

    [Header("Detection")]
    public float detectionDistance = 8f;
    public float detectionTime = 3f;

    [Header("Enemy")]
    public string enemyTag = "Enemy";

    [Header("UI")]
    public Image detectionCircle;

    private float currentAngle;
    private int direction = 1;

    private HorrorEnemyAI enemyAI;
    private PlayerMovement playerMovement;

    private float detectionTimer;

    void Start()
    {
        GameObject enemy =
            GameObject.FindGameObjectWithTag(enemyTag);

        if (enemy != null)
        {
            enemyAI = enemy.GetComponent<HorrorEnemyAI>();
        }

        GameObject player =
            GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            playerMovement =
                player.GetComponent<PlayerMovement>();
        }

        if (detectionCircle != null)
        {
            detectionCircle.fillAmount = 0f;
            detectionCircle.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        RotateCamera();
        DetectPlayer();
    }

    void RotateCamera()
    {
        // CHECK ALL 360 RAYS
        for (int angle = 0; angle < 360; angle++)
        {
            Vector3 checkDir =
                Quaternion.Euler(0f, angle, 0f) *
                Vector3.forward;

            bool blocked = Physics.Raycast(
                transform.position,
                checkDir,
                rayDistance,
                wallLayer);

            Debug.DrawRay(
                transform.position,
                checkDir * rayDistance,
                blocked ? Color.red : Color.green
            );
        }

        // CAMERA CURRENT DIRECTION
        Vector3 currentDir =
            Quaternion.Euler(0f, currentAngle, 0f) *
            Vector3.forward;

        // IF CURRENT PATH IS BLOCKED -> TURN AROUND
        if (Physics.Raycast(
            transform.position,
            currentDir,
            rayDistance,
            wallLayer))
        {
            direction *= -1;
        }

        // ROTATE
        currentAngle +=
            rotationSpeed *
            direction *
            Time.deltaTime;

        transform.rotation =
            Quaternion.Euler(0f, currentAngle, 0f);
    }

    void DetectPlayer()
    {
        if (playerMovement == null)
            return;

        Transform player =
            playerMovement.transform;

        Vector3 toPlayer =
            (player.position - transform.position)
            .normalized;

        float distance =
            Vector3.Distance(
                transform.position,
                player.position);

        bool canSeePlayer =
            distance <= detectionDistance &&
            !Physics.Raycast(
                transform.position,
                toPlayer,
                distance,
                wallLayer);

        if (canSeePlayer)
        {
            // CROUCHING = slower detection
            if (playerMovement.IsCrouching)
            {
                detectionTimer +=
                    Time.deltaTime * 0.35f;
            }
            else
            {
                detectionTimer +=
                    Time.deltaTime;
            }

            if (detectionCircle != null)
            {
                detectionCircle.gameObject.SetActive(true);

                detectionCircle.fillAmount =
                    detectionTimer /
                    detectionTime;
            }

            if (detectionTimer >= detectionTime)
            {
                if (enemyAI != null)
                {
                    enemyAI.OnSignalReceived(
                        player.position,
                        .05f);
                }
            }
        }
        else
        {
            detectionTimer -= Time.deltaTime;

            detectionTimer =
                Mathf.Clamp(
                    detectionTimer,
                    0f,
                    detectionTime);

            if (detectionCircle != null)
            {
                detectionCircle.fillAmount =
                    detectionTimer /
                    detectionTime;

                if (detectionTimer <= 0f)
                {
                    detectionCircle.gameObject.SetActive(false);
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            detectionDistance
        );
    }
}