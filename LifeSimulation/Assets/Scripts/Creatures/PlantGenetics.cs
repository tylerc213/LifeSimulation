// -----------------------------------------------------------------------------
// Project:     EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:        Plant Subtypes
// Requirement: Lifeform Simulation
// Author:      Luke Kivett
// Date:        04/06/2026
// Version:     0.0.1
//
// Description:
//    Reads a plant Genome and applies trait effects to nutrition, color,
//    and eat-attractiveness. Attach alongside the Plant script.
//    Scale is now controlled entirely by Plant.Grow() using GetLeafSizeScaleMultiplier()
//    so that plants grow to their genetically correct size over time.
// -----------------------------------------------------------------------------
using UnityEngine;

/// <summary>Applies genetic trait effects to a Plant at spawn time.</summary>
/// <remarks>
/// Call Init with a genome immediately after instantiation to override the
/// random genome assigned in Awake. EcosystemManager handles this for offspring.
/// Scale is intentionally not set here — Plant.Grow() reads the LeafSize gene
/// directly and lerps toward the correct max scale each frame.
/// </remarks>
[RequireComponent(typeof(Plant))]
public class PlantGenetics : MonoBehaviour
{
    /// <summary>The genome currently applied to this plant.</summary>
    public Genome Genome { get; private set; }

    /// <summary>Multiplier applied to base nutrition; driven by leaf size.</summary>
    public float NutritionMultiplier { get; private set; } = 1f;

    /// <summary>True when the Poisonous gene is expressed.</summary>
    public bool IsPoisonous { get; private set; } = false;

    /// <summary>Damage per second dealt to a grazer that eats this plant.</summary>
    public float PoisonDamagePerSec { get; private set; } = 3f;

    /// <summary>Multiplier increasing grazer willingness to eat; driven by Tasty gene.</summary>
    public float TastyMultiplier { get; private set; } = 1f;

    /// <summary>Multiplier decreasing grazer willingness to eat; driven by Bitter/Poisonous genes.</summary>
    public float BitterMultiplier { get; private set; } = 1f;

    /// <summary>True when the Resilient gene is expressed.</summary>
    public bool IsResilient { get; private set; } = false;

    private Plant _plant;
    private SpriteRenderer _sr;

    /// <summary>Assigns a random genome if none has been provided via Init.</summary>
    private void Awake()
    {
        _plant = GetComponent<Plant>();
        _sr = GetComponent<SpriteRenderer>();

        if (Genome == null)
            Init(Genome.RandomPlant());
    }

    /// <summary>Applies a specific genome to this plant, overriding any previous assignment.</summary>
    /// <param name="genome">Genome to apply.</param>
    public void Init(Genome genome)
    {
        Genome = genome;
        ApplyTraits();
        // Notify Plant to recalculate its effective max scale with the new genome
        GetComponent<Plant>()?.RecalculateMaxScale();
    }

    /// <summary>Reads each gene and applies its effect to stats and color.</summary>
    /// <remarks>Scale is NOT set here — Plant.Grow() controls scale via GetLeafSizeScaleMultiplier().</remarks>
    private void ApplyTraits()
    {
        float exprPrimary = ExpressionStrengthRuntime.NormalizedStrength(ExpressionStrengthRuntime.PlantPrimary);
        float exprSecondary = ExpressionStrengthRuntime.NormalizedStrength(ExpressionStrengthRuntime.PlantSecondary);
        float exprDefense = ExpressionStrengthRuntime.NormalizedStrength(ExpressionStrengthRuntime.PlantDefense);

        // Leaf size: nutrition multiplier and color only — scale is handled by Plant.Grow()
        Gene leafGene = Genome.Get(TraitType.LeafSize);
        if (leafGene != null)
        {
            int leafLevel = (leafGene.AlleleA ? 1 : 0) + (leafGene.AlleleB ? 1 : 0);
            float baseNut = 0.7f + leafLevel * 0.3f;
            // Scale the nutrition effect by expression strength
            NutritionMultiplier = 1f + (baseNut - 1f) * exprPrimary;

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

        // Tasty raises attractiveness so grazers prefer this plant
        if (Genome.IsExpressed(TraitType.Tasty) && exprSecondary > 0f)
            TastyMultiplier = 1f + 0.5f * exprSecondary;

        // Bitter lowers attractiveness so grazers avoid this plant
        if (Genome.IsExpressed(TraitType.Bitter) && exprSecondary > 0f)
            BitterMultiplier = Mathf.Lerp(1f, 0.4f, Mathf.Clamp01(exprSecondary));

        // Poisonous strongly deters eating and deals damage over time
        if (Genome.IsExpressed(TraitType.Poisonous) && exprDefense > 0f)
        {
            IsPoisonous = true;
            BitterMultiplier = Mathf.Lerp(1f, 0.1f, Mathf.Clamp01(exprDefense));
            if (_sr != null) _sr.color = new Color(0.5f, 0.1f, 0.5f);
        }

        // Resilient requires expression to be active
        IsResilient = Genome.IsExpressed(TraitType.Resilient) && exprDefense > 0f;
    }

    /// <summary>Combined eat-attractiveness score used by grazers when selecting a plant.</summary>
    /// <returns>Product of tasty and bitter multipliers; higher means more likely to be eaten.</returns>
    public float EatAttractiveness => TastyMultiplier * BitterMultiplier;
}
