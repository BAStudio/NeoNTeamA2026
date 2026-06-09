using System.Collections.Generic;
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

    [Header("Target Rotation")]
    public Transform objectToRotate;
    public float objectRotationSpeed = 10f;
    public float scanStep = 5f;

    private float currentAngle;
    private int direction = 1;

    private HorrorEnemyAI enemyAI;
    private PlayerMovement playerMovement;

    private float detectionTimer;

    void Start()
    {
        GameObject enemy = GameObject.FindGameObjectWithTag(enemyTag);
        if (enemy != null)
        {
            enemyAI = enemy.GetComponent<HorrorEnemyAI>();
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
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
        RotateObjectBasedOnWallHits();
        DetectPlayer();
    }

    void RotateCamera()
    {
        for (int angle = 0; angle < 360; angle++)
        {
            Vector3 checkDir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

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

        Vector3 currentDir = Quaternion.Euler(0f, currentAngle, 0f) * Vector3.forward;

        if (Physics.Raycast(
            transform.position,
            currentDir,
            rayDistance,
            wallLayer))
        {
            direction *= -1;
        }

        currentAngle += rotationSpeed * direction * Time.deltaTime;

        transform.rotation = Quaternion.Euler(0f, currentAngle + 180, 0f);
    }

    void RotateObjectBasedOnWallHits()
    {
        if (objectToRotate == null)
            return;

        List<float> hitAngles = new List<float>();

        for (float angle = 0f; angle < 360f; angle += scanStep)
        {
            Vector3 checkDir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

            bool hitWall = Physics.Raycast(
                transform.position,
                checkDir,
                rayDistance,
                wallLayer);

            if (hitWall)
            {
                hitAngles.Add(angle);

                Debug.DrawRay(
                    transform.position,
                    checkDir * rayDistance,
                    Color.red
                );
            }
        }

        if (hitAngles.Count == 0)
            return;

        float sumX = 0f;
        float sumY = 0f;

        foreach (float angle in hitAngles)
        {
            float radians = angle * Mathf.Deg2Rad;
            sumX += Mathf.Cos(radians);
            sumY += Mathf.Sin(radians);
        }

        float middleAngle = Mathf.Atan2(sumY, sumX) * Mathf.Rad2Deg;

        if (middleAngle < 0f)
            middleAngle += 360f;

        Quaternion targetRotation = Quaternion.Euler(0f, middleAngle, 0f);

        objectToRotate.rotation = Quaternion.Slerp(
            objectToRotate.rotation,
            targetRotation,
            Time.deltaTime * objectRotationSpeed
        );
    }

    void DetectPlayer()
    {
        if (playerMovement == null)
            return;

        Transform player = playerMovement.transform;

        Vector3 toPlayer = (player.position - transform.position).normalized;

        float distance = Vector3.Distance(transform.position, player.position);

        bool canSeePlayer =
            distance <= detectionDistance &&
            !Physics.Raycast(
                transform.position,
                toPlayer,
                distance,
                wallLayer);

        if (canSeePlayer)
        {
            if (playerMovement.IsCrouching)
            {
                detectionTimer += Time.deltaTime * 0.35f;
            }
            else
            {
                detectionTimer += Time.deltaTime;
            }

            if (detectionCircle != null)
            {
                detectionCircle.gameObject.SetActive(true);
                detectionCircle.fillAmount = detectionTimer / detectionTime;
            }

            if (detectionTimer >= detectionTime)
            {
                if (enemyAI != null)
                {
                    enemyAI.OnSignalReceived(player.position, .05f);
                }
            }
        }
        else
        {
            detectionTimer -= Time.deltaTime;
            detectionTimer = Mathf.Clamp(detectionTimer, 0f, detectionTime);

            if (detectionCircle != null)
            {
                detectionCircle.fillAmount = detectionTimer / detectionTime;

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