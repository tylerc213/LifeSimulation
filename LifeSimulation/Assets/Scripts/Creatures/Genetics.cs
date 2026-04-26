﻿// -----------------------------------------------------------------------------
// Project:     EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:        Lifeforms
// Requirement: Lifeform Simulation
// Author:      Luke Kivett
// Date:        4/6/2026
// Version:     0.0.0
//
// Description:
//    Core data structures for the heritable trait system. Defines TraitType,
//    Gene (allele pair + dominance), and Genome (full trait collection with
//    Mendelian inheritance and random initialisation factory methods).
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
    NightVision,    // recessive

    // ── Additional trait for predators and grazers ───────────────────────────────────────────────────────────
    Reptile         // dominant
}

/// <summary>A single heritable gene consisting of two alleles and a dominance flag.</summary>
/// <remarks>
/// Dominant genes express when at least one allele is true (Aa or AA).
/// Recessive genes express only when both alleles are true (aa).
/// </remarks>
[System.Serializable]
public class Gene
{
    public TraitType Trait;
    public bool AlleleA;
    public bool AlleleB;
    public bool IsDominant;

    /// <summary>Constructs a gene with explicit allele values.</summary>
    /// <param name="trait">Trait this gene controls.</param>
    /// <param name="alleleA">First allele value.</param>
    /// <param name="alleleB">Second allele value.</param>
    /// <param name="isDominant">Whether the trait is dominant or recessive.</param>
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

    /// <summary>Produces an offspring gene via Mendelian inheritance from two parents.</summary>
    /// <param name="otherParent">The other parent's copy of this gene.</param>
    /// <returns>New gene with one allele from each parent.</returns>
    public Gene InheritWith(Gene otherParent)
    {
        bool childA = UnityEngine.Random.value < 0.5f ? AlleleA : AlleleB;
        bool childB = UnityEngine.Random.value < 0.5f ? otherParent.AlleleA : otherParent.AlleleB;
        return new Gene(Trait, childA, childB, IsDominant);
    }

    /// <summary>Creates a gene with randomly assigned alleles at a given frequency.</summary>
    /// <param name="trait">Trait to assign.</param>
    /// <param name="isDominant">Whether the trait is dominant.</param>
    /// <param name="alleleFrequency">Probability each allele is the trait allele.</param>
    /// <returns>New gene with randomised alleles.</returns>
    public static Gene Random(TraitType trait, bool isDominant, float alleleFrequency = 0.5f)
    {
        return new Gene(
            trait,
            UnityEngine.Random.value < alleleFrequency,
            UnityEngine.Random.value < alleleFrequency,
            isDominant);
    }
}

/// <summary>A complete set of genes for one entity with factory and inheritance methods.</summary>
[System.Serializable]
public class Genome
{
    public List<Gene> Genes = new List<Gene>();

    // Allele frequency: probability that any given allele is the trait allele.
    // Dominants start high (likely expressed), recessives start low (rarely expressed).
    private const float DomFreq = 0.6f;
    private const float RecFreq = 0.25f;

    /// <summary>Creates a randomised plant genome.</summary>
    /// <returns>New genome with all plant trait genes.</returns>
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

    /// <summary>Creates a randomised grazer genome.</summary>
    /// <returns>New genome with all grazer trait genes.</returns>
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
        g.Genes.Add(Gene.Random(TraitType.Reptile, true, DomFreq));
        return g;
    }

    /// <summary>Creates a randomised predator genome.</summary>
    /// <returns>New genome with all predator trait genes.</returns>
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
        g.Genes.Add(Gene.Random(TraitType.NightVision, false, RecFreq));
        g.Genes.Add(Gene.Random(TraitType.Reptile, true, DomFreq));
        return g;
    }

    /// <summary>Produces an offspring genome via Mendelian inheritance from two parents.</summary>
    /// <param name="parentA">First parent genome.</param>
    /// <param name="parentB">Second parent genome.</param>
    /// <returns>New genome with one allele per gene from each parent.</returns>
    public static Genome Inherit(Genome parentA, Genome parentB)
    {
        var child = new Genome();
        for (int i = 0; i < parentA.Genes.Count; i++)
            child.Genes.Add(parentA.Genes[i].InheritWith(parentB.Genes[i]));
        return child;
    }

    /// <summary>Retrieves a gene by trait type.</summary>
    /// <param name="trait">Trait to look up.</param>
    /// <returns>Matching gene, or null if not found.</returns>
    public Gene Get(TraitType trait)
    {
        foreach (var g in Genes)
            if (g.Trait == trait) return g;
        return null;
    }

    /// <summary>Returns true if the specified trait is phenotypically expressed.</summary>
    /// <param name="trait">Trait to evaluate.</param>
    /// <returns>True if the gene exists and its expression condition is met.</returns>
    public bool IsExpressed(TraitType trait)
    {
        var g = Get(trait);
        return g != null && g.IsExpressed;
    }
}
