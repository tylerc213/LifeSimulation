// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Simulation Analysis
// Requirement:	Event Log
// Author:		Caden Nieves
// Date:		03/04/2026
// Version:		0.1.0
//
// Description:
//    Tracks current population counts for each lifeform type and generates
//    PopSnapshot objects when requested by the logging system.
// -----------------------------------------------------------------------------

using UnityEngine;

/// <summary>
/// Provides populaation data for logging system by generating PopSnapshot
/// objects when requested by system.
/// </summary>
public class PopTracker : MonoBehaviour
{
    [Header("Optional Sources")]
    [Tooltip("Authoritative runtime counts (spawn/death). Prefer this over SimulationManager.population.")]
    public EcosystemManager ecosystemManager;

    [Tooltip("Only updated when placing via WorldEditor; used if EcosystemManager is missing.")]
    public SimulationManager simulationManager;

    public string plantPopulationKey = "Plant";
    public string grazerPopulationKey = "Grazer";
    public string predatorPopulationKey = "Predator";

    /// <summary>
    /// Generates population snapshot for current tick
    /// </summary>
    /// <param name="tick">Current tick used to timestamp snapshot</param>
    /// <returns>PopSnapshot containing population counts</returns>
    public PopSnapshot GetSnapshot(int tick)
    {
        int plants;
        int grazers;
        int predators;

        EcosystemManager eco = ecosystemManager != null ? ecosystemManager : EcosystemManager.Instance;
        if (eco != null)
        {
            plants = eco.PlantCount;
            grazers = eco.GrazerCount;
            predators = eco.PredatorCount;
        }
        else
        {
            plants = TryGetPopulation(plantPopulationKey);
            grazers = TryGetPopulation(grazerPopulationKey);
            predators = TryGetPopulation(predatorPopulationKey);
        }

        return new PopSnapshot(tick, plants, grazers, predators);
    }

    private int TryGetPopulation(string key)
    {
        if (simulationManager == null || string.IsNullOrWhiteSpace(key))
        {
            return 0;
        }

        if (simulationManager.population == null || !simulationManager.population.TryGetValue(key, out int count))
        {
            return 0;
        }

        return Mathf.Max(0, count);
    }
}
