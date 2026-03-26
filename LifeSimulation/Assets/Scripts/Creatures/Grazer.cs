using UnityEngine;

public class Grazer : Creature
{
    protected override void Start()
    {
        Species = SpeciesType.Grazer;

        MaxHealth = 100f;
        Damage = 25f;
        Speed = 1f;

        base.Start();
    }

    public void EatPlant(Plant plant)
    {
        CurrentEnergy += plant.EnergyGiven;

        PoisonousTrait poison = plant.GetComponent<PoisonousTrait>();

        if (poison != null)
            poison.PoisonEater(this);
    }
}