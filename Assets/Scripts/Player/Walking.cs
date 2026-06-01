using UnityEngine;

public class Walking : MonoBehaviour
{
    public Rigidbody playerRigidbody;

    private float walkingSpeed;
    private float runningSpeed;

    private float speed;

    private bool isRunning;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GetComponent<Rigidbody>();

        walkingSpeed = 10f;
        runningSpeed = 16f;

        speed = 0f;


        isRunning = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            isRunning = true;

            speed = runningSpeed;
        }
        else
        {
            isRunning = false;

            speed = walkingSpeed;
        }

        if (Input.GetKey(KeyCode.W))
        {
            Vector3 currentForce = playerRigidbody.linearVelocity;

            if (currentForce.magnitude < speed)
            {
                playerRigidbody.AddForce(transform.forward * speed * 2f, ForceMode.Acceleration);
            }
        }

        if (Input.GetKey(KeyCode.A))
        {
            Vector3 currentForce = playerRigidbody.linearVelocity;

            if (currentForce.magnitude < speed)
            {
                playerRigidbody.AddForce(transform.right * -speed * 2f, ForceMode.Acceleration);
            }
        }

        if (Input.GetKey(KeyCode.S))
        {
            Vector3 currentForce = playerRigidbody.linearVelocity;

            if (currentForce.magnitude < speed)
            {
                playerRigidbody.AddForce(transform.forward * -speed * 2f, ForceMode.Acceleration);
            }
        }

        if (Input.GetKey(KeyCode.D))
        {
            Vector3 currentForce = playerRigidbody.linearVelocity;

            if (currentForce.magnitude < speed)
            {
                playerRigidbody.AddForce(transform.right * speed * 2f, ForceMode.Acceleration);
            }
        }
    }
}

