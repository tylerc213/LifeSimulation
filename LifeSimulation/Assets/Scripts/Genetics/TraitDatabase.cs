using UnityEngine;

using System.Collections.Generic;

public static class TraitDatabase
{
    static Dictionary<TraitType, bool> dominantTraits =
        new Dictionary<TraitType, bool>()
    {
        {TraitType.Nimble,true},
        {TraitType.Strong,true},
        {TraitType.ThickSkinned,true},
        {TraitType.Tasty,true},
        {TraitType.Resilient,true},

        {TraitType.Bitter,false},
        {TraitType.Poisonous,false},
        {TraitType.Camouflage,false},
        {TraitType.Spiky,false},
        {TraitType.NightVision,false},
        {TraitType.HerdMentality,false},
        {TraitType.HerdLeader,false},
        {TraitType.Venomous,false},
        {TraitType.Ambusher,false},
        {TraitType.HerdHunter,false},
        {TraitType.ApexPredator,false}
    };

    public static bool IsDominant(TraitType type)
    {
        return dominantTraits[type];
    }
}