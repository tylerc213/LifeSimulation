using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Shared base for Grazer and Predator (and optionally Plant).
/// Manages health, hunger drain, and death.  Inherit from this.
/// </summary>
public class EntityBase : MonoBehaviour
{
    [Header("Vitals")]
    [SerializeField] protected float maxHealth = 100f;
    [SerializeField] protected float maxHunger = 100f;   // 0 = starving
    [SerializeField] protected float hungerDrainRate = 5f; // per second

    [Header("Events")]
    public UnityEvent OnDeath;

    public float Health { get; protected set; }
    public float Hunger { get; protected set; }
    public bool IsDead { get; private set; }

    protected virtual void Awake()
    {
        Health = maxHealth;
        Hunger = maxHunger * 0.5f;  // start at 50% hunger instead of full
    }

    protected virtual void Update()
    {
        if (IsDead) return;

        // Drain hunger over time
        Hunger -= hungerDrainRate * Time.deltaTime;
        Hunger = Mathf.Clamp(Hunger, 0f, maxHunger);

        // Starving causes health loss
        if (Hunger <= 0f)
            TakeDamage(hungerDrainRate * Time.deltaTime);
    }

    public virtual void TakeDamage(float amount)
    {
        if (IsDead) return;
        Health = Mathf.Max(0f, Health - amount);
        if (Health <= 0f) Die();
    }

    public virtual void Heal(float amount)
    {
        if (IsDead) return;
        Health = Mathf.Min(maxHealth, Health + amount);
    }

    public virtual void Feed(float amount)
    {
        Hunger = Mathf.Min(maxHunger, Hunger + amount);
    }

    protected virtual void Die()
    {
        if (IsDead) return;
        IsDead = true;
        OnDeath?.Invoke();
        Destroy(gameObject, 0.1f);
    }
}