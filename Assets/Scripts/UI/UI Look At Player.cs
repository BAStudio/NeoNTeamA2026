using UnityEngine;

public class UILookAtPlayer : MonoBehaviour
{
    void LateUpdate()
    {
        transform.forward = Camera.main.transform.forward;
    }
}

