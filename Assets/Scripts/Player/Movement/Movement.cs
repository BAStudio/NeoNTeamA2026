using UnityEngine;
using UnityEngine.Events;

public class Movement_ : MonoBehaviour
{
    [SerializeField] private float _speed;
    private Rigidbody rb;

    private bool _isRunning = false;

    private float h;
    private float v;

    [SerializeField] private float _runSpeed = 25f;
    [SerializeField] private float _walkSpeed = 15f;

    [SerializeField] private float _acceleration = 8f;
    [SerializeField] private float _turnSpeedMultiplier = 2f;

    [SerializeField] private UnityEvent IsWalking;
    [SerializeField] private UnityEvent IsRunning;
    [SerializeField] private UnityEvent IsIdle;

    [SerializeField] private float _stopAudioDelay = 0.3f;
    private float _noInputTimer;

    private bool _allowedToMove = true;

    PlayerStamina playerStamina;

    void Start()
    {
        playerStamina = PlayerStamina.Instance;
        rb = GetComponent<Rigidbody>();

        NotePad_.StopMovement += StopMovement;
        NotePad_.ContinueMovement += ContinueMovement;

        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    void FixedUpdate()
    {
        if (!_allowedToMove)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            return;
        }

        Move();

        bool isMoving = (h != 0 || v != 0);
        bool isRunning = Input.GetKey(KeyCode.LeftShift) && isMoving;

        if (isMoving)
        {
            _noInputTimer = 0f;

            if (isRunning && playerStamina.stamina > 0)
                Running();
            else
                Walking();
        }
        else
        {
            _noInputTimer += Time.fixedDeltaTime;

            if (_noInputTimer >= _stopAudioDelay)
                Idle();
        }
    }

    private void Move()
    {
        h = Input.GetAxis("Horizontal");
        v = Input.GetAxis("Vertical");

        Vector3 inputDir = (-v * transform.forward -h * transform.right).normalized;

        _isRunning = Input.GetKey(KeyCode.LeftShift);

        //float targetSpeed = _isRunning ? _runSpeed : _walkSpeed;

        float targetSpeed;

        if (playerStamina.stamina > 0 && _isRunning)
        {
            targetSpeed = _runSpeed;
        }
        else
        {
            targetSpeed = _walkSpeed;
        }

            Vector3 currentVelocity = rb.linearVelocity;
        Vector3 currentHorizontalVel = new Vector3(currentVelocity.x, 0, currentVelocity.z);

        Vector3 desiredVelocity = inputDir * targetSpeed;

        if (inputDir.sqrMagnitude > 0.01f)
        {
            float alignment = Vector3.Dot(currentHorizontalVel.normalized, inputDir);
            alignment = Mathf.Clamp01((alignment + 1f) * 0.5f); // 0 = opposite, 1 = same dir

            float dynamicAccel = _acceleration * Mathf.Lerp(_turnSpeedMultiplier, 1f, alignment);

            currentHorizontalVel = Vector3.Lerp(
                currentHorizontalVel,
                desiredVelocity,
                dynamicAccel * Time.fixedDeltaTime
            );
        }
        else
        {
            // natural deceleration when no input
            currentHorizontalVel = Vector3.Lerp(
                currentHorizontalVel,
                Vector3.zero,
                _acceleration * Time.fixedDeltaTime
            );
        }

        rb.linearVelocity = new Vector3(
            currentHorizontalVel.x,
            currentVelocity.y,
            currentHorizontalVel.z
        );
    }

    private void StopMovement()
    {
        _allowedToMove = false;
    }

    private void ContinueMovement()
    {
        _allowedToMove = true;
    }

    private void Walking()
    {
        IsWalking?.Invoke();
    }

    private void Running()
    {
        IsRunning?.Invoke();
        playerStamina.ConsumeStamina(1f * Time.fixedDeltaTime);
    }

    private void Idle()
    {
        IsIdle?.Invoke();
    }
}
