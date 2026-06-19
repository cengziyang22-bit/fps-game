using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public float regenDelay = 5f;
    public float regenRate = 15f;

    [HideInInspector] public int currentHealth;

    private float lastDamageTime = float.MinValue;

    void Start()
    {
        currentHealth = maxHealth;
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.gameOver) return;

        if (Time.time - lastDamageTime > regenDelay && currentHealth < maxHealth)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + Mathf.CeilToInt(regenRate * Time.deltaTime));
            UIManager.Instance?.UpdateHealth(currentHealth, maxHealth);
        }
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        lastDamageTime = Time.time;
        UIManager.Instance?.UpdateHealth(currentHealth, maxHealth);
        UIManager.Instance?.TriggerDamageFlash();
        UIManager.Instance?.TriggerShake(0.1f);

        if (currentHealth <= 0)
            GameManager.Instance?.EndGame();
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        lastDamageTime = float.MinValue;
        UIManager.Instance?.UpdateHealth(currentHealth, maxHealth);
    }
}
