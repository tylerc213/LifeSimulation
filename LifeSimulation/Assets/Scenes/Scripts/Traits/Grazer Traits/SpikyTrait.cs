using UnityEngine;

public class SpikyTrait : TraitBehavior
{
    protected override void OnTraitApplied() { }

    public void ReflectDamage(Creature attacker, float damage)
    {
        attacker.TakeDamage(damage * 0.3f);
    }
}
