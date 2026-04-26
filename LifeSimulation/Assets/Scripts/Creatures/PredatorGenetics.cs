// -----------------------------------------------------------------------------
// Project:     EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:        Predator Subtypes
// Requirement: Lifeform Simulation
// Author:      Luke Kivett
// Date:        04/06/2026
// Version:     0.0.0
//
// Description:
//    Reads a predator Genome and applies trait effects to movement speed, health,
//    damage, and special behaviours. Colors stack additively per expressed trait.
// -----------------------------------------------------------------------------

using UnityEngine;

/// <summary>Applies genetic trait effects to a Predator at spawn time.</summary>
/// <remarks>
/// Colors are additive from a white base sprite. Apex Predator overrides all
/// color with bright red. Call Init immediately after instantiation to supply
/// an inherited genome.
/// </remarks>
[RequireComponent(typeof(Predator))]
public class PredatorGenetics : MonoBehaviour
{
    /// <summary>The genome currently applied to this predator.</summary>
    public Genome Genome { get; private set; }

    /// <summary>Multiplier applied to hunt and patrol speed.</summary>
    public float SpeedMultiplier { get; private set; } = 1f;

    /// <summary>Multiplier applied to max health via EntityBase.</summary>
    public float HealthMultiplier { get; private set; } = 1f;

    /// <summary>Multiplier applied to outgoing attack damage.</summary>
    public float DamageMultiplier { get; private set; } = 1f;

    /// <summary>True when the Venomous gene is expressed.</summary>
    public bool IsVenomous { get; private set; } = false;

    /// <summary>True when the Ambusher gene is expressed.</summary>
    public bool IsAmbusher { get; private set; } = false;

    /// <summary>True when the HerdHunter gene is expressed.</summary>
    public bool IsHerdHunter { get; private set; }

    /// <summary>True when this predator has the Apex Predator gene expressed.</summary>
    public bool IsApexPredator { get; private set; } = false;

    /// <summary>True when this predator has the Night Vision gene expressed.</summary>
    public bool HasNightVision { get; private set; } = false;

    /// <summary>True when this predator has the Reptile gene expressed.</summary>
    public bool IsReptile { get; private set; } = false;

    /// <summary>Poison damage per second applied to a struck target.</summary>
    public const float VenomDamagePerSec = 5f;

    /// <summary>Damage multiplier applied when attacking an unaware target.</summary>
    public const float AmbushDamageBonus = 1.5f;

    /// <summary>Stat multiplier applied to all Apex Predator stats.</summary>
    public const float ApexMult = 5.0f;

    private SpriteRenderer _sr;

    /// <summary>Assigns a random genome if none has been provided via Init.</summary>
    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (Genome == null)
            Init(global::Genome.RandomPredator());
    }

    /// <summary>Applies a specific genome to this predator, overriding any previous assignment.</summary>
    /// <param name="genome">Genome to apply.</param>
    public void Init(Genome genome)
    {
        Genome = genome;
        ApplyTraits();
    }
    /// <summary>Reads each gene and applies stat multipliers, flags, and additive color tints.</summary>
    private void ApplyTraits()
    {
        float exprStat = ExpressionStrengthRuntime.NormalizedStrength(ExpressionStrengthRuntime.PredatorStat);
        float exprRare = ExpressionStrengthRuntime.NormalizedStrength(ExpressionStrengthRuntime.PredatorRare);
        float exprApex = ExpressionStrengthRuntime.NormalizedStrength(ExpressionStrengthRuntime.PredatorApex);

        if (Genome.IsExpressed(TraitType.PredatorNimble) && exprStat > 0f)
            SpeedMultiplier *= 1f + 0.2f * exprStat;

        if (Genome.IsExpressed(TraitType.PredatorStrong) && exprStat > 0f)
            DamageMultiplier *= 1f + 0.2f * exprStat;

        if (Genome.IsExpressed(TraitType.PredatorThickSkinned) && exprStat > 0f)
            HealthMultiplier *= 1f + 0.2f * exprStat;

        IsVenomous = Genome.IsExpressed(TraitType.Venomous) && exprRare > 0f;
        if (IsVenomous && _sr != null)
            _sr.color = new Color(0.4f, 0.8f, 0.3f);  // green tint

        IsAmbusher = Genome.IsExpressed(TraitType.Ambusher) && exprRare > 0f;

        IsHerdHunter = Genome.IsExpressed(TraitType.HerdHunter) && exprRare > 0f;

        // Apex Predator overrides all other color and multiplies all stats
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

        HasNightVision = Genome.IsExpressed(TraitType.NightVision) && exprRare > 0f;
        // Visual feedback: Give them glowing yellow eyes or a purple tint if they have Night Vision
        if (HasNightVision && _sr != null)
        {
            if (!IsVenomous) _sr.color = new Color(0.6f, 0.4f, 0.9f);
        }

        IsReptile = Genome.IsExpressed(TraitType.Reptile);
        // Visual feedback: Give reptiles a scaly yellow/brown tint
        if (IsReptile && _sr != null)
        {
            _sr.color = Color.Lerp(_sr.color, new Color(0.7f, 0.7f, 0.2f), 0.5f);
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
