using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 6;
    public int Current { get; private set; }
    public event Action<int, int> OnChanged;

    void Awake() => Current = maxHealth;

    public void TakeDamage(int amount)
    {
        Current = Mathf.Max(0, Current - amount);
        OnChanged?.Invoke(Current, maxHealth);
    }
}
