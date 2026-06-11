using UnityEngine;

public class PlayerStamina : MonoBehaviour
{
    public static PlayerStamina Instance { get; private set; }

    [Header("Bars")]
    [SerializeField] private RectTransform staminaBar;      
    [SerializeField] private RectTransform regenDelayBar;   
    [SerializeField] private CanvasGroup staminaUI;

    [Header("Stamina")]
    [SerializeField] private float maxStamina = 5f;
    public float stamina;

    [Header("Regen")]
    [SerializeField] private float staminaRegenDelay = 2f;
    [SerializeField] private float staminaRegenPerSecond = 1f;

    [Header("Fade")]
    [SerializeField] private float fadeSpeed = 5f;

    private float staminaRegenTimer = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        stamina = maxStamina;
    }

    private void Update()
    {
        HandleRegen();
        UpdateBars();
        HandleFade();
    }

    private void HandleRegen()
    {
        if (stamina < maxStamina)
        {
            if (staminaRegenTimer > 0f)
            {
                staminaRegenTimer -= Time.deltaTime;
            }
            else
            {
                RegenerateStamina(staminaRegenPerSecond * Time.deltaTime);
            }
        }
    }

    private void UpdateBars()
    {
        float staminaPercent = stamina / maxStamina;
        staminaBar.localScale = new Vector3(staminaPercent, 1f, 1f);

        float delayPercent = staminaRegenTimer / staminaRegenDelay;

        regenDelayBar.localScale = new Vector3(delayPercent, 1f, 1f);
    }

    private void HandleFade()
    {
        bool shouldHide = stamina >= maxStamina;

        float targetAlpha = shouldHide ? 0f : 1f;

        staminaUI.alpha = Mathf.Lerp(
            staminaUI.alpha,
            targetAlpha,
            Time.deltaTime * fadeSpeed
        );
    }

    public void ConsumeStamina(float amount)
    {
        stamina -= amount;
        stamina = Mathf.Clamp(stamina, 0f, maxStamina);

        staminaRegenTimer = staminaRegenDelay;
    }

    public void RegenerateStamina(float amount)
    {
        stamina += amount;
        stamina = Mathf.Clamp(stamina, 0f, maxStamina);
    }
}