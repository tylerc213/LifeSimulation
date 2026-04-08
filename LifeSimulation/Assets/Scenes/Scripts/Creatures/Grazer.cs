using UnityEngine;

public class Grazer : Creature
{
    public float Energy = 100f;
    public float MaxEnergy = 400f;

    protected override void Start()
    {
        base.Start();
    }

    public void EatPlant(Plant plant)
    {
        if (plant == null) return;

        Energy += plant.EnergyGiven;
        if (Energy > MaxEnergy) Energy = MaxEnergy;

        // Kill the plant
        plant.Kill();
    }
}