// -----------------------------------------------------------------------------
// Genetics.cs
// Core data structures for the trait/gene system.
// No MonoBehaviour — pure data, used by Genome.cs and all entity scripts.
// -----------------------------------------------------------------------------
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Every distinct heritable trait in the simulation.
/// </summary>
public enum TraitType
{
    // ── Plant ──────────────────────────────────────────────────────────────
    LeafSize,       // small / medium / large  (encoded as 0/1/2 via allele value)
    Tasty,          // dominant
    Bitter,         // recessive
    Poisonous,      // recessive
    Resilient,      // dominant

    // ── Grazer ─────────────────────────────────────────────────────────────
    GrazerNimble,
    GrazerStrong,
    GrazerThickSkinned,
    Camouflage,     // recessive
    Spiky,          // recessive
    HerdMentality,  // recessive – requires 2+ grazers in pack
    HerdLeader,     // recessive – one per pack

    // ── Predator ───────────────────────────────────────────────────────────
    PredatorNimble,
    PredatorStrong,
    PredatorThickSkinned,
    Venomous,       // recessive
    Ambusher,       // recessive
    HerdHunter,     // recessive – requires 2+ predators in pack
    ApexPredator,   // recessive – one per simulation
}

/// <summary>
/// A single gene: two alleles (true = trait allele, false = wild-type allele)
/// and whether the trait is dominant or recessive.
/// Trait is expressed when:
///   Dominant  → at least one allele is true  (Aa or AA)
///   Recessive → both alleles are true         (aa only)
/// </summary>
[System.Serializable]
public class Gene
{
    public TraitType Trait;
    public bool AlleleA;
    public bool AlleleB;
    public bool IsDominant;

    public Gene(TraitType trait, bool alleleA, bool alleleB, bool isDominant)
    {
        Trait = trait;
        AlleleA = alleleA;
        AlleleB = alleleB;
        IsDominant = isDominant;
    }

    /// <summary>Returns true if this gene is expressed (phenotype active).</summary>
    public bool IsExpressed =>
        IsDominant ? (AlleleA || AlleleB)   // dominant: Aa or AA
                   : (AlleleA && AlleleB);  // recessive: aa only

    /// <summary>
    /// Mendelian cross: takes one allele from this gene and one from the other parent.
    /// </summary>
    public Gene InheritWith(Gene otherParent)
    {
        bool childA = UnityEngine.Random.value < 0.5f ? AlleleA : AlleleB;
        bool childB = UnityEngine.Random.value < 0.5f ? otherParent.AlleleA : otherParent.AlleleB;
        return new Gene(Trait, childA, childB, IsDominant);
    }

    /// <summary>Creates a gene with randomised alleles.</summary>
    public static Gene Random(TraitType trait, bool isDominant, float alleleFrequency = 0.5f)
    {
        return new Gene(
            trait,
            UnityEngine.Random.value < alleleFrequency,
            UnityEngine.Random.value < alleleFrequency,
            isDominant);
    }
}

/// <summary>
/// A full set of genes for one entity.
/// Build via the static factory methods, then attach to an entity component.
/// </summary>
[System.Serializable]
public class Genome
{
    public List<Gene> Genes = new List<Gene>();

    // Allele frequency: probability that any given allele is the trait allele.
    // Dominants start high (likely expressed), recessives start low (rarely expressed).
    private const float DomFreq = 0.6f;
    private const float RecFreq = 0.25f;

    // ── Factory methods ────────────────────────────────────────────────────

    public static Genome RandomPlant()
    {
        var g = new Genome();
        g.Genes.Add(Gene.Random(TraitType.LeafSize, true, DomFreq));
        g.Genes.Add(Gene.Random(TraitType.Tasty, true, DomFreq));
        g.Genes.Add(Gene.Random(TraitType.Bitter, false, RecFreq));
        g.Genes.Add(Gene.Random(TraitType.Poisonous, false, RecFreq));
        g.Genes.Add(Gene.Random(TraitType.Resilient, true, DomFreq));
        return g;
    }

    public static Genome RandomGrazer()
    {
        var g = new Genome();
        g.Genes.Add(Gene.Random(TraitType.GrazerNimble, true, DomFreq));
        g.Genes.Add(Gene.Random(TraitType.GrazerStrong, true, DomFreq));
        g.Genes.Add(Gene.Random(TraitType.GrazerThickSkinned, true, DomFreq));
        g.Genes.Add(Gene.Random(TraitType.Camouflage, false, RecFreq));
        g.Genes.Add(Gene.Random(TraitType.Spiky, false, RecFreq));
        g.Genes.Add(Gene.Random(TraitType.HerdMentality, false, RecFreq));
        g.Genes.Add(Gene.Random(TraitType.HerdLeader, false, RecFreq * 0.3f));
        return g;
    }

    public static Genome RandomPredator()
    {
        var g = new Genome();
        g.Genes.Add(Gene.Random(TraitType.PredatorNimble, true, DomFreq));
        g.Genes.Add(Gene.Random(TraitType.PredatorStrong, true, DomFreq));
        g.Genes.Add(Gene.Random(TraitType.PredatorThickSkinned, true, DomFreq));
        g.Genes.Add(Gene.Random(TraitType.Venomous, false, RecFreq));
        g.Genes.Add(Gene.Random(TraitType.Ambusher, false, RecFreq));
        g.Genes.Add(Gene.Random(TraitType.HerdHunter, false, RecFreq));
        g.Genes.Add(Gene.Random(TraitType.ApexPredator, false, RecFreq * 0.2f));
        return g;
    }

    /// <summary>Mendelian inheritance from two parent genomes.</summary>
    public static Genome Inherit(Genome parentA, Genome parentB)
    {
        var child = new Genome();
        for (int i = 0; i < parentA.Genes.Count; i++)
            child.Genes.Add(parentA.Genes[i].InheritWith(parentB.Genes[i]));
        return child;
    }

    // ── Lookup helpers ─────────────────────────────────────────────────────

    public Gene Get(TraitType trait)
    {
        foreach (var g in Genes)
            if (g.Trait == trait) return g;
        return null;
    }

    public bool IsExpressed(TraitType trait)
    {
        var g = Get(trait);
        return g != null && g.IsExpressed;
    }
}