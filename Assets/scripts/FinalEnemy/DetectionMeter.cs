using UnityEngine;

public class DetectionMeter : MonoBehaviour
{
    [Header("Detection Meter")]
    [SerializeField] private float detectionDecaySpeed = 5f;
    [SerializeField] private float stalkingDecaySpeed = 2f;
    [SerializeField] private float maxDetection = 100f;

    private float currentDetection;

    private void OnEnable()
    {
        currentDetection = 0f;
    }

    public void Tick(AIStateManager.AIState currentState, float deltaTime)
    {
        float decayRate = currentState == AIStateManager.AIState.Stalking
            ? stalkingDecaySpeed
            : detectionDecaySpeed;

        currentDetection = Mathf.Clamp(currentDetection - decayRate * deltaTime, 0f, maxDetection);
    }

    public void AddDetection(float amount)
    {
        currentDetection = Mathf.Clamp(currentDetection + amount, 0f, maxDetection);
    }

    public void Reset()
    {
        currentDetection = 0f;
    }

    public float GetDetection() => currentDetection;
    public float GetMaxDetection() => maxDetection;

    public bool IsStalking() => currentDetection >= maxDetection * 0.5f;
    public bool IsFullyDetected() => currentDetection >= maxDetection * 0.7f;
    public bool IsAlerted() => currentDetection >= maxDetection * 0.3f;
}