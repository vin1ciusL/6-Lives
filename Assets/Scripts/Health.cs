using UnityEngine;

public class Health : MonoBehaviour
{
    public int maxHealth = 1;
    int current;

    void Awake() => current = maxHealth;

    public void TakeDamage(int amount)
    {
        current -= amount;
        if (current <= 0) Destroy(gameObject);
    }
}
