
using UnityEngine;
using UnityEngine.UI;
using System;

public class CameraPoint : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Vector3 rotation;
    [SerializeField] private GameObject panel;
    [SerializeField] private bool isActive = false;

    private void Start()
    {
        button.onClick.AddListener(OnButtonClick);
    }

    private void OnButtonClick()
    {
        isActive = true;
    }

    private void Update()
    {

        if (isActive == true)
        {
            panel.SetActive(true);
            isActive = false;
        }
        else
        {
            panel.SetActive(false);
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