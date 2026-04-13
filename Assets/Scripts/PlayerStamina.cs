using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayerStamina : MonoBehaviour
{
    [SerializeField] private GameObject staminaBar;
    public static PlayerStamina Instance;

    [SerializeField] private float maxStamina = 5f;
    public float stamina;

    [SerializeField] private float staminaRegenDelay = 2f;
    private float staminaRegenTimer = 0f;

    private void Awake()
    {
        Instance = this;
        stamina = maxStamina;
    }

    private void Update()
    {
        staminaBar.transform.localScale = new Vector3(stamina / maxStamina, stamina / maxStamina, 1f);
        if (stamina < maxStamina && staminaRegenTimer <= 0)
        {
            RegenerateStamina(1f * Time.deltaTime);
        }
        else
        {
            staminaRegenTimer -= Time.deltaTime;
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
