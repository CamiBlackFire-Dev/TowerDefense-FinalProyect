using UnityEngine;

public class TowerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    private float currentHealth;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        currentHealth = Mathf.Clamp(
            currentHealth,
            0f,
            maxHealth
        );

        Debug.Log(
            $"{gameObject.name} recibió {damage} de daño. " +
            $"Vida: {currentHealth}/{maxHealth}"
        );

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Repair(float amount)
    {
        currentHealth += amount;

        currentHealth = Mathf.Clamp(
            currentHealth,
            0f,
            maxHealth
        );

        Debug.Log(
            $"{gameObject.name} reparada. " +
            $"Vida: {currentHealth}/{maxHealth}"
        );
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} fue destruida.");

        // Después podemos poner aquí
        // animación, partículas, etc.
    }
}