using System.Diagnostics;
using UnityEngine;

public class LeafLargeTrait : TraitBehavior
{
    protected override void OnTraitApplied()
    {
        if (creature is Plant plant)
        {
            plant.EnergyGiven *= 1.5f;   // Large leaves give more energy
            plant.SunlightNeeded *= 1.5f; // Large leaves require more sunlight
        }
        else
        {
            UnityEngine.Debug.LogWarning("LeafLargeTrait applied to non-Plant!");
        }
    }
}
