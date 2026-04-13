using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float rotationSpeed = 180f;

    private Rigidbody rb;
    private Vector3 movement;
    private float rotationInput;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.z = Input.GetAxisRaw("Vertical");
        movement = movement.normalized;

        rotationInput = 0f;

        if (Input.GetKey(KeyCode.E))
            rotationInput = 1f;

        if (Input.GetKey(KeyCode.Q))
            rotationInput = -1f;
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + transform.TransformDirection(movement) * speed * Time.fixedDeltaTime);
        if (rotationInput != 0f)
        {
            Quaternion turn = Quaternion.Euler(0f, rotationInput * rotationSpeed * Time.fixedDeltaTime, 0f);
            rb.MoveRotation(rb.rotation * turn);
        }
    }
}