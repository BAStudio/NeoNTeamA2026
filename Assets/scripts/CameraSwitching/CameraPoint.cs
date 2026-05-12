
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
    [SerializeField] private KeyCode activationKey = KeyCode.G;
    private bool ifHacked = false;
    hacking hacking;


    private void Start()
    {
        //button.onClick.AddListener(OnButtonClick);
        hacking = GetComponent<hacking>();
        hacking.OnCameraPointFalse += SartHacking;
    }


    private void OnTriggerStay()
    {
        Debug.Log("Trigger Entered: " + gameObject.name);
        if (CompareTag("Camera")  && ifHacked == true)
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

    void SartHacking()
    {
        ifHacked = true;

    }

}