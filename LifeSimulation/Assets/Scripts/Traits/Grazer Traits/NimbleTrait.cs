using UnityEngine;

public class NimbleTrait : TraitBehavior
{
    protected override void OnTraitApplied()
    {
        creature.Speed *= 1.5f;
    }
}
