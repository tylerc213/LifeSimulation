// -----------------------------------------------------------------------------
// PredatorGenetics.cs
// Reads a Genome and modifies Predator stats and behaviours.
// Attach to every Predator prefab alongside the Predator script.
// -----------------------------------------------------------------------------
using UnityEngine;

/// <summary>
/// Applies genetic traits to a Predator.
/// </summary>
[RequireComponent(typeof(Predator))]
public class PredatorGenetics : MonoBehaviour
{
    public Genome Genome { get; private set; }

    public float SpeedMultiplier { get; private set; } = 1f;
    public float HealthMultiplier { get; private set; } = 1f;
    public float DamageMultiplier { get; private set; } = 1f;

    public bool IsVenomous { get; private set; } = false;
    public bool IsAmbusher { get; private set; } = false;
    public bool IsHerdHunter { get; private set; }
    public bool IsApexPredator { get; private set; } = false;

    public const float VenomDamagePerSec = 5f;
    public const float AmbushDamageBonus = 1.5f;
    public const float ApexMult = 5.0f;

    private SpriteRenderer _sr;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (Genome == null)
            Init(global::Genome.RandomPredator());
    }

    public void Init(Genome genome)
    {
        Genome = genome;
        ApplyTraits();
    }

    private void ApplyTraits()
    {
        float exprStat = ExpressionStrengthRuntime.NormalizedStrength(ExpressionStrengthRuntime.PredatorStat);
        float exprRare = ExpressionStrengthRuntime.NormalizedStrength(ExpressionStrengthRuntime.PredatorRare);
        float exprApex = ExpressionStrengthRuntime.NormalizedStrength(ExpressionStrengthRuntime.PredatorApex);

        // ── Nimble (dominant) ──────────────────────────────────────────────
        if (Genome.IsExpressed(TraitType.PredatorNimble) && exprStat > 0f)
            SpeedMultiplier *= 1f + 0.2f * exprStat;

        // ── Strong (dominant) ──────────────────────────────────────────────
        if (Genome.IsExpressed(TraitType.PredatorStrong) && exprStat > 0f)
            DamageMultiplier *= 1f + 0.2f * exprStat;

        // ── Thick-Skinned (dominant) ───────────────────────────────────────
        if (Genome.IsExpressed(TraitType.PredatorThickSkinned) && exprStat > 0f)
            HealthMultiplier *= 1f + 0.2f * exprStat;

        // ── Venomous (recessive) ───────────────────────────────────────────
        IsVenomous = Genome.IsExpressed(TraitType.Venomous) && exprRare > 0f;
        if (IsVenomous && _sr != null)
            _sr.color = new Color(0.4f, 0.8f, 0.3f);  // green tint

        // ── Ambusher (recessive) ───────────────────────────────────────────
        IsAmbusher = Genome.IsExpressed(TraitType.Ambusher) && exprRare > 0f;

        IsHerdHunter = Genome.IsExpressed(TraitType.HerdHunter) && exprRare > 0f;

        // ── Apex Predator (recessive) ──────────────────────────────────────
        if (Genome.IsExpressed(TraitType.ApexPredator) && exprApex > 0f)
        {
            IsApexPredator = true;
            float apexBlend = Mathf.Clamp01(exprApex);
            float apexFactor = Mathf.Lerp(1f, ApexMult, apexBlend);
            SpeedMultiplier *= apexFactor;
            HealthMultiplier *= apexFactor;
            DamageMultiplier *= apexFactor;
            if (_sr != null) _sr.color = new Color(0.9f, 0.1f, 0.1f);
        }

        // Apply health multiplier to EntityBase
        EntityBase entity = GetComponent<EntityBase>();
        if (entity != null)
        {
            entity.ApplyHealthMultiplier(HealthMultiplier);
            entity.RefreshBaseColor();   // ensure flash returns to genetics tint
        }
    }


}
