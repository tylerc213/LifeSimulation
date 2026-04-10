using System.Collections;
using UnityEngine;

public class VenomousTrait : TraitBehavior
{
    protected override void OnTraitApplied() { }

    public void ApplyPoison(Creature target)
    {
        creature.StartCoroutine(Poison(target));
    }

    IEnumerator Poison(Creature target)
    {
        for (int i = 0; i < 5; i++)
        {
            target.TakeDamage(2f);
            yield return new WaitForSeconds(1f);
        }
    }
}
