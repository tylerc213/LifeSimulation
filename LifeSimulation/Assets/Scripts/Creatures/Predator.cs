using UnityEngine;

public class Predator : Creature
{
    protected override void Start()
    {
        Species = SpeciesType.Predator;

        MaxHealth = 75f;
        Damage = 50f;
        Speed = 1.2f;

        base.Start();
    }

    public void Attack(Creature target)
    {
        float dmg = Damage;

        AmbusherTrait ambusher = GetComponent<AmbusherTrait>();

        if (ambusher != null)
            dmg = ambusher.ModifyDamage(dmg, false);

        target.TakeDamage(dmg);

        VenomousTrait venom = GetComponent<VenomousTrait>();

        if (venom != null)
            venom.ApplyVenom(target);
    }
}