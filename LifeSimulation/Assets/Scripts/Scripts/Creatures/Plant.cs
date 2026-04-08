using UnityEngine;

public class Plant : Creature
{
    public float EnergyGiven = 25f;
    public float SunlightNeeded = 10f;

    public enum LeafSize { Small, Medium, Large }
    public LeafSize Size = LeafSize.Small;

    protected override void Start()
    {
        base.Start();
    }

    protected override void Die()
    {
        base.Die(); // calls Destroy(gameObject)
    }

    // Public wrapper to allow grazers to kill the plant
    public void Kill()
    {
        Die();
    }
}