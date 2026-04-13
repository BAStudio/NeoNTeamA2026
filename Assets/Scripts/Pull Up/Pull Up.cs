using UnityEngine;
using UnityEngine.Events;

public class PullUp : MonoBehaviour
{
    [SerializeField] private Transform _playerHangingPoint;

    [SerializeField] private Transform _hangingPoint;
        
    [SerializeField] private Rigidbody _player;

    [SerializeField] private Collider _playerCollider;

    [SerializeField] private float _pullUpSpeed;

    [SerializeField] private PlayerCamera _playerCamera;

    [SerializeField] private RotatePlayer _rotatePlayer;

    [SerializeField] private UnityEvent HoldingUp;

    // ✅ NEW (cooldown)
    [SerializeField] private float _cooldown = 1.0f;

    private float _nextPullUpTime;

    private bool _canPullUp;

    private bool _pullingUp;

    private bool _hasHeldUp;

    private Vector3 _startPosition;

    private float _progress;

    private float _kinematicDelay = 0.1f;

    private float _kinematicTimer;

    private bool _isCurrentTarget = false;

    private void OnEnable()
    {
        UITrigger.PlayerUITriggerEnter += OnEnter;
        UITrigger.PlayerUITriggerExit += OnExit;
    }

    private void OnDisable()
    {
        UITrigger.PlayerUITriggerEnter -= OnEnter;
        UITrigger.PlayerUITriggerExit -= OnExit;
    }

    private void OnEnter(GameObject obj)
    {
        if (obj == gameObject)
        {
            _isCurrentTarget = true;
        }
    }

    private void OnExit(GameObject obj)
    {
        if (obj == gameObject)
        {
            _isCurrentTarget = false;
        }
    }

    private void Start()
    {
        _canPullUp = false;

        _pullingUp = false;

        _hasHeldUp = false;

        _player.isKinematic = false;

        _playerCollider.enabled = true;
    }

    private void Update()
    {
        // ✅ changed to GetKeyDown (prevent spam)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Release();
        }

        if (_canPullUp)
        {
            StartPullingUp();
        }

        if (_pullingUp)
        {
            _progress += _pullUpSpeed * Time.deltaTime;

            float easedProgress = Mathf.SmoothStep(0f, 1f, _progress);

            Vector3 newPosition = Vector3.Lerp(
                _startPosition,
                _hangingPoint.position,
                easedProgress
            );

            // ✅ FIXED: use player position directly
            _player.transform.position = newPosition;

            if (_progress >= 1f)
            {
                _player.transform.position = _hangingPoint.position;

                if (!_hasHeldUp)
                {
                    FinishPullUp();
                }
            }
        }
    }

    public void CanPullUp()
    {
        if (!_isCurrentTarget) return;

        // ✅ NEW: block while hanging or pulling
        if (_hasHeldUp || _pullingUp) return;

        // ✅ NEW: cooldown check
        if (Time.time < _nextPullUpTime) return;

        _canPullUp = true;

        Debug.Log("Correct PullUp triggered on: " + gameObject.name);
    }

    private void StartPullingUp()
    {
        _kinematicTimer += Time.deltaTime;

        if (_kinematicTimer >= _kinematicDelay && !_player.isKinematic)
        {
            _player.linearVelocity = Vector3.zero;
            _player.angularVelocity = Vector3.zero;

            _player.isKinematic = true;
        }

        _pullingUp = true;

        _canPullUp = false;

        // ✅ FIXED: correct start position
        _startPosition = _player.transform.position;

        _progress = 0f;

        _player.linearVelocity = Vector3.zero;
        _player.angularVelocity = Vector3.zero;

        _kinematicTimer = 0f;
    }

    private void FinishPullUp()
    {
        _pullingUp = false;
        _hasHeldUp = true;

        _player.linearVelocity = Vector3.zero;
        _player.angularVelocity = Vector3.zero;

        _player.transform.position = _hangingPoint.position;

        _player.isKinematic = true;

        HoldingUp?.Invoke();

        _playerCamera.EnableLimitedLook();

        _rotatePlayer.DisableRotation();

        _playerCollider.enabled = false;

        Debug.Log("Pull-up completed and aligned EXACTLY to hanging point!");
    }

    private void Release()
    {
        _playerCamera.DisableLimitedLook();

        _rotatePlayer.EnableRotation();

        _playerCollider.enabled = true;

        _player.isKinematic = false;

        // reset state
        _canPullUp = false;

        _pullingUp = false;

        _hasHeldUp = false;

        _progress = 0f;
        _kinematicTimer = 0f;

        _player.linearVelocity = Vector3.zero;
        _player.angularVelocity = Vector3.zero;

        // ✅ NEW: start cooldown
        _nextPullUpTime = Time.time + _cooldown;

        Debug.Log("Pull-up released and reset");
    }
}