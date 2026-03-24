using UnityEngine;
using System;
using System.Collections;

public class Mantling : MonoBehaviour
{
    public event Action OnStartMantling;
    [SerializeField] private float climbHeight = 2f;
    [SerializeField] private float climbDuration = 0.8f;
    [SerializeField] private float topDelay = 2f;
    [SerializeField] private float forwardStepDistance = 0.5f;
    [SerializeField] private float forwardStepDuration = 0.2f;
    [SerializeField] private float movementLockTime = 1f;
    [SerializeField] private float climbCooldown = 2f;
    [SerializeField] private float rayDistance = 1f;
    private bool canClimb = true;
    private bool isClimbing;
    private Vector3 climbDirection;
    private PlayerMovement playerMovement;

    private void Start()
        {
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) TryClimb();
    }

    private void TryClimb()
    {
        if (!canClimb || isClimbing) return;
        if (!Physics.Raycast(transform.position, transform.forward, out var hit, rayDistance)) return;
        if (!hit.collider.CompareTag("mantling")) return;
        climbDirection = -hit.normal; 
        climbDirection.y = 0f; 
        climbDirection.Normalize();
        StartCoroutine(ClimbRoutine());
    }

    private IEnumerator ClimbRoutine()
    {
        OnStartMantling?.Invoke();
        isClimbing = true;
        canClimb = false;
        if (playerMovement) playerMovement.enabled = false;

        yield return MoveTo(transform.position + Vector3.up * climbHeight, climbDuration);
        yield return new WaitForSeconds(topDelay);
        yield return MoveTo(transform.position + climbDirection * forwardStepDistance, forwardStepDuration);
        yield return new WaitForSeconds(movementLockTime);
        if (playerMovement) playerMovement.enabled = true;
        isClimbing = false;

        yield return new WaitForSeconds(climbCooldown);
        canClimb = true;
    }

    private IEnumerator MoveTo(Vector3 target, float duration)
    {
        Vector3 start = transform.position;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(start, target, t / duration);
            yield return null;
        }
        transform.position = target;
    }
}