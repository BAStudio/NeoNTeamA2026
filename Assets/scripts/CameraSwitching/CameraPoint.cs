
using UnityEngine;
using UnityEngine.UI;
using System;
using Unity.VisualScripting;

public class CameraPoint : MonoBehaviour
{
    //[SerializeField] private Button button;
    [SerializeField] private Vector3 rotation;
    [SerializeField] private GameObject panel;
    [SerializeField] private bool isOnCameraPoint = false;
    [SerializeField] private bool isCameraPointActive = true;
    [SerializeField] private KeyCode activationKey = KeyCode.G;

    private void Start()
    {
        //button.onClick.AddListener(OnButtonClick);
    }

    private void Update()
    {

        if (Input.GetKeyDown(activationKey))
        {
            if (isCameraPointActive == true)
            {
                isCameraPointActive = false;
                Debug.Log("Camera Point Deactivated: " + gameObject.name);
            }

            else if (isCameraPointActive == false)
            {
                isCameraPointActive = true;
                Debug.Log("Camera Point Activated: " + gameObject.name);
            }
        }


    }

    private void OnTriggerStay()
    {
        Debug.Log("Trigger Entered: " + gameObject.name);
        if (CompareTag("Camera") && isCameraPointActive == false)
        {
            isOnCameraPoint = true;
            if (isOnCameraPoint == true)
            {
                panel.SetActive(true);
            }
            else if (isOnCameraPoint == false)
            {
                panel.SetActive(false);
            }
        }
        if (CompareTag("Camera") && isCameraPointActive == true)
        {
            if (isCameraPointActive == true)
            {
                panel.SetActive(false);
            }

        }
    }
    private void OnTriggerExit()
    {
        Debug.Log("Trigger Exited: " + gameObject.name);
        if (CompareTag("Camera"))
        {
            isOnCameraPoint = false;
            if (isOnCameraPoint == true)
            {
                panel.SetActive(true);
            }
            else if (isOnCameraPoint == false)
            {
                panel.SetActive(false);
            }
        }
    }

    public Vector3 GetCameraPointPosition()
    {
        return transform.position;
    }

    public Quaternion GetCameraPointRotation()
    {
        return Quaternion.Euler(rotation);
    }
}