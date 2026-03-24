using UnityEngine;

public class Movement : MonoBehaviour
{
    [SerializeField] private float _speed;
    private Rigidbody rb;
    private bool _isRunning = false;

    [SerializeField] private float _runSpeed = 25f;
    [SerializeField] private float _walkSpeed = 15f;

    private bool _allowedToMove = true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Subscribe ONCE
        NotePad.StopMovement += StopMovement;
        NotePad.ContinueMovement += ContinueMovement;

        // Prevent tipping over
        rb.freezeRotation = true;

        // Better collision for fast movement
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    void FixedUpdate()
    {
        if (!_allowedToMove)
        {
            // Stop movement completely
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            return;
        }

        Move();
    }

    private void Move()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = (v * transform.forward + h * transform.right).normalized;

        // Sprint logic
        _isRunning = Input.GetKey(KeyCode.LeftShift);
        _speed = _isRunning ? _runSpeed : _walkSpeed;

        Vector3 targetVelocity = move * _speed;

        // ✅ THIS is the key line
        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
    }

    private void StopMovement()
    {
        _allowedToMove = false;
    }

    private void ContinueMovement()
    {
        _allowedToMove = true;
    }
}