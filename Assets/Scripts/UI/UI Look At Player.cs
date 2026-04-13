using UnityEngine;

public class UILookAtPlayer : MonoBehaviour
{
    [Header("Modes")]
    [SerializeField] private bool _lookAtPlayer;
    [SerializeField] private bool _snap90Degrees;

    [Header("Settings")]
    [SerializeField] private float _rotationSpeed = 10f;

    [Header("Scale Presets")]
    [SerializeField] private Vector3 _scaleForward = new Vector3(0.1f, 1f, 0.1f);
    [SerializeField] private Vector3 _scaleSide = new Vector3(1f, 0.1f, 1f);

    private float _currentYRotation;

    private void LateUpdate()
    {
        if (Camera.main == null) return;

        if (_snap90Degrees)
        {
            SnapLookAtPlayer();
            ApplyScale();
        }
        else if (_lookAtPlayer)
        {
            LookAtPlayer();
        }
    }

    private void LookAtPlayer()
    {
        transform.forward = Camera.main.transform.forward;
    }

    private void SnapLookAtPlayer()
    {
        Vector3 camForward = Camera.main.transform.forward;

        camForward.y = 0f;
        camForward.Normalize();

        float dotForward = Vector3.Dot(camForward, Vector3.forward);
        float dotRight = Vector3.Dot(camForward, Vector3.right);

        float yRotation;

        if (Mathf.Abs(dotForward) > Mathf.Abs(dotRight))
        {
            yRotation = (dotForward > 0) ? 0f : 180f;
        }
        else
        {
            yRotation = (dotRight > 0) ? 90f : -90f;
        }

        _currentYRotation = yRotation;

        Quaternion targetRotation = Quaternion.Euler(90f, yRotation, 0f);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * _rotationSpeed
        );
    }

    // 🔥 THIS IS THE KEY PART
    private void ApplyScale()
    {
        float snapped = Mathf.Round(_currentYRotation / 90f) * 90f;

        // Forward / Back
        if (snapped == 0f || Mathf.Abs(snapped) == 180f)
        {
            transform.localScale = _scaleForward;
        }
        else // Left / Right
        {
            transform.localScale = _scaleSide;
        }
    }
}

