using UnityEngine;

public class CamouflageTrait : TraitBehavior
{
    protected override void OnTraitApplied()
    {
        VisionSystem vision = creature.GetComponent<VisionSystem>();
        if (vision != null)
        {
            vision.visionRange *= 0.7f;
        }
    }
}
