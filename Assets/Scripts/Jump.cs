using UnityEngine;

public class Jump : MonoBehaviour
{
    private Vector3 jump;
    [SerializeField] float jumpForce = 2.0f;

    [SerializeField] private bool isGrounded;
    Rigidbody rb;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        jump = new Vector3(0, jumpForce, 0);
    }


    private void OnCollisionEnter(Collision collision)
    {
        isGrounded = true;
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            {
                isGrounded = false;
                rb.AddForce(jump * jumpForce, ForceMode.Impulse);
                Debug.Log("jump");
            }
        }
    }
}
