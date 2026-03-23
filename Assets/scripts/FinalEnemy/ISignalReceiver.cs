using UnityEngine;

public interface ISignalReceiver
{

    void OnSignalReceived(Vector3 source, float strength);
}