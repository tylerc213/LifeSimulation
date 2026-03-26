using UnityEngine;

public class Plant : Creature
{
    public float EnergyGiven = 25;

    protected override void Start()
    {
        Species = SpeciesType.Plant;

        MaxHealth = 20;

        base.Start();
    }
}