using UnityEngine;

public class DetectionMeter : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float maxDetection = 100f;

    [SerializeField] private float patrolDecay = 6f;
    [SerializeField] private float alertDecay = 3f;
    [SerializeField] private float stalkingDecay = 1.25f;

    [SerializeField] private float dangerThreshold = 45f;

    private float currentDetection;

    public void Tick(AIStateManager.AIState state, float dt)
    {
        float decay = patrolDecay;

        switch (state)
        {
            case AIStateManager.AIState.Alert:
                decay = alertDecay;
                break;

            case AIStateManager.AIState.Stalking:
            case AIStateManager.AIState.Chase:
                decay = stalkingDecay;
                break;
        }

        currentDetection -= decay * dt;
        currentDetection = Mathf.Clamp(currentDetection, 0f, maxDetection);

        UpdateMusicState();
    }

    public void AddDetection(float amount)
    {
        currentDetection += amount;
        currentDetection = Mathf.Clamp(currentDetection, 0f, maxDetection);

        UpdateMusicState();
    }

    public void ResetDetection()
    {
        currentDetection = 0f;

        UpdateMusicState();
    }

    private void UpdateMusicState()
    {
        if (MusicManager.Instance == null)
            return;

        bool danger = currentDetection >= dangerThreshold;

        MusicManager.Instance.SetDangerState(danger);
    }

    public float GetDetection()
    {
        return currentDetection;
    }

    public float GetMaxDetection()
    {
        return maxDetection;
    }

    public bool IsAlerted()
    {
        return currentDetection >= maxDetection * 0.3f;
    }

    public bool IsStalking()
    {
        return currentDetection >= maxDetection * 0.5f;
    }

    public bool IsFullyDetected()
    {
        return currentDetection >= maxDetection * 0.7f;
    }
}