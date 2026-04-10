using UnityEngine;

public class HerdLeaderTrait : TraitBehavior
{
    [Tooltip("Multiplier for leader speed and pack influence.")]
    public float SpeedMultiplier = 2f;
    public float HealthMultiplier = 1.5f;

    protected override void OnTraitApplied()
    {
        // Boost speed immediately
        creature.Speed *= SpeedMultiplier;

        // Optionally boost max health
        creature.MaxHealth *= HealthMultiplier;
        creature.CurrentHealth = creature.MaxHealth;
    }
}
