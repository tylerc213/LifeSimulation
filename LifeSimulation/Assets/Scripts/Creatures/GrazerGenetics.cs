// -----------------------------------------------------------------------------
// Project:     EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:        Grazer Subtypes
// Requirement: Lifeform Simulation
// Author:      Luke Kivett
// Date:        04/06/2026
// Version:     0.0.0
//
// Description:
//    Reads a grazer Genome and applies trait effects to movement speed, health,
//    damage, and special behaviours. Colors stack additively per expressed trait.
// -----------------------------------------------------------------------------

using UnityEngine;

/// <summary>Applies genetic trait effects to a Grazer at spawn time.</summary>
/// <remarks>
/// Colors are additive from a white base sprite. Each expressed trait shifts
/// specific RGB channels. Herd Leader overrides all color with gold.
/// Call Init immediately after instantiation to supply an inherited genome.
/// </remarks>
[RequireComponent(typeof(Grazer))]
public class GrazerGenetics : MonoBehaviour
{
    /// <summary>The genome currently applied to this grazer.</summary>
    public Genome Genome { get; private set; }
    
    /// <summary>Multiplier applied to movement and flee speed.</summary>
    public float SpeedMultiplier { get; private set; } = 1f;

    /// <summary>Multiplier applied to max health via EntityBase.</summary>
    public float HealthMultiplier { get; private set; } = 1f;

    /// <summary>Multiplier applied to outgoing damage.</summary>
    public float DamageMultiplier { get; private set; } = 1f;

    /// <summary>True when the Camouflage gene is expressed.</summary>
    public bool HasCamouflage { get; private set; } = false;

    /// <summary>True when the Spiky gene is expressed.</summary>
    public bool HasSpiky { get; private set; } = false;

    /// <summary>True when the HerdMentality gene is expressed.</summary>
    public bool HasHerdMentality { get; private set; }

    /// <summary>True when this grazer has claimed the Herd Leader role.</summary>
    public bool HasHerdLeader { get; private set; }

    /// <summary>True when this grazer is a reptile.</summary>
    public bool IsReptile { get; private set; } = false;

    /// <summary>Probability that camouflage prevents predator detection when near an object.</summary>
    public const float CamouflageChance = 0.75f;

    /// <summary>Fraction of incoming damage reflected back to the attacker by Spiky.</summary>
    public const float SpikyReflect = 0.50f;

    /// <summary>Stat multiplier applied to all Herd Leader stats.</summary>
    public const float HerdLeaderMult = 2.0f;

    private SpriteRenderer _sr;

    /// <summary>Assigns a random genome if none has been provided via Init.</summary>
    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (Genome == null)
            Init(global::Genome.RandomGrazer());
    }

    /// <summary>Applies a specific genome to this grazer, overriding any previous assignment.</summary>
    /// <param name="genome">Genome to apply.</param>
    public void Init(Genome genome)
    {
        Genome = genome;
        ApplyTraits();
    }

    /// <summary>Reads each gene and applies stat multipliers, flags, and additive color tints.</summary>
    private void ApplyTraits()
    {
        float exprStat = ExpressionStrengthRuntime.NormalizedStrength(ExpressionStrengthRuntime.GrazerStat);
        float exprRare = ExpressionStrengthRuntime.NormalizedStrength(ExpressionStrengthRuntime.GrazerRare);
        float exprPack = ExpressionStrengthRuntime.NormalizedStrength(ExpressionStrengthRuntime.GrazerPack);

        if (Genome.IsExpressed(TraitType.GrazerNimble) && exprStat > 0f)
            SpeedMultiplier *= 1f + 0.2f * exprStat;
        if (Genome.IsExpressed(TraitType.GrazerStrong) && exprStat > 0f)
            DamageMultiplier *= 1f + 0.2f * exprStat;
        if (Genome.IsExpressed(TraitType.GrazerThickSkinned) && exprStat > 0f)
            HealthMultiplier *= 1f + 0.2f * exprStat;

        HasCamouflage = Genome.IsExpressed(TraitType.Camouflage) && exprRare > 0f;
        HasSpiky = Genome.IsExpressed(TraitType.Spiky) && exprRare > 0f;
        HasHerdMentality = Genome.IsExpressed(TraitType.HerdMentality) && exprPack > 0f;
        HasHerdLeader = Genome.IsExpressed(TraitType.HerdLeader) && exprPack > 0f;
        IsReptile = Genome.IsExpressed(TraitType.Reptile);
        // Visual feedback: Give reptiles a scaly yellow/brown tint
        if (IsReptile && _sr != null)
        {
            _sr.color = Color.Lerp(_sr.color, new Color(0.6f, 0.6f, 0.2f), 0.5f);
        }

        EntityBase entity = GetComponent<EntityBase>();
        if (entity != null)
        {
            entity.ApplyHealthMultiplier(HealthMultiplier);
            entity.RefreshBaseColor();
        }
    }
}
