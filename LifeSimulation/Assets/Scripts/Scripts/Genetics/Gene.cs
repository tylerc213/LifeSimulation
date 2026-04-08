using System.Collections.Generic;

[System.Serializable]
public class Gene
{
    public TraitType traitType;
    public bool isDominant;

    public Gene(TraitType type, bool dominant)
    {
        traitType = type;
        isDominant = dominant;
    }
}