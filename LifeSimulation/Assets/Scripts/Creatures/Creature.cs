using System.Collections.Generic;
using UnityEngine;

public abstract class Creature : MonoBehaviour
{
    public SpeciesType Species;

    public float MaxHealth;
    public float CurrentHealth;

    public float Damage;
    public float Speed;

    public float BaseEnergyRequirement = 100f;
    public float CurrentEnergy;
    public float MaxEnergy = 400f;
    public float EnergyDecay = 1f;

    public List<Gene> Genes = new List<Gene>();
    public List<TraitType> ExpressedTraits = new List<TraitType>();

    protected virtual void Start()
    {
        CurrentHealth = MaxHealth;
        CurrentEnergy = BaseEnergyRequirement;

        EvaluateGenes();
        ApplyTraitEnergyCosts();
        ApplyTraitBehaviours();
    }

    protected virtual void Update()
    {
        CurrentEnergy -= EnergyDecay * Time.deltaTime;

        if (CurrentEnergy <= 0)
            Die();
    }

    void EvaluateGenes()
    {
        ExpressedTraits.Clear();

        foreach (var gene in Genes)
        {
            if (gene.IsExpressed())
                ExpressedTraits.Add(gene.TraitType);
        }

        TraitValidator.Validate(ExpressedTraits);
    }

    void ApplyTraitEnergyCosts()
    {
        foreach (var trait in ExpressedTraits)
        {
            if (TraitDatabase.IsDominant(trait))
                BaseEnergyRequirement += 25f;
            else
                BaseEnergyRequirement += 50f;
        }
    }

    void ApplyTraitBehaviours()
    {
        foreach (var trait in ExpressedTraits)
        {
            System.Type t = TraitFactory.GetTraitScript(trait);

            if (t != null)
            {
                TraitBehavior tb = gameObject.AddComponent(t) as TraitBehavior;
                tb.Initialize(this);
            }
        }
    }

    public virtual void TakeDamage(float dmg)
    {
        CurrentHealth -= dmg;

        if (CurrentHealth <= 0)
            Die();
    }

    protected virtual void Die()
    {
        Destroy(gameObject);
    }
}