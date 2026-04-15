// -----------------------------------------------------------------------------
// PlantGenetics.cs
// Reads a Genome and modifies the Plant component accordingly.
// Attach to every Plant prefab alongside the Plant script.
// -----------------------------------------------------------------------------
using UnityEngine;

/// <summary>
/// Applies genetic traits to a Plant.
/// Call Init(genome) right after instantiation (EcosystemManager does this),
/// or it will self-initialise with a random genome in Awake.
/// </summary>
[RequireComponent(typeof(Plant))]
public class PlantGenetics : MonoBehaviour
{
    public Genome Genome { get; private set; }

    // Multipliers read by Plant.cs
    public float NutritionMultiplier { get; private set; } = 1f;
    public bool IsPoisonous { get; private set; } = false;
    public float PoisonDamagePerSec { get; private set; } = 3f;
    public float TastyMultiplier { get; private set; } = 1f;  // >1 = more likely eaten
    public float BitterMultiplier { get; private set; } = 1f;  // <1 = less likely eaten
    public bool IsResilient { get; private set; } = false;

    private Plant _plant;
    private SpriteRenderer _sr;

    private void Awake()
    {
        _plant = GetComponent<Plant>();
        _sr = GetComponent<SpriteRenderer>();

        if (Genome == null)
            Init(Genome.RandomPlant());
    }

    /// <summary>Call this immediately after Instantiate to provide a specific genome.</summary>
    public void Init(Genome genome)
    {
        Genome = genome;
        ApplyTraits();
    }

    private void ApplyTraits()
    {
        // ── Leaf Size ──────────────────────────────────────────────────────
        // Encoded via allele pair: AA=large, Aa=medium, aa=small
        Gene leafGene = Genome.Get(TraitType.LeafSize);
        if (leafGene != null)
        {
            int leafLevel = (leafGene.AlleleA ? 1 : 0) + (leafGene.AlleleB ? 1 : 0); // 0,1,2
            NutritionMultiplier = 0.7f + leafLevel * 0.3f;   // small=0.7, med=1.0, large=1.3
            float scaleBonus = 1f + leafLevel * 0.15f;
            transform.localScale *= scaleBonus;

            // Tint: small=yellow, medium=green, large=dark green
            if (_sr != null)
            {
                Color[] leafColors = {
                    new Color(0.85f, 0.85f, 0.2f),
                    new Color(0.2f,  0.75f, 0.2f),
                    new Color(0.05f, 0.45f, 0.05f)
                };
                _sr.color = leafColors[leafLevel];
            }
        }

        // ── Tasty (dominant) ───────────────────────────────────────────────
        if (Genome.IsExpressed(TraitType.Tasty))
            TastyMultiplier = 1.5f;

        // ── Bitter (recessive) ─────────────────────────────────────────────
        if (Genome.IsExpressed(TraitType.Bitter))
            BitterMultiplier = 0.4f;

        // ── Poisonous (recessive) ──────────────────────────────────────────
        // Poisonous overrides bitter/tasty — grazers really don't want to eat it
        if (Genome.IsExpressed(TraitType.Poisonous))
        {
            IsPoisonous = true;
            BitterMultiplier = 0.1f;   // strongly deters eating
            if (_sr != null) _sr.color = new Color(0.5f, 0.1f, 0.5f);  // purple tint
        }

        // ── Resilient (dominant) ───────────────────────────────────────────
        IsResilient = Genome.IsExpressed(TraitType.Resilient);

        // Global expression strength (settings UI)
        float p = ExpressionStrengthRuntime.PlantPrimary;
        float s = ExpressionStrengthRuntime.PlantSecondary;
        float d = ExpressionStrengthRuntime.PlantDefense;
        NutritionMultiplier *= p;
        if (Genome.IsExpressed(TraitType.Tasty))
            TastyMultiplier = 1f + (TastyMultiplier - 1f) * s;
        else
            TastyMultiplier *= s;
        if (Genome.IsExpressed(TraitType.Bitter))
            BitterMultiplier *= s;
        if (Genome.IsExpressed(TraitType.Poisonous))
            PoisonDamagePerSec *= d;
    }

    /// <summary>
    /// Combined eat-attractiveness score used by Grazer when choosing a plant.
    /// Higher = more likely to be chosen.
    /// </summary>
    public float EatAttractiveness => TastyMultiplier * BitterMultiplier;
}