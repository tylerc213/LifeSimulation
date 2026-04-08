using UnityEngine;

public class StrongTrait : TraitBehavior
{
    protected override void OnTraitApplied()
    {
        Predator p = creature as Predator;
        if (p != null)
        {
            p.AttackDamage *= 1.5f;
        }
    }
}
