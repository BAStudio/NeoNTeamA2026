using UnityEngine;
using System;

public class WallClimbing : MonoBehaviour
{
    public event Action<bool> OnWallClimb;
    [SerializeField] private float climbSpeed = 4f;
    [SerializeField] private float detectDistance = 1f;
    [SerializeField] private LayerMask climbLayer;
    [SerializeField] private float rayCastPosition = 0.8f;
    private bool isClimbing;
    private bool wasPressing;
    private Rigidbody rb;

    private PlayerStamina playerStamina;

    private void Start()
    {
        playerStamina = PlayerStamina.Instance;
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        DetectWall();
        if (isClimbing && playerStamina.stamina > 0) ClimbMovement();
    }

    private void DetectWall()
    {
        bool pressing = Input.GetKey(KeyCode.Space) && playerStamina.stamina > 0;

        Vector3 origin = transform.position + Vector3.down * rayCastPosition;
        if (Physics.Raycast(origin, transform.forward, out RaycastHit hit, detectDistance, climbLayer))
        {
            if (pressing) StartClimbing();

            if (pressing != wasPressing)
            {
                wasPressing = pressing;
                OnWallClimb?.Invoke(pressing);
            }
        }
        else
        {
            StopClimbing();

            if (wasPressing)
            {
                wasPressing = false;
                OnWallClimb?.Invoke(false);
            }
        }
    }

    private void ClimbMovement()
    {
        playerStamina.ConsumeStamina(0.5f * Time.deltaTime);
        float y = 0f;
        if (Input.GetKey(KeyCode.Space) && playerStamina.stamina > 0) y = climbSpeed;
        else y = -climbSpeed;
        rb.linearVelocity = new Vector3(0, y, 0);
    }

    private void StartClimbing()
    {
        if (isClimbing) return;
        isClimbing = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
    }

    private void StopClimbing()
    {
        if (!isClimbing) return;
        isClimbing = false;
        rb.useGravity = true;
    }
}