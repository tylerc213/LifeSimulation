using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Shared base for Grazer and Predator.
/// Manages health, hunger drain, death, hit-flash, and starvation pulse visuals.
/// </summary>
public class EntityBase : MonoBehaviour
{
    [Header("Vitals")]
    [SerializeField] protected float maxHealth = 100f;
    [SerializeField] protected float maxHunger = 100f;
    [SerializeField] protected float hungerDrainRate = 5f;

    [Header("Hit Flash")]
    [SerializeField] private float flashDuration = 0.12f;

    [Header("Starvation Visual")]
    [SerializeField] private float starvationPulseSpeed = 2.5f;  // pulses per second
    [SerializeField] private float starvationPulseMin = 0.4f;  // darkest alpha multiplier

    [Header("Events")]
    public UnityEvent OnDeath;

    public float Health { get; protected set; }
    public float Hunger { get; protected set; }
    public bool IsDead { get; private set; }

    private SpriteRenderer _sr;
    private Color _baseColor;
    private Coroutine _flashCoroutine;
    private bool _isStarving = false;

    protected virtual void Awake()
    {
        Health = maxHealth;
        Hunger = maxHunger * 0.5f;
        _sr = GetComponent<SpriteRenderer>();
        _baseColor = _sr != null ? _sr.color : Color.white;
    }

    public void ApplyHealthMultiplier(float multiplier)
    {
        maxHealth = maxHealth * multiplier;
        Health = maxHealth;
    }

    protected virtual void Update()
    {
        if (IsDead) return;

        Hunger -= hungerDrainRate * Time.deltaTime;
        Hunger = Mathf.Clamp(Hunger, 0f, maxHunger);

        bool starving = Hunger <= 0f;

        if (starving)
        {
            // Use silent damage — no hit flash for hunger ticks
            TakeDamageSilent(hungerDrainRate * Time.deltaTime);
        }

        // Update starvation pulse visual
        if (starving != _isStarving)
        {
            _isStarving = starving;
            if (!starving) RestoreBaseColor();  // immediately restore when fed
        }

        if (_isStarving && _sr != null && _flashCoroutine == null)
            ApplyStarvationPulse();
    }

    // ── Damage ────────────────────────────────────────────────────────────────

    /// <summary>Damage with hit flash — use for attacks and combat.</summary>
    public virtual void TakeDamage(float amount)
    {
        if (IsDead) return;
        Health = Mathf.Max(0f, Health - amount);
        FlashHit();
        if (Health <= 0f) Die();
    }

    /// <summary>Damage without hit flash — used for starvation and poison ticks.</summary>
    public virtual void TakeDamageSilent(float amount)
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

    // ── Hit Flash ─────────────────────────────────────────────────────────────

    public void FlashHit()
    {
        if (_sr == null) return;
        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(FlashCoroutine());
    }

    public void RefreshBaseColor()
    {
        if (_sr != null) _baseColor = _sr.color;
    }

    private IEnumerator FlashCoroutine()
    {
        if (_sr == null) yield break;
        _sr.color = Color.red;
        yield return new WaitForSeconds(flashDuration);
        if (!IsDead && _sr != null)
            _sr.color = _isStarving ? GetStarvationColor() : _baseColor;
        _flashCoroutine = null;
    }

    // ── Starvation Pulse ──────────────────────────────────────────────────────

    private void ApplyStarvationPulse()
    {
        if (_sr == null || IsDead) return;
        // Only update if no flash is running
        if (_flashCoroutine == null)
            _sr.color = GetStarvationColor();
    }

    private Color GetStarvationColor()
    {
        // Pulse between base color and an orange-tinted darker version
        float pulse = (Mathf.Sin(Time.time * starvationPulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
        float factor = Mathf.Lerp(starvationPulseMin, 1f, pulse);
        Color orange = Color.Lerp(_baseColor, new Color(1f, 0.4f, 0f), 0.5f);
        return Color.Lerp(orange * factor, _baseColor * factor, pulse);
    }

    private void RestoreBaseColor()
    {
        if (_sr != null && _flashCoroutine == null)
            _sr.color = _baseColor;
    }
}