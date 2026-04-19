// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Simulation settings model
// Requirement:	Configuration
// Author:		Benjamin Jones
// Date:		04/14/2026
// Version:		0.0.0
//
// Description:
//    The portable definition of this run's world: what JSON, the config scene,
//    and the in-sim editor all read and write so behavior stays one contract.
// -----------------------------------------------------------------------------

using System;
using UnityEngine;

/// <summary> Root document for JSON persistence and import/export. </summary>
[Serializable]
public class SimulationSettings
{
    public int schemaVersion = 1;

    public GameSettingsData game = new GameSettingsData();
    public TerrainSettingsData terrain = new TerrainSettingsData();
    public PlantSettingsBlock plant = new PlantSettingsBlock();
    public GrazerSettingsBlock grazer = new GrazerSettingsBlock();
    public PredatorSettingsBlock predator = new PredatorSettingsBlock();

    public static SimulationSettings CreateDefaults()
    {
        return new SimulationSettings();
    }
}

[Serializable]
public class GameSettingsData
{
    [Range(0f, 10f)] public float simulationSpeed = 2f;
}

[Serializable]
public class TerrainSettingsData
{
    /// <summary> Legacy v1 JSON key; migrated to <see cref="rockSpawnRate"/> in validator. </summary>
    public float obstacleSpawnChance;

    /// <summary> Legacy v1; migrated to <see cref="waterSpawnRate"/>. </summary>
    public float waterThreshold;

    /// <summary> Probability each tile starts a rock cluster attempt (kept low — large maps). </summary>
    [Range(0f, 0.03f)] public float rockSpawnRate = 0.01f;

    /// <summary> 0 = least water, 1 = most water (drives Perlin threshold). </summary>
    [Range(0f, 1f)] public float waterSpawnRate = 0.5f;

    [Min(1)] public int obstacleMinCluster = 1;
    [Min(1)] public int obstacleMaxCluster = 2;
    [Min(1)] public int mapWidth = 100;
    [Min(1)] public int mapHeight = 100;
    [Range(0.02f, 0.3f)] public float perlinScale = 0.16f;
}

[Serializable]
public class PlantExpressionSettings
{
    [Range(0f, 3f)] public float primaryStats = 1.5f;
    [Range(0f, 3f)] public float secondaryTraits = 1.5f;
    [Range(0f, 3f)] public float defenseTraits = 1.5f;
}

[Serializable]
public class GrazerExpressionSettings
{
    [Range(0f, 3f)] public float statTraits = 1.5f;
    [Range(0f, 3f)] public float rareTraits = 1.5f;
    [Range(0f, 3f)] public float packTraits = 1.5f;
}

[Serializable]
public class PredatorExpressionSettings
{
    [Range(0f, 3f)] public float statTraits = 1.5f;
    [Range(0f, 3f)] public float rareTraits = 1.5f;
    [Range(0f, 3f)] public float apexTraits = 1.5f;
}

[Serializable]
public class PlantSettingsBlock
{
    [Min(0)] public int startingPopulation = 10;
    [Min(0)] public int maxPopulation = 200;
    public bool replenishEnabled = true;
    [Min(0.1f)] public float replenishIntervalSeconds = 15f;
    [Min(0)] public int replenishAmount = 3;
    public PlantExpressionSettings expression = new PlantExpressionSettings();
}

[Serializable]
public class GrazerSettingsBlock
{
    [Min(0)] public int startingPopulation = 15;
    [Min(0)] public int maxPopulation = 150;
    public bool replenishEnabled = false;
    [Min(0.1f)] public float replenishIntervalSeconds = 2f;
    [Min(0)] public int replenishAmount = 6;
    public GrazerExpressionSettings expression = new GrazerExpressionSettings();
}

[Serializable]
public class PredatorSettingsBlock
{
    [Min(0)] public int startingPopulation = 8;
    [Min(0)] public int maxPopulation = 200;
    public bool replenishEnabled = true;
    [Min(0.1f)] public float replenishIntervalSeconds = 4f;
    [Min(0)] public int replenishAmount = 12;
    public PredatorExpressionSettings expression = new PredatorExpressionSettings();
}
