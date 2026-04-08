using UnityEngine;

public class ThickSkinnedTrait : TraitBehavior
{
    protected override void OnTraitApplied()
    {
        creature.MaxHealth *= 1.5f;
        creature.CurrentHealth = creature.MaxHealth;
    }
}
