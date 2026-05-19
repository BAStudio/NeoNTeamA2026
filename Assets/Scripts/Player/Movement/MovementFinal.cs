using UnityEngine;

public class PlayerMovement : BasePhysics
{
    [Header("References")]
    [SerializeField] private Transform orientation; 

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float sprintSpeed = 12f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField, Range(0f, 1f)] private float airControl = 0.4f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 8f;

    private Vector3 inputDirection;
    private bool isSprinting;

    protected override void Update()
    {
        base.Update();

        HandleInput();
        HandleMovement();
        HandleJump();

        SafeMove(currentVelocity * Time.deltaTime);
    }

    private void Awake()
    {
        if (orientation == null && Camera.main != null)
            orientation = Camera.main.transform.parent; // works if camera is a child of the player root
    }

    private void HandleInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        isSprinting = Input.GetKey(KeyCode.LeftShift);

        if (orientation == null)
        {
            inputDirection = new Vector3(h, 0f, v).normalized;
            return;
        }

        Vector3 forward = orientation.forward;
        Vector3 right = orientation.right;

        forward = -forward;
        right = -right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        inputDirection = (forward * v + right * h).normalized;
    }

    private void HandleMovement()
    {
        float control = IsGrounded ? 1f : airControl;
        float currentSpeed = isSprinting && IsGrounded ? sprintSpeed : moveSpeed;

        Vector3 targetVelocity = inputDirection * currentSpeed;

        currentVelocity.x = Mathf.MoveTowards(
            currentVelocity.x,
            targetVelocity.x,
            acceleration * control * Time.deltaTime
        );

        currentVelocity.z = Mathf.MoveTowards(
            currentVelocity.z,
            targetVelocity.z,
            acceleration * control * Time.deltaTime
        );
    }

    private void HandleJump()
    {
        if (IsGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            currentVelocity.y = 0f;
            ApplyImpulse(Vector3.up * jumpForce);
        }
    }
}