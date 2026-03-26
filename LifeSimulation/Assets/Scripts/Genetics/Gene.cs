using UnityEngine;

[System.Serializable]
public class Gene
{
    public TraitType TraitType;

    public bool AlleleA;
    public bool AlleleB;

    public Gene(TraitType type, bool a, bool b)
    {
        TraitType = type;
        AlleleA = a;
        AlleleB = b;
    }

    public bool IsExpressed()
    {
        bool dominant = TraitDatabase.IsDominant(TraitType);

        if (dominant)
            return AlleleA || AlleleB;

        return AlleleA && AlleleB;
    }

    public bool GetRandomAllele()
    {
        return Random.value < 0.5f ? AlleleA : AlleleB;
    }
}