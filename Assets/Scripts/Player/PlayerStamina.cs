using UnityEngine;

public class PlayerStamina : MonoBehaviour
{
    public static PlayerStamina Instance { get; private set; }

    [SerializeField] private GameObject staminaBar;

    [SerializeField, Min(0.1f)] private float maxStamina = 5f;
    public float stamina;

    [SerializeField, Min(0f)] private float staminaRegenDelay = 2f;
    [SerializeField, Min(0f)] private float staminaRegenPerSecond = 1f;

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
        if (staminaBar != null)
        {
            float scale = stamina / maxStamina;
            staminaBar.transform.localScale = new Vector3(scale, scale, 1f);
        }

        if (stamina < maxStamina)
        {
            if (staminaRegenTimer <= 0f)
            {
                RegenerateStamina(staminaRegenPerSecond * Time.deltaTime);
            }
            else
            {
                staminaRegenTimer -= Time.deltaTime;
            }
        }
    }

    public void ConsumeStamina(float amount)
    {
        staminaRegenTimer = staminaRegenDelay;
        stamina = Mathf.Max(stamina - amount, 0f);
    }

    public void RegenerateStamina(float amount)
    {
        stamina = Mathf.Min(stamina + amount, maxStamina);
    }
}