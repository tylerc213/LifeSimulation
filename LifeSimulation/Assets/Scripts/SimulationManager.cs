// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Simulation GUI
// Requirement:	Sim Editor
// Author:		Robert Amborski
// Date:		3/25/2026
// Version:		0.0.0
//
// Description:
//    
// -----------------------------------------------------------------------------

using UnityEngine;
using System.Collections.Generic;

public class SimulationManager : MonoBehaviour
{
    //
    public static SimulationManager Instance;

    //
    public Dictionary<string, int> population = new Dictionary<string, int>();

    //
    [Header("Simulation State")]
    public float currentSpeed = 1.0f;
    private bool isHalted = false;
    public SimulationSceneHandler simulationSceneHandler;

    //
    void Awake() => Instance = this;

    //
    public void SetSimulationSpeed(float Speed)
    {
        currentSpeed = Mathf.Clamp(Speed, 0f, 100f);
        Time.timeScale = currentSpeed;

        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }

    //
    public void UpdatePopulation(string uniqueID, int change)
    {
        if (!population.ContainsKey(uniqueID))
        {
            population.Add(uniqueID, 0);
        }

        population[uniqueID] += change;

        if (population[uniqueID] <= 0 && !isHalted)
        {
            isHalted = true;
            Time.timeScale = 0;

            if (simulationSceneHandler != null)
            {
                simulationSceneHandler.QuitToScoreSummary();
            }
        }
    }
}
