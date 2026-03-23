using UnityEngine;

public class PullUp : MonoBehaviour
{
    [SerializeField] private Transform _playerHangingPoint;

    [SerializeField] private Transform _hangingPoint;

    [SerializeField] private Rigidbody _player;

    [SerializeField] private float _pullUpForce;

    [SerializeField] private float _pullUpSpeed;

    public static event System.Action HoldingUp;

    private bool _canPullUp;

    private bool _pullingUp;

    private bool _hasHeldUp; 

    void Start()
    {
        _canPullUp = false;

        _pullingUp = false;

        _hasHeldUp = false;

        _player.isKinematic = false;
    }

    void Update()
    {
        if (_canPullUp)
        {
            StartPullingUp();
        }

        if (_pullingUp)
        {
            _playerHangingPoint.position = Vector3.Lerp(_playerHangingPoint.position, _hangingPoint.position, _pullUpSpeed * Time.deltaTime);

            if (!_hasHeldUp && Vector3.Distance(_playerHangingPoint.position, _hangingPoint.position) < 0.01f)
            {
                _playerHangingPoint.position = _hangingPoint.position;

                _player.isKinematic = true;

                HoldingUp?.Invoke();

                _hasHeldUp = true;

                _pullingUp = false;

                _canPullUp = false;

                Debug.Log("Executed the 'PullingUp' and 'HoldingUp' actions once!");
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
    }
}


