using UnityEngine;

public class DetectionMeter : MonoBehaviour
{
    [Header("Detection Meter")]
    [SerializeField] private float detectionBuildSpeed = 60f;
    [SerializeField] private float detectionDecaySpeed = 5f; // Slower decay - stalker feels persistent
    [SerializeField] private float stalkingDecaySpeed = 2f; // Even slower in stalking state
    [SerializeField] private float maxDetection = 100f;

    private float currentDetection;

    private void OnEnable()
    {
        currentDetection = 0f;
    }

    /// <summary>
    /// Updates detection meter based on whether player is visible and current state
    /// </summary>
    public void Update(bool playerVisible, AIStateManager.AIState currentState)
    {
        if (playerVisible)
            currentDetection += detectionBuildSpeed * Time.deltaTime;
        else
        {
            // Slower decay in Stalking state (enemy is determined)
            float decayRate = (currentState == AIStateManager.AIState.Stalking) ? stalkingDecaySpeed : detectionDecaySpeed;
            currentDetection -= decayRate * Time.deltaTime;
        }

        currentDetection = Mathf.Clamp(currentDetection, 0f, maxDetection);
    }

    /// <summary>
    /// Adds detection from a signal or hearing
    /// </summary>
    public void AddDetection(float amount)
    {
        currentDetection = Mathf.Clamp(currentDetection + amount, 0f, maxDetection);
    }

    /// <summary>
    /// Resets detection to 0
    /// </summary>
    public void Reset()
    {
        currentDetection = 0f;
    }

    /// <summary>
    /// Gets current detection value
    /// </summary>
    public float GetDetection() => currentDetection;

    /// <summary>
    /// Gets max detection threshold
    /// </summary>
    public float GetMaxDetection() => maxDetection;

    /// <summary>
    /// Checks if detection is at stalking threshold (50% of max) - new lower threshold
    /// </summary>
    public bool IsStalking() => currentDetection >= maxDetection * 0.5f;

    /// <summary>
    /// Checks if detection is at chase threshold (70% of max) - lowered from 80%
    /// </summary>
    public bool IsFullyDetected() => currentDetection >= maxDetection * 0.7f;

    /// <summary>
    /// Checks if detection is at alert threshold (30% of max) - lowered from 40%
    /// </summary>
    public bool IsAlerted() => currentDetection >= maxDetection * 0.3f;
}
