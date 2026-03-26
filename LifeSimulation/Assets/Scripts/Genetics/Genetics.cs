using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Genetics
{
    const float MutationChance = 0.01f;

    public static List<Gene> Breed(Creature parentA, Creature parentB)
    {
        List<Gene> childGenes = new List<Gene>();

        foreach (var geneA in parentA.Genes)
        {
            var geneB = parentB.Genes.FirstOrDefault
            (g => g.TraitType == geneA.TraitType);

            if (geneB == null) continue;

            bool alleleA = geneA.GetRandomAllele();
            bool alleleB = geneB.GetRandomAllele();

            if (Random.value < MutationChance)
                alleleA = !alleleA;

            if (Random.value < MutationChance)
                alleleB = !alleleB;

            childGenes.Add(new Gene(geneA.TraitType, alleleA, alleleB));
        }

        return childGenes;
    }
}