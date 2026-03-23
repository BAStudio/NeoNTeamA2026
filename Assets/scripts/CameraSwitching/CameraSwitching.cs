using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class CameraSwitching : MonoBehaviour
{
    [SerializeField] List<GameObject> cameraPointLists = new List<GameObject>();
    [SerializeField] private Camera cam;
    [SerializeField] private CameraPoint[] cameraPoints;
    [SerializeField] private GameObject textUI;
    [SerializeField] private Transform player;
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private Transform cameraPlace;
    [SerializeField] private FirstPersonCamera firstPersonCamera;

    private int index = 0;

    void Start()
    {
        CameraPoint cameraPoint = GetComponent<CameraPoint>();

    }
    void Update()
    {
        float distance = Vector3.Distance(player.position, interactionPoint.position);

        textUI.SetActive(distance <= maxDistance);

        if (distance <= maxDistance && Input.GetKeyDown(KeyCode.P))
        {
            firstPersonCamera.enabled = false;


            cam.transform.position = cameraPoints[index].GetCameraPointPosition();
            cam.transform.rotation = cameraPoints[index].GetCameraPointRotation();

            index++;

            if (index >= cameraPoints.Length)
            {
                index = 0;
                cam.transform.position = cameraPlace.position;
                firstPersonCamera.enabled = true;

            }
        }
        if (distance > maxDistance - 0.1 && distance <= maxDistance)
        {
            cam.transform.position = cameraPlace.position;
            firstPersonCamera.enabled = true;
        }
    }

}