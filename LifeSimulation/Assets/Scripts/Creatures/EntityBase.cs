// -----------------------------------------------------------------------------
// Project:     EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:        Lifeforms
// Requirement: Lifeform Simulation
// Author:      Luke Kivett
// Date:        4/6/2026
// Version:     0.0.0
//
// Description:
//    Shared base class for all living entities. Manages health, hunger drain,
//    death events, hit-flash feedback, and starvation pulse visuals.
// -----------------------------------------------------------------------------
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
 
/// <summary>Shared vitals and visual feedback base for Grazer and Predator.</summary>
public class EntityBase : MonoBehaviour
{
    [Header("Vitals")]
    [SerializeField] protected float maxHealth       = 100f;
    [SerializeField] protected float maxHunger       = 100f;
    [SerializeField] protected float hungerDrainRate = 5f;
 
    [Header("Hit Flash")]
    [SerializeField] private float flashDuration = 0.12f;
 
    [Header("Starvation Visual")]
    [SerializeField] private float starvationPulseSpeed = 2.5f;
    [SerializeField] private float starvationPulseMin   = 0.4f;
 
    [Header("Events")]
    public UnityEvent OnDeath;
 
    public float Health { get; protected set; }
    public float Hunger { get; protected set; }
    public bool  IsDead { get; private set; }
 
    private SpriteRenderer _sr;
    private Color          _baseColor;
    private Coroutine      _flashCoroutine;
    private bool           _isStarving = false;
 
    /// <summary>Initialises vitals and caches the sprite renderer base color.</summary>
    protected virtual void Awake()
    {
        Health     = maxHealth;
        Hunger     = maxHunger * 0.5f;
        _sr        = GetComponent<SpriteRenderer>();
        _baseColor = _sr != null ? _sr.color : Color.white;
    }
 
    /// <summary>Scales max health by a multiplier; must be called before combat begins.</summary>
    /// <param name="multiplier">Scale factor applied to max health.</param>
    public void ApplyHealthMultiplier(float multiplier)
    {
        maxHealth = maxHealth * multiplier;
        Health    = maxHealth;
    }
 
    /// <summary>Drains hunger each frame and applies starvation damage and visuals.</summary>
    protected virtual void Update()
    {
        if (IsDead) return;
 
        Hunger -= hungerDrainRate * Time.deltaTime;
        Hunger  = Mathf.Clamp(Hunger, 0f, maxHunger);
 
        bool starving = Hunger <= 0f;
 
        if (starving)
        {
            // Use silent damage so hunger ticks don't trigger the hit flash
            TakeDamageSilent(hungerDrainRate * Time.deltaTime);
        }
 
        // Toggle starvation pulse when state changes
        if (starving != _isStarving)
        {
            _isStarving = starving;
            if (!starving) RestoreBaseColor();
        }
 
        if (_isStarving && _sr != null && _flashCoroutine == null)
            ApplyStarvationPulse();
    }
 
    /// <summary>Applies damage and triggers a hit flash visual.</summary>
    /// <param name="amount">Damage amount to apply.</param>
    public virtual void TakeDamage(float amount)
    {
        if (IsDead) return;
        Health = Mathf.Max(0f, Health - amount);
        FlashHit();
        if (Health <= 0f) Die();
    }
 
    /// <summary>Applies damage without any visual feedback; used for continuous effects.</summary>
    /// <param name="amount">Damage amount to apply.</param>
    public virtual void TakeDamageSilent(float amount)
    {
        if (IsDead) return;
        Health = Mathf.Max(0f, Health - amount);
        if (Health <= 0f) Die();
    }
 
    /// <summary>Restores health up to the maximum value.</summary>
    /// <param name="amount">Amount of health to restore.</param>
    public virtual void Heal(float amount)
    {
        if (IsDead) return;
        Health = Mathf.Min(maxHealth, Health + amount);
    }
 
    /// <summary>Restores hunger up to the maximum value.</summary>
    /// <param name="amount">Amount of hunger to restore.</param>
    public virtual void Feed(float amount)
    {
        Hunger = Mathf.Min(maxHunger, Hunger + amount);
    }
 
    /// <summary>Triggers death, fires the OnDeath event, and destroys the GameObject.</summary>
    protected virtual void Die()
    {
        if (IsDead) return;
        IsDead = true;
        OnDeath?.Invoke();
        Destroy(gameObject, 0.1f);
    }
 
    /// <summary>Briefly flashes the sprite red to signal a combat hit.</summary>
    public void FlashHit()
    {
        if (_sr == null) return;
        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(FlashCoroutine());
    }
 
    /// <summary>Updates the cached base color after genetics tinting is applied.</summary>
    public void RefreshBaseColor()
    {
        if (_sr != null) _baseColor = _sr.color;
    }
 
    /// <summary>Flashes red then restores the appropriate base or starvation color.</summary>
    private IEnumerator FlashCoroutine()
    {
        if (_sr == null) yield break;
        _sr.color = Color.red;
        yield return new WaitForSeconds(flashDuration);
        if (!IsDead && _sr != null)
            _sr.color = _isStarving ? GetStarvationColor() : _baseColor;
        _flashCoroutine = null;
    }
 
    /// <summary>Applies the pulsing orange starvation tint each frame.</summary>
    private void ApplyStarvationPulse()
    {
        if (_sr == null || IsDead) return;
        if (_flashCoroutine == null)
            _sr.color = GetStarvationColor();
    }
 
    /// <summary>Calculates the current starvation pulse color using a sine wave.</summary>
    /// <returns>Interpolated color between base and orange at current pulse phase.</returns>
    private Color GetStarvationColor()
    {
        float pulse  = (Mathf.Sin(Time.time * starvationPulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
        float factor = Mathf.Lerp(starvationPulseMin, 1f, pulse);
        Color orange = Color.Lerp(_baseColor, new Color(1f, 0.4f, 0f), 0.5f);
        return Color.Lerp(orange * factor, _baseColor * factor, pulse);
    }
 
    /// <summary>Restores the sprite to its base color when starvation ends.</summary>
    private void RestoreBaseColor()
    {
        if (_sr != null && _flashCoroutine == null)
            _sr.color = _baseColor;
    }
}
