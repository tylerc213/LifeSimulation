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
    public bool HasHerdHunter { get; private set; } = false;
    public bool IsApexPredator { get; private set; } = false;

    public const float VenomDamagePerSec = 5f;
    public const float AmbushDamageBonus = 1.5f;
    public const float ApexMult = 5.0f;

    public static float EffectiveVenomDamagePerSec => VenomDamagePerSec * ExpressionStrengthRuntime.PredatorRare;
    public static float EffectiveAmbushDamageBonus => 1f + (AmbushDamageBonus - 1f) * ExpressionStrengthRuntime.PredatorRare;

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
        // ── Nimble (dominant) ──────────────────────────────────────────────
        if (Genome.IsExpressed(TraitType.PredatorNimble))
            SpeedMultiplier *= 1.2f;

        // ── Strong (dominant) ──────────────────────────────────────────────
        if (Genome.IsExpressed(TraitType.PredatorStrong))
            DamageMultiplier *= 1.2f;

        // ── Thick-Skinned (dominant) ───────────────────────────────────────
        if (Genome.IsExpressed(TraitType.PredatorThickSkinned))
            HealthMultiplier *= 1.2f;

        float st = ExpressionStrengthRuntime.PredatorStat;
        SpeedMultiplier = 1f + (SpeedMultiplier - 1f) * st;
        DamageMultiplier = 1f + (DamageMultiplier - 1f) * st;
        HealthMultiplier = 1f + (HealthMultiplier - 1f) * st;

        // ── Venomous (recessive) ───────────────────────────────────────────
        IsVenomous = Genome.IsExpressed(TraitType.Venomous);
        if (IsVenomous && _sr != null)
            _sr.color = new Color(0.4f, 0.8f, 0.3f);  // green tint

        // ── Ambusher (recessive) ───────────────────────────────────────────
        IsAmbusher = Genome.IsExpressed(TraitType.Ambusher);

        // ── Herd Hunter (recessive) ────────────────────────────────────────
        HasHerdHunter = Genome.IsExpressed(TraitType.HerdHunter);

        // ── Apex Predator (recessive) ──────────────────────────────────────
        // Only one apex predator allowed at a time — PredatorPack enforces this
        if (Genome.IsExpressed(TraitType.ApexPredator) && PredatorPack.CanBecomeApex())
        {
            IsApexPredator = true;
            float apexMult = ApexMult * ExpressionStrengthRuntime.PredatorApex;
            SpeedMultiplier *= apexMult;
            HealthMultiplier *= apexMult;
            DamageMultiplier *= apexMult;
            if (_sr != null) _sr.color = new Color(0.9f, 0.1f, 0.1f);  // red tint
            PredatorPack.RegisterApex(this);
        }

        // Apply health multiplier to EntityBase
        EntityBase entity = GetComponent<EntityBase>();
        if (entity != null)
            entity.ApplyHealthMultiplier(HealthMultiplier);
    }

    private void OnDestroy()
    {
        if (IsApexPredator)
            PredatorPack.UnregisterApex();
    }
}