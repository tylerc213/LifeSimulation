using UnityEngine;

public class HerdMentalityTrait : TraitBehavior
{
    [Tooltip("Multiplier for how strongly this creature sticks to the pack.")]
    public float CohesionMultiplier = 2f;

    protected override void OnTraitApplied()
    {
        // Nothing needed immediately; effect applied in Pack system
    }

    // Optional helper for Pack system
    public float GetCohesionMultiplier()
    {
        return CohesionMultiplier;
    }
}