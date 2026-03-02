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

        Vector3 positionUpdate = transform.position - Input.GetAxis("Vertical") * transform.forward * _speed * Time.deltaTime;
        transform.position = positionUpdate;
        Vector3 positionUpdate2 = transform.position - Input.GetAxis("Horizontal") * transform.right * _speed * Time.deltaTime;
        transform.position = positionUpdate2;

       

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
