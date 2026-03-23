using UnityEngine;
using UnityEngine.UIElements;

public class UILookAtPlayer : MonoBehaviour
{
    [SerializeField] private bool _lookAtPlayer;

    void LateUpdate()
    {
        if (_lookAtPlayer)
        {
            LookAtPlayer();
        }
        else
        {
            return;
        }
    }

    private void LookAtPlayer()
    {
        transform.forward = Camera.main.transform.forward;
    }   
}

