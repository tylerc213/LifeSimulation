using UnityEngine;

public class ApexPredatorTrait : TraitBehavior
{
    protected override void OnTraitApplied()
    {
        creature.Speed *= 1.3f;
        creature.MaxHealth *= 1.5f;

        Predator p = creature as Predator;
        if (p != null)
            p.AttackDamage *= 1.5f;
    }
}
