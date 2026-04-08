using UnityEngine;

public abstract class TraitBehavior : MonoBehaviour
{
    protected Creature creature;

    public void Initialize(Creature c)
    {
        creature = c;
        OnTraitApplied();
    }

    protected abstract void OnTraitApplied();
}