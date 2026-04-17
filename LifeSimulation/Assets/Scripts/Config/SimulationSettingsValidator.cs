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

    /// <summary> Canonical bounds shared with editor sliders and <see cref="ClampInPlace"/>. </summary>
    public static class Limits
    {
        public const float GameSimulationSpeedMin = 0f;
        public const float GameSimulationSpeedMax = 10f;

        public const float TerrainRockSpawnMin = 0f;
        public const float TerrainRockSpawnMax = 0.03f;
        public const float TerrainWaterSpawnMin = 0f;
        public const float TerrainWaterSpawnMax = 1f;
        public const float TerrainPerlinScaleMin = 0.02f;
        public const float TerrainPerlinScaleMax = 0.3f;
        public const int TerrainMapSizeMin = 20;
        public const int TerrainMapSizeMax = 500;

        public const int TerrainObstacleMinClusterMin = 1;
        public const int TerrainObstacleMinClusterMax = 16;
        public const int TerrainObstacleMaxClusterMin = 1;
        public const int TerrainObstacleMaxClusterMax = 32;

        public const float ReplenishIntervalMin = 0.25f;
        public const float ReplenishIntervalMax = 300f;

        public const int PlantStartingPopulationMin = 0;
        public const int PlantStartingPopulationMax = 100;
        public const int PlantMaxPopulationMin = 1;
        public const int PlantMaxPopulationMax = 1000;
        public const int PlantReplenishAmountMax = 100;

        public const int GrazerStartingPopulationMin = 0;
        public const int GrazerStartingPopulationMax = 100;
        public const int GrazerMaxPopulationMin = 1;
        public const int GrazerMaxPopulationMax = 2000;
        public const int GrazerReplenishAmountMax = 200;

        public const int PredatorStartingPopulationMin = 0;
        public const int PredatorStartingPopulationMax = 100;
        public const int PredatorMaxPopulationMin = 1;
        public const int PredatorMaxPopulationMax = 2000;
        public const int PredatorReplenishAmountMax = 200;

        public const float ExpressionTraitMin = 0f;
        public const float ExpressionTraitMax = 3f;

        public const int PredatorReplenishAutoEnableThreshold = 50;
    }

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
            t.rockSpawnRate = Mathf.Clamp(t.obstacleSpawnChance, Limits.TerrainRockSpawnMin, Limits.TerrainRockSpawnMax);

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
        {
            s.game.simulationSpeed = Mathf.Clamp(s.game.simulationSpeed, Limits.GameSimulationSpeedMin,
                Limits.GameSimulationSpeedMax);
        }

        if (s.terrain != null)
        {
            MigrateTerrainV1(s.terrain);
            s.terrain.rockSpawnRate = Mathf.Clamp(s.terrain.rockSpawnRate, Limits.TerrainRockSpawnMin,
                Limits.TerrainRockSpawnMax);
            s.terrain.waterSpawnRate = Mathf.Clamp(s.terrain.waterSpawnRate, Limits.TerrainWaterSpawnMin,
                Limits.TerrainWaterSpawnMax);
            s.terrain.obstacleMinCluster = Mathf.Clamp(s.terrain.obstacleMinCluster, Limits.TerrainObstacleMinClusterMin,
                Limits.TerrainObstacleMinClusterMax);
            s.terrain.obstacleMaxCluster = Mathf.Clamp(s.terrain.obstacleMaxCluster, Limits.TerrainObstacleMaxClusterMin,
                Limits.TerrainObstacleMaxClusterMax);
            s.terrain.obstacleMaxCluster = Mathf.Max(s.terrain.obstacleMinCluster, s.terrain.obstacleMaxCluster);
            s.terrain.mapWidth = Mathf.Clamp(s.terrain.mapWidth, Limits.TerrainMapSizeMin, Limits.TerrainMapSizeMax);
            s.terrain.mapHeight = Mathf.Clamp(s.terrain.mapHeight, Limits.TerrainMapSizeMin, Limits.TerrainMapSizeMax);
            s.terrain.perlinScale = Mathf.Clamp(s.terrain.perlinScale, Limits.TerrainPerlinScaleMin,
                Limits.TerrainPerlinScaleMax);
        }

        ClampPopulationAndReplenish(s);
        ClampExpression(s);
    }

    static void ClampPopulationAndReplenish(SimulationSettings s)
    {
        if (s.plant != null)
        {
            s.plant.startingPopulation = Mathf.Clamp(s.plant.startingPopulation, Limits.PlantStartingPopulationMin,
                Limits.PlantStartingPopulationMax);
            s.plant.maxPopulation = Mathf.Clamp(s.plant.maxPopulation, Limits.PlantMaxPopulationMin,
                Limits.PlantMaxPopulationMax);
            s.plant.replenishIntervalSeconds = Mathf.Clamp(s.plant.replenishIntervalSeconds, Limits.ReplenishIntervalMin,
                Limits.ReplenishIntervalMax);
            s.plant.replenishAmount = Mathf.Clamp(s.plant.replenishAmount, 0, Limits.PlantReplenishAmountMax);
        }

        if (s.grazer != null)
        {
            s.grazer.startingPopulation = Mathf.Clamp(s.grazer.startingPopulation, Limits.GrazerStartingPopulationMin,
                Limits.GrazerStartingPopulationMax);
            s.grazer.maxPopulation = Mathf.Clamp(s.grazer.maxPopulation, Limits.GrazerMaxPopulationMin,
                Limits.GrazerMaxPopulationMax);
            s.grazer.replenishIntervalSeconds = Mathf.Clamp(s.grazer.replenishIntervalSeconds, Limits.ReplenishIntervalMin,
                Limits.ReplenishIntervalMax);
            s.grazer.replenishAmount = Mathf.Clamp(s.grazer.replenishAmount, 0, Limits.GrazerReplenishAmountMax);
        }

        if (s.predator != null)
        {
            s.predator.startingPopulation = Mathf.Clamp(s.predator.startingPopulation, Limits.PredatorStartingPopulationMin,
                Limits.PredatorStartingPopulationMax);
            s.predator.maxPopulation = Mathf.Clamp(s.predator.maxPopulation, Limits.PredatorMaxPopulationMin,
                Limits.PredatorMaxPopulationMax);
            s.predator.replenishIntervalSeconds = Mathf.Clamp(s.predator.replenishIntervalSeconds,
                Limits.ReplenishIntervalMin, Limits.ReplenishIntervalMax);
            s.predator.replenishAmount = Mathf.Clamp(s.predator.replenishAmount, 0, Limits.PredatorReplenishAmountMax);
            if (!s.predator.replenishEnabled &&
                s.predator.replenishAmount >= Limits.PredatorReplenishAutoEnableThreshold)
                s.predator.replenishEnabled = true;
        }
    }

    static void ClampExpression(SimulationSettings s)
    {
        if (s.plant?.expression != null)
        {
            s.plant.expression.primaryStats = Mathf.Clamp(s.plant.expression.primaryStats, Limits.ExpressionTraitMin,
                Limits.ExpressionTraitMax);
            s.plant.expression.secondaryTraits = Mathf.Clamp(s.plant.expression.secondaryTraits, Limits.ExpressionTraitMin,
                Limits.ExpressionTraitMax);
            s.plant.expression.defenseTraits = Mathf.Clamp(s.plant.expression.defenseTraits, Limits.ExpressionTraitMin,
                Limits.ExpressionTraitMax);
        }

        if (s.grazer?.expression != null)
        {
            s.grazer.expression.statTraits = Mathf.Clamp(s.grazer.expression.statTraits, Limits.ExpressionTraitMin,
                Limits.ExpressionTraitMax);
            s.grazer.expression.rareTraits = Mathf.Clamp(s.grazer.expression.rareTraits, Limits.ExpressionTraitMin,
                Limits.ExpressionTraitMax);
            s.grazer.expression.packTraits = Mathf.Clamp(s.grazer.expression.packTraits, Limits.ExpressionTraitMin,
                Limits.ExpressionTraitMax);
        }

        if (s.predator?.expression != null)
        {
            s.predator.expression.statTraits = Mathf.Clamp(s.predator.expression.statTraits, Limits.ExpressionTraitMin,
                Limits.ExpressionTraitMax);
            s.predator.expression.rareTraits = Mathf.Clamp(s.predator.expression.rareTraits, Limits.ExpressionTraitMin,
                Limits.ExpressionTraitMax);
            s.predator.expression.apexTraits = Mathf.Clamp(s.predator.expression.apexTraits, Limits.ExpressionTraitMin,
                Limits.ExpressionTraitMax);
        }
    }
}
