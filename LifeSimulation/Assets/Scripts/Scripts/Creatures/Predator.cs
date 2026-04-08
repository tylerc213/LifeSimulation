using UnityEngine;

public class Predator : Creature
{
    public float AttackDamage = 50f;

    protected override void Start()
    {
        base.Start();
    }

    public void Attack(Creature target)
    {
        if (target == null) return;

        target.TakeDamage(AttackDamage);

        // Handle Spiky Trait of target
        foreach (var trait in target.ActiveTraits)
        {
            if (trait is SpikyTrait)
                TakeDamage(AttackDamage * 0.5f);
        }
    }
}