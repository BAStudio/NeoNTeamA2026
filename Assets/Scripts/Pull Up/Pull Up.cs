using System.Runtime.CompilerServices;
using UnityEngine;

public class PullUp : MonoBehaviour
{
    [SerializeField] private Transform _playerHangingPoint;

    [SerializeField] private Transform _hangingPoint;

    [SerializeField] private float _pullUpForce;

    [SerializeField] private float _pullUpSpeed;

    public static event System.Action HoldingUp;

    private bool _canPullUp;

    private bool _pullingUp;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _canPullUp = false;

        _pullingUp = false;
    }

    // Update is called once per frame
    void Update()
    {

        if (_canPullUp)
        {
            PullingUp();
        }

        if (_pullingUp)
        {
            _playerHangingPoint.position = Vector3.Lerp(_playerHangingPoint.position, _hangingPoint.position, _pullUpSpeed * Time.deltaTime);

            if (Vector3.Distance(_playerHangingPoint.position, _hangingPoint.position) < 0.01f)
            {
                _playerHangingPoint.position = _hangingPoint.position;

                HoldingUp?.Invoke();

                _canPullUp = false;

                Debug.Log("Executed the 'PullingUp' and 'HoldingUp' actions");
            }
        }

        
    }

    public void CanPullUp()
    {
        _canPullUp = true;
    }

    private void PullingUp()
    {
        _pullingUp = true;

        _canPullUp = false;
    }
}


