using UnityEngine;

public class HerdHunterTrait : TraitBehavior
{
    [Tooltip("Requires at least 2 predators in the pack to coordinate attacks.")]
    public int RequiredPackSize = 2;

    protected override void OnTraitApplied()
    {
        // No immediate stat change; pack system checks this trait
    }

    // Helper for Pack system
    public bool CanCoordinateAttack(Pack pack)
    {
        return pack.Members.Count >= RequiredPackSize;
    }
}