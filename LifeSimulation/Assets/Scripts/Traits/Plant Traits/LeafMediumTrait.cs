using UnityEngine;

public class LeafMediumTrait : TraitBehavior
{
    protected override void OnTraitApplied()
    {
        Plant p = creature as Plant;
        p.EnergyGiven = 25f;
        p.SunlightNeeded = 1f;
    }
}
