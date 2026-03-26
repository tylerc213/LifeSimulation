using UnityEngine;

using System.Collections.Generic;

public static class TraitValidator
{
    public static void Validate(List<TraitType> traits)
    {
        if (traits.Contains(TraitType.Camouflage)
        && traits.Contains(TraitType.Spiky))
            traits.Remove(TraitType.Spiky);

        if (traits.Contains(TraitType.Tasty)
        && traits.Contains(TraitType.Bitter))
            traits.Remove(TraitType.Bitter);

        if (traits.Contains(TraitType.ApexPredator)
        && traits.Contains(TraitType.HerdHunter))
            traits.Remove(TraitType.HerdHunter);

        if (traits.Contains(TraitType.HerdLeader)
        && traits.Contains(TraitType.HerdMentality))
            traits.Remove(TraitType.HerdMentality);
    }
}