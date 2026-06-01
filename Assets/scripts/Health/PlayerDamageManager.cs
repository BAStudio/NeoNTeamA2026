using UnityEngine;
using UnityEngine.SceneManagement;
public class PlayerDamageManager : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;
    [SerializeField] private Scene targetScene;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        Debug.Log("Player took damage: " + amount);

        if (currentHealth <= 0)
        {
            Die();
        }

        if(currentHealth != maxHealth)
        {
            Debug.Log("Current Health: " + currentHealth);
        }
    }

    private void Die()
    {
        Debug.Log("Player died.");

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

    }

    public void KillAndRestart()
    {
        Die();

    }
}