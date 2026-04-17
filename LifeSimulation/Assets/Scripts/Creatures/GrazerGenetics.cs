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
        // ── Stats ──────────────────────────────────────────────────────────────
        if (Genome.IsExpressed(TraitType.GrazerNimble)) SpeedMultiplier *= 1.2f;
        if (Genome.IsExpressed(TraitType.GrazerStrong)) DamageMultiplier *= 1.2f;
        if (Genome.IsExpressed(TraitType.GrazerThickSkinned)) HealthMultiplier *= 1.2f;

        HasCamouflage = Genome.IsExpressed(TraitType.Camouflage);
        HasSpiky = Genome.IsExpressed(TraitType.Spiky);

        EntityBase entity = GetComponent<EntityBase>();
        if (entity != null)
        {
            entity.ApplyHealthMultiplier(HealthMultiplier);
            entity.RefreshBaseColor();
        }
    }
}