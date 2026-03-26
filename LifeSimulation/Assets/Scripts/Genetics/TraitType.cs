using UnityEngine;

public enum SpeciesType
{
    Plant,
    Grazer,
    Predator
}

public enum TraitType
{
    // Plants
    Tasty,
    Bitter,
    Poisonous,
    Resilient,

    // Shared
    Nimble,
    Strong,
    ThickSkinned,
    NightVision,

    // Grazers
    Camouflage,
    Spiky,
    HerdMentality,
    HerdLeader,

    // Predators
    Venomous,
    Ambusher,
    HerdHunter,
    ApexPredator
}