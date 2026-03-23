using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;

public class Movement : MonoBehaviour
{
    [SerializeField] private float _speed = 30f;
    private Rigidbody rb;
    private bool _isRunning = false;
    [SerializeField]private float _runSpeed = 50f;
    [SerializeField] private float _walkSpeed = 30f;

    private bool _allowedToMove = true;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        NotePad.StopMovement += StopMovement;

        NotePad.ContinueMovement += ContinueMovement;

        if(_allowedToMove)
        {
            Move();
        }

        if (!_allowedToMove)
        {
            return;
        }

    }

    private void Move()
    {
        //laat het character bewegen naar voor achter links en recht
        transform.position -= (Input.GetAxis("Vertical") * transform.forward + Input.GetAxis("Horizontal") * transform.right) * _speed * Time.deltaTime;

        //zorgt ervoor als je left shift in klikt dat je sneller gaat (sprinten)
        _isRunning = Input.GetKey(KeyCode.LeftShift);
        _speed = _isRunning ? _runSpeed : _walkSpeed;  
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
