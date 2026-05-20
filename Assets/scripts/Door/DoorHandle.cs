using UnityEngine;
using System;

public class DoorHandle : MonoBehaviour
{
    private bool doorIsOpen = false;

    public event Action OnDoorHandleClicked;
    public event Action OnDoorOpened;

    private void OnMouseDown()
    {
        if (!doorIsOpen)
        {
            OpenDoor();
        }
        else
        {
            CloseDoor();
        }
    }

    private void OpenDoor()
    {
        OnDoorHandleClicked?.Invoke();
        doorIsOpen = true;

        //Debug.Log("Door opened");
    }

    private void CloseDoor()
    {
        OnDoorOpened?.Invoke();
        doorIsOpen = false;

        //Debug.Log("Door closed");
    }
}