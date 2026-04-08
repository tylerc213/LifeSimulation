using System.Collections.Generic;
using UnityEngine;

public class Creature : MonoBehaviour
{
    [Header("Base Stats")]
    public float MaxHealth = 100f;
    public float CurrentHealth;

    // NEW: Movement speed
    public float Speed = 1f;

    [Header("Traits & Genetics")]
    public Genome Genome;
    public List<TraitBehavior> ActiveTraits = new List<TraitBehavior>();

    protected virtual void Awake()
    {
        CurrentHealth = MaxHealth;
    }

    protected virtual void Start()
    {
        if (Genome != null)
            ApplyGenome(Genome);
    }

    public void ApplyGenome(Genome genome)
    {
        ActiveTraits.Clear();
        foreach (var gene in genome.genes)
        {
            var traitType = TraitFactory.GetTrait(gene.traitType);
            if (traitType != null)
            {
                TraitBehavior trait = gameObject.AddComponent(traitType) as TraitBehavior;
                trait.Initialize(this);
                ActiveTraits.Add(trait);
            }
        }
    }

    public void TakeDamage(float amount)
    {
        CurrentHealth -= amount;
        if (CurrentHealth <= 0f)
            Die();
    }

    protected virtual void Die()
    {
        Destroy(gameObject);
    }
}