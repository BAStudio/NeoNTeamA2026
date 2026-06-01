using UnityEngine;

public interface ISignalReceiver
{
    // Generic stimulus input: camera pings, alarms, scripted sightings, etc.
    void OnSignalReceived(Vector3 source, float strength);
}