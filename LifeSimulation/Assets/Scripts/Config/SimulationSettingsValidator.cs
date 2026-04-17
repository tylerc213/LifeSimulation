// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Settings validation
// Requirement:	Configuration
// Author:		Benjamin Jones
// Date:		04/14/2026
// Version:		0.0.0
//
// Description:
//    Normalizes and bounds settings after parse or edit so the sim never runs on
//    broken numbers or outdated JSON shapes; everything downstream sees a safe snapshot.
// -----------------------------------------------------------------------------

using UnityEngine;

/// <summary> Validates and clamps <see cref="SimulationSettings"/> in place. </summary>
public static class SimulationSettingsValidator
{
    public const int SupportedSchemaVersion = 1;

    public static bool TryValidate(SimulationSettings s, out string error)
    {
        error = null;
        if (s == null)
        {
            error = "Settings object is null.";
            return false;
        }

        if (s.schemaVersion < 1 || s.schemaVersion > SupportedSchemaVersion)
        {
            error = "Unsupported schemaVersion: " + s.schemaVersion;
            return false;
        }

        ClampInPlace(s);
        return true;
    }

    static void MigrateTerrainV1(TerrainSettingsData t)
    {
        if (t.rockSpawnRate <= 0f && t.obstacleSpawnChance > 0f)
            t.rockSpawnRate = Mathf.Clamp(t.obstacleSpawnChance, 0f, 0.03f);

        if (t.waterSpawnRate <= 0f && t.waterThreshold > 0f)
            t.waterSpawnRate = Mathf.Clamp01(Mathf.InverseLerp(0.9f, 0.5f, t.waterThreshold));

        if (t.rockSpawnRate < 0f)
            t.rockSpawnRate = 0.01f;
        if (t.waterSpawnRate <= 0f)
            t.waterSpawnRate = 0.5f;
    }

    static void ClampInPlace(SimulationSettings s)
    {
        if (s.game != null)
            s.game.simulationSpeed = Mathf.Clamp(s.game.simulationSpeed, 0f, 10f);

        if (s.terrain != null)
        {
            MigrateTerrainV1(s.terrain);
            s.terrain.rockSpawnRate = Mathf.Clamp(s.terrain.rockSpawnRate, 0f, 0.03f);
            s.terrain.waterSpawnRate = Mathf.Clamp01(s.terrain.waterSpawnRate);
            s.terrain.obstacleMinCluster = Mathf.Max(1, s.terrain.obstacleMinCluster);
            s.terrain.obstacleMaxCluster = Mathf.Max(s.terrain.obstacleMinCluster, s.terrain.obstacleMaxCluster);
            s.terrain.mapWidth = Mathf.Clamp(s.terrain.mapWidth, 20, 500);
            s.terrain.mapHeight = Mathf.Clamp(s.terrain.mapHeight, 20, 500);
            s.terrain.perlinScale = Mathf.Clamp(s.terrain.perlinScale, 0.02f, 0.3f);
        }

        ClampPopulationAndReplenish(s);
        ClampExpression(s);
    }

    static void ClampPopulationAndReplenish(SimulationSettings s)
    {
        if (s.plant != null)
        {
            s.plant.startingPopulation = Mathf.Clamp(s.plant.startingPopulation, 0, 2000);
            s.plant.maxPopulation = Mathf.Clamp(s.plant.maxPopulation, 1, 5000);
            s.plant.replenishIntervalSeconds = Mathf.Clamp(s.plant.replenishIntervalSeconds, 0.25f, 300f);
            s.plant.replenishAmount = Mathf.Clamp(s.plant.replenishAmount, 0, 500);
        }

        if (s.grazer != null)
        {
            s.grazer.startingPopulation = Mathf.Clamp(s.grazer.startingPopulation, 0, 2000);
            s.grazer.maxPopulation = Mathf.Clamp(s.grazer.maxPopulation, 1, 3000);
            s.grazer.replenishIntervalSeconds = Mathf.Clamp(s.grazer.replenishIntervalSeconds, 0.25f, 300f);
            s.grazer.replenishAmount = Mathf.Clamp(s.grazer.replenishAmount, 0, 200);
        }

        if (s.predator != null)
        {
            s.predator.startingPopulation = Mathf.Clamp(s.predator.startingPopulation, 0, 1000);
            s.predator.maxPopulation = Mathf.Clamp(s.predator.maxPopulation, 1, 2000);
            s.predator.replenishIntervalSeconds = Mathf.Clamp(s.predator.replenishIntervalSeconds, 0.25f, 300f);
            s.predator.replenishAmount = Mathf.Clamp(s.predator.replenishAmount, 0, 200);
            if (!s.predator.replenishEnabled && s.predator.replenishAmount >= 50)
                s.predator.replenishEnabled = true;
        }
    }

    static void ClampExpression(SimulationSettings s)
    {
        if (s.plant?.expression != null)
        {
            s.plant.expression.primaryStats = Mathf.Clamp(s.plant.expression.primaryStats, 0f, 3f);
            s.plant.expression.secondaryTraits = Mathf.Clamp(s.plant.expression.secondaryTraits, 0f, 3f);
            s.plant.expression.defenseTraits = Mathf.Clamp(s.plant.expression.defenseTraits, 0f, 3f);
        }

        if (s.grazer?.expression != null)
        {
            s.grazer.expression.statTraits = Mathf.Clamp(s.grazer.expression.statTraits, 0f, 3f);
            s.grazer.expression.rareTraits = Mathf.Clamp(s.grazer.expression.rareTraits, 0f, 3f);
            s.grazer.expression.packTraits = Mathf.Clamp(s.grazer.expression.packTraits, 0f, 3f);
        }

        if (s.predator?.expression != null)
        {
            s.predator.expression.statTraits = Mathf.Clamp(s.predator.expression.statTraits, 0f, 3f);
            s.predator.expression.rareTraits = Mathf.Clamp(s.predator.expression.rareTraits, 0f, 3f);
            s.predator.expression.apexTraits = Mathf.Clamp(s.predator.expression.apexTraits, 0f, 3f);
        }
    }
}
