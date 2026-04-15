// -----------------------------------------------------------------------------
// Validates SimulationSettings after JSON parse and clamps cross-field rules.
// -----------------------------------------------------------------------------

using UnityEngine;

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

        if (s.terrain != null)
        {
            s.terrain.obstacleMinCluster = Mathf.Max(1, s.terrain.obstacleMinCluster);
            s.terrain.obstacleMaxCluster = Mathf.Max(s.terrain.obstacleMinCluster, s.terrain.obstacleMaxCluster);
            s.terrain.mapWidth = Mathf.Clamp(s.terrain.mapWidth, 20, 500);
            s.terrain.mapHeight = Mathf.Clamp(s.terrain.mapHeight, 20, 500);
        }

        ClampInPlace(s);
        return true;
    }

    /// <summary> Copy legacy obstacle/water fields into rockSpawnRate / waterSpawnRate when JSON predates those keys. </summary>
    static void MigrateTerrainV1(TerrainSettingsData t)
    {
        if (t.rockSpawnRate <= 0f && t.obstacleSpawnChance > 0f)
            t.rockSpawnRate = Mathf.Clamp(t.obstacleSpawnChance, 0.0005f, 0.015f);

        if (t.waterSpawnRate <= 0f && t.waterThreshold > 0f)
        {
            // Old: blue if perlin > waterThreshold (higher threshold => less water). Approximate inverse for 0–1 rate.
            t.waterSpawnRate = Mathf.Clamp01(Mathf.InverseLerp(0.9f, 0.5f, t.waterThreshold));
        }

        if (t.rockSpawnRate <= 0f)
            t.rockSpawnRate = 0.004f;
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
            s.terrain.rockSpawnRate = Mathf.Clamp(s.terrain.rockSpawnRate, 0.0005f, 0.015f);
            s.terrain.waterSpawnRate = Mathf.Clamp01(s.terrain.waterSpawnRate);
            s.terrain.obstacleMinCluster = Mathf.Max(1, s.terrain.obstacleMinCluster);
            s.terrain.obstacleMaxCluster = Mathf.Max(s.terrain.obstacleMinCluster, s.terrain.obstacleMaxCluster);
            s.terrain.mapWidth = Mathf.Clamp(s.terrain.mapWidth, 20, 500);
            s.terrain.mapHeight = Mathf.Clamp(s.terrain.mapHeight, 20, 500);
            s.terrain.perlinScale = Mathf.Clamp(s.terrain.perlinScale, 0.02f, 0.3f);
        }

        ClampExpression(s);
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
