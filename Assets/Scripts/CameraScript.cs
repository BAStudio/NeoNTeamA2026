using UnityEngine;

public class CameraScript : MonoBehaviour
{
    [SerializeField] private float RotateY;
    [SerializeField] private float Speed;

    [SerializeField] private float angle;

    // Update is called once per frame
    void Update()
    {
        angle += Speed * Time.deltaTime;
        transform.rotation = Quaternion.Euler(0f, angle, 0f);
    }
}
