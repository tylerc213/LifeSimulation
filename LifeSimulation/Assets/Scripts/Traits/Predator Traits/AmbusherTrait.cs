using UnityEngine;

public class AmbusherTrait : TraitBehavior
{
    protected override void OnTraitApplied() { }

    public float GetAmbushBonus()
    {
        return 2.0f; // double damage on first hit
    }
}
