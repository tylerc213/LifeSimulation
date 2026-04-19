using UnityEngine;

/// <summary>
/// Applies genetic traits to a Grazer.
/// Colors are additive — each expressed trait contributes RGB components
/// that blend together into a final tint on the white base sprite.
/// </summary>
[RequireComponent(typeof(Grazer))]
public class GrazerGenetics : MonoBehaviour
{
    public Genome Genome { get; private set; }

    public float SpeedMultiplier { get; private set; } = 1f;
    public float HealthMultiplier { get; private set; } = 1f;
    public float DamageMultiplier { get; private set; } = 1f;

    public bool HasCamouflage { get; private set; } = false;
    public bool HasSpiky { get; private set; } = false;
    public bool HasHerdMentality { get; private set; }
    public bool HasHerdLeader { get; private set; }

    public const float CamouflageChance = 0.75f;
    public const float SpikyReflect = 0.50f;
    public const float HerdLeaderMult = 2.0f;

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
        float exprStat = ExpressionStrengthRuntime.NormalizedStrength(ExpressionStrengthRuntime.GrazerStat);
        float exprRare = ExpressionStrengthRuntime.NormalizedStrength(ExpressionStrengthRuntime.GrazerRare);
        float exprPack = ExpressionStrengthRuntime.NormalizedStrength(ExpressionStrengthRuntime.GrazerPack);

        // ── Stats ──────────────────────────────────────────────────────────────
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

        EntityBase entity = GetComponent<EntityBase>();
        if (entity != null)
        {
            entity.ApplyHealthMultiplier(HealthMultiplier);
            entity.RefreshBaseColor();
        }
    }
}
