using UnityEngine;

public class CameraPoint : MonoBehaviour
{
    [SerializeField] private Vector3 rotation;

    public Vector3 GetCameraPointPosition()
    {
        return transform.position;
    }

    public Quaternion GetCameraPointRotation()
    {
        return Quaternion.Euler(rotation);
    }
}