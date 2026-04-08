using System.Collections.Generic;

[System.Serializable]
public class Genome
{
    public List<Gene> genes = new List<Gene>();
    public int maxGenes = 4;

    public Genome() { }
    public Genome(List<Gene> initialGenes) { genes = new List<Gene>(initialGenes); }

    public void AddGene(Gene gene)
    {
        if (genes.Count < maxGenes && !ContainsTrait(gene.traitType))
            genes.Add(gene);
    }

    public bool ContainsTrait(TraitType type)
    {
        foreach (var g in genes)
            if (g.traitType == type) return true;
        return false;
    }
}