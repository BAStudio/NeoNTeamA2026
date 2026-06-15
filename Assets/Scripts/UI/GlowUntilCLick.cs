using UnityEngine;
using UnityEngine.UI;

public class GlowUntilTab : MonoBehaviour
{
    [Header("UI Image")]
    public Image targetImage;

    [Header("Glow Settings")]
    public Color glowColor = Color.yellow;
    public float glowSpeed = 2f;
    public float glowIntensity = 0.5f;

    private Color originalColor;
    private bool stopGlow = false;

    void Start()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }

        originalColor = targetImage.color;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            stopGlow = true;
            targetImage.color = originalColor;
            return;
        }

        if (!stopGlow)
        {
            float t = (Mathf.Sin(Time.time * glowSpeed) + 1f) / 2f;

            targetImage.color = Color.Lerp(
                originalColor,
                glowColor,
                t * glowIntensity
            );
        }
    }
}