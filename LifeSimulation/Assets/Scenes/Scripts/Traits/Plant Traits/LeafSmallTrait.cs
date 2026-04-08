using System.Diagnostics;
using UnityEngine;

// Example: Small leaf plant uses less sunlight
public class LeafSmallTrait : TraitBehavior
{
    protected override void OnTraitApplied()
    {
        if (creature is Plant plant)
        {
            plant.SunlightNeeded *= 0.75f; // small leaves need less sunlight
        }
        else
        {
            UnityEngine.Debug.LogWarning("LeafSmallTrait applied to non-Plant!");
        }
    }
}