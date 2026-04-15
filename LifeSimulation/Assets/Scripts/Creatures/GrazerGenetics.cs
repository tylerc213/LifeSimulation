// -----------------------------------------------------------------------------
// GrazerGenetics.cs
// Reads a Genome and modifies Grazer stats and behaviours.
// Attach to every Grazer prefab alongside the Grazer script.
// -----------------------------------------------------------------------------
using UnityEngine;

/// <summary>
/// Applies genetic traits to a Grazer.
/// Modifies speed, health, damage resistance, and special behaviours.
/// </summary>
[RequireComponent(typeof(Grazer))]
public class GrazerGenetics : MonoBehaviour
{
    public Genome Genome { get; private set; }

    // Stat multipliers read by Grazer.cs
    public float SpeedMultiplier { get; private set; } = 1f;
    public float HealthMultiplier { get; private set; } = 1f;
    public float DamageMultiplier { get; private set; } = 1f;

    // Special trait flags read by Grazer.cs
    public bool HasCamouflage { get; private set; } = false;
    public bool HasSpiky { get; private set; } = false;
    public bool HasHerdMentality { get; private set; } = false;
    public bool IsHerdLeader { get; private set; } = false;

    public const float CamouflageChance = 0.75f;
    public const float SpikyReflect = 0.50f;
    public const float HerdLeaderMult = 2.0f;

    public static float EffectiveCamouflageChance => CamouflageChance * ExpressionStrengthRuntime.GrazerRare;
    public static float EffectiveSpikyReflect => SpikyReflect * ExpressionStrengthRuntime.GrazerRare;

    private SpriteRenderer _sr;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (Genome == null)
            Init(global::Genome.RandomGrazer());
    }

    public void Init(Genome genome)
    {
        Genome = genome;
        ApplyTraits();
    }

    private void ApplyTraits()
    {
        // ── Nimble (dominant) ──────────────────────────────────────────────
        if (Genome.IsExpressed(TraitType.GrazerNimble))
            SpeedMultiplier *= 1.2f;

        // ── Strong (dominant) ──────────────────────────────────────────────
        if (Genome.IsExpressed(TraitType.GrazerStrong))
            DamageMultiplier *= 1.2f;

        // ── Thick-Skinned (dominant) ───────────────────────────────────────
        if (Genome.IsExpressed(TraitType.GrazerThickSkinned))
            HealthMultiplier *= 1.2f;

        float st = ExpressionStrengthRuntime.GrazerStat;
        SpeedMultiplier = 1f + (SpeedMultiplier - 1f) * st;
        DamageMultiplier = 1f + (DamageMultiplier - 1f) * st;
        HealthMultiplier = 1f + (HealthMultiplier - 1f) * st;

        // ── Camouflage (recessive) ─────────────────────────────────────────
        HasCamouflage = Genome.IsExpressed(TraitType.Camouflage);

        // ── Spiky (recessive) ──────────────────────────────────────────────
        HasSpiky = Genome.IsExpressed(TraitType.Spiky);
        if (HasSpiky && _sr != null)
            _sr.color = new Color(0.6f, 0.6f, 0.6f);

        // ── Herd Mentality (recessive) ─────────────────────────────────────
        HasHerdMentality = Genome.IsExpressed(TraitType.HerdMentality);

        // ── Herd Leader (recessive) ────────────────────────────────────────
        // Only one leader allowed at a time — GrazerPack enforces this
        if (Genome.IsExpressed(TraitType.HerdLeader) && GrazerPack.TryBecomeLeader(this))
        {
            IsHerdLeader = true;
            float packMult = HerdLeaderMult * ExpressionStrengthRuntime.GrazerPack;
            SpeedMultiplier *= packMult;
            HealthMultiplier *= packMult;
            DamageMultiplier *= packMult;
            if (_sr != null) _sr.color = new Color(1f, 0.85f, 0.1f);  // gold tint
        }

        // Apply health multiplier to EntityBase
        EntityBase entity = GetComponent<EntityBase>();
        if (entity != null)
            entity.ApplyHealthMultiplier(HealthMultiplier);
    }
}