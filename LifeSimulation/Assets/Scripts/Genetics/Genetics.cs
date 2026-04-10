using System.Collections.Generic;
using UnityEngine;

public static class Genetics
{
    public static Genome Breed(Genome parentA, Genome parentB)
    {
        List<Gene> offspringGenes = new List<Gene>();
        List<TraitType> uniqueTraits = new List<TraitType>();

        foreach (var g in parentA.genes) if (!uniqueTraits.Contains(g.traitType)) uniqueTraits.Add(g.traitType);
        foreach (var g in parentB.genes) if (!uniqueTraits.Contains(g.traitType)) uniqueTraits.Add(g.traitType);

        foreach (var trait in uniqueTraits)
        {
            int count = 0;
            if (parentA.ContainsTrait(trait)) count++;
            if (parentB.ContainsTrait(trait)) count++;

            bool isDominant = false;
            foreach (var g in parentA.genes) if (g.traitType == trait) isDominant = g.isDominant;
            foreach (var g in parentB.genes) if (g.traitType == trait) isDominant |= g.isDominant;

            float probability = (count / (float)uniqueTraits.Count) * (isDominant ? 1f : 0.5f);
            if (UnityEngine.Random.value <= probability)
                offspringGenes.Add(new Gene(trait, isDominant));
        }

        if (offspringGenes.Count < 4 && UnityEngine.Random.value <= 0.25f)
        {
            Gene newTrait = GenerateRandomGene(offspringGenes);
            if (newTrait != null) offspringGenes.Add(newTrait);
        }

        return new Genome(offspringGenes);
    }

    private static Gene GenerateRandomGene(List<Gene> existing)
    {
        TraitType[] allTraits = (TraitType[])System.Enum.GetValues(typeof(TraitType));
        List<TraitType> possible = new List<TraitType>();
        foreach (var t in allTraits) if (!existing.Exists(g => g.traitType == t)) possible.Add(t);
        if (possible.Count == 0) return null;

        TraitType chosen = possible[UnityEngine.Random.Range(0, possible.Count)];
        bool isDominant = UnityEngine.Random.value <= 0.75f;
        return new Gene(chosen, isDominant);
    }
}