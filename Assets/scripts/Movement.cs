using UnityEngine;
using UnityEngine.Rendering;

public class Movement : MonoBehaviour
{
    private float _speed = 30f;
    private Rigidbody rb;

    private float _velocity;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        Move();
   
    }

    private void Move()
    {

        transform.position -= (Input.GetAxis("Vertical") * transform.forward + Input.GetAxis("Horizontal") * transform.right) * _speed * Time.deltaTime;



        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            _velocity = 50f;
        }
        else 
        {
            _velocity = 30f;
        }
    }
}
