using UnityEngine;

public class TowerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    private float currentHealth;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        
        currentHealth = Mathf.Max(currentHealth, 0f);

        Debug.Log($"Torre {gameObject.name}: {currentHealth}/{maxHealth} HP");

        if (currentHealth <= 0f)
        {
            DestroyTower();
        }
    }

    private void DestroyTower()
    {
        Destroy(gameObject);
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    public float GetMaxHealth()
    {
        return maxHealth;
    }

    public void Heal(float amount)
    {
        currentHealth += amount;

        currentHealth = Mathf.Min(currentHealth, maxHealth);

        Debug.Log($"Torre reparada: {currentHealth}/{maxHealth}");
    }
}