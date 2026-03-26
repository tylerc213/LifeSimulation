using UnityEngine;

using System;

public static class TraitFactory
{
    public static Type GetTraitScript(TraitType type)
    {
        switch (type)
        {
            case TraitType.Nimble: return typeof(NimbleTrait);
            case TraitType.Strong: return typeof(StrongTrait);
            case TraitType.ThickSkinned: return typeof(ThickSkinnedTrait);
            case TraitType.Camouflage: return typeof(CamouflageTrait);
            case TraitType.Spiky: return typeof(SpikyTrait);
            case TraitType.NightVision: return typeof(NightVisionTrait);
            case TraitType.HerdMentality: return typeof(HerdMentalityTrait);
            case TraitType.HerdLeader: return typeof(HerdLeaderTrait);
            case TraitType.Venomous: return typeof(VenomousTrait);
            case TraitType.Ambusher: return typeof(AmbusherTrait);
            case TraitType.HerdHunter: return typeof(HerdHunterTrait);
            case TraitType.ApexPredator: return typeof(ApexPredatorTrait);
            case TraitType.Tasty: return typeof(TastyTrait);
            case TraitType.Bitter: return typeof(BitterTrait);
            case TraitType.Poisonous: return typeof(PoisonousTrait);
            case TraitType.Resilient: return typeof(ResilientTrait);
        }

        return null;
    }
}