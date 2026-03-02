using System.Runtime.CompilerServices;
using UnityEngine;

public class PullUp : MonoBehaviour
{
    [SerializeField] private Transform _playerHangingPoint;

    [SerializeField] private Transform _hangingPoint;

    [SerializeField] private float _pullUpForce;

    private bool _canPullUp;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _canPullUp = false;
    }

    // Update is called once per frame
    void Update()
    {
        if(_canPullUp)
        {             
            PullingUp();
        }
    }

    public void PullingUp()
    {
        _playerHangingPoint.position = Vector3.Lerp(_playerHangingPoint.position, _hangingPoint.position, Time.deltaTime * _pullUpForce);

        Debug.Log("Pulling up");
    }
}


