using UnityEngine;
using System;

public static class TraitFactory
{
    public static Type GetTrait(TraitType type)
    {
        switch (type)
        {
            // Grazers
            case TraitType.Nimble: return typeof(NimbleTrait);
            case TraitType.ThickSkinned: return typeof(ThickSkinnedTrait);
            case TraitType.Spiky: return typeof(SpikyTrait);
            case TraitType.Camouflage: return typeof(CamouflageTrait);
            case TraitType.HerdMentality: return typeof(HerdMentalityTrait);
            case TraitType.HerdLeader: return typeof(HerdLeaderTrait);

            // Predators
            case TraitType.Strong: return typeof(StrongTrait);
            case TraitType.Venomous: return typeof(VenomousTrait);
            case TraitType.HerdHunter: return typeof(HerdHunterTrait);
            case TraitType.Ambusher: return typeof(AmbusherTrait);
            case TraitType.ApexPredator: return typeof(ApexPredatorTrait);

            // Plants
            case TraitType.LeafSmall: return typeof(LeafSmallTrait);
            case TraitType.LeafMedium: return typeof(LeafMediumTrait);
            case TraitType.LeafLarge: return typeof(LeafLargeTrait);
        }

        return null;
    }
}