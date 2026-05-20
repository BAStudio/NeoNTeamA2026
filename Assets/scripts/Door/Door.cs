using System;
using System.Collections;
using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] private DoorHandle doorHandle;

    [SerializeField] private Transform doorTransform;

    [SerializeField] private Transform openPosition;
    [SerializeField] private Transform openRotation;

    [SerializeField] private float moveDuration = 1f;

    private Vector3 closedPosition;
    private Quaternion closedRotation;

    private void Start()
    {
        closedPosition = doorTransform.position;
        closedRotation = doorTransform.rotation;
    }

    private void OnEnable()
    {
        doorHandle.OnDoorHandleClicked += OpenDoor;
        doorHandle.OnDoorOpened += CloseDoor;
    }

    private void OnDisable()
    {
        doorHandle.OnDoorHandleClicked -= OpenDoor;
        doorHandle.OnDoorOpened -= CloseDoor;
    }

    private void OpenDoor()
    {
        StopAllCoroutines();
        StartCoroutine(MoveDoor(
            openPosition.position,
            openRotation.rotation));
    }

    private void CloseDoor()
    {
        StopAllCoroutines();
        StartCoroutine(MoveDoor(
            closedPosition,
            closedRotation));
    }

    private IEnumerator MoveDoor(Vector3 targetPos, Quaternion targetRot)
    {
        Vector3 startPos = doorTransform.position;
        Quaternion startRot = doorTransform.rotation;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * moveDuration;

            doorTransform.position =
                Vector3.Lerp(startPos, targetPos, t);

            doorTransform.rotation =
                Quaternion.Slerp(startRot, targetRot, t);

            yield return null;
        }

        doorTransform.position = targetPos;
        doorTransform.rotation = targetRot;
    }
}