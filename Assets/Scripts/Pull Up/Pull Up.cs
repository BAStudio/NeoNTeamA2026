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

    private bool _canPullUp;

    private bool _pullingUp;

    private bool _hasHeldUp;

    private Vector3 _startPosition;

    private float _progress;

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
        if (_canPullUp)
        {
            StartPullingUp();
        }

        if (_pullingUp)
        {
            _progress += _pullUpSpeed * Time.deltaTime;

            Vector3 newPosition = Vector3.Lerp(
                _startPosition,
                _hangingPoint.position,
                _progress
            );

            _playerHangingPoint.position = newPosition;

            _player.transform.position = _playerHangingPoint.position;

            if (_progress >= 1f)
            {
                _playerHangingPoint.position = _hangingPoint.position;

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
        _canPullUp = true;

        _hasHeldUp = false;
    }

    private void StartPullingUp()
    {
        _pullingUp = true;

        _canPullUp = false;

        _startPosition = _playerHangingPoint.position;

        _progress = 0f;

        _player.linearVelocity = Vector3.zero;

        _player.angularVelocity = Vector3.zero;

        _player.isKinematic = true;
    }

    private void FinishPullUp()
    {
        _pullingUp = false;

        _hasHeldUp = true;

        _player.linearVelocity = Vector3.zero;

        _player.angularVelocity = Vector3.zero;

        _playerHangingPoint.position = _hangingPoint.position;

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
    }
}