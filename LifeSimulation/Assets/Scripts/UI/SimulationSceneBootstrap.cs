// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Simulation scene bootstrap
// Requirement:	Simulation user interface
// Author:		Benjamin Jones
// Date:		04/14/2026
// Version:		0.0.0
//
// Description:
//    Runs once when entering the sim: ensures settings infrastructure exists,
//    then brings up the canvas editor so tuning and play share one coherent entry path.
// -----------------------------------------------------------------------------

using UnityEngine;

/// <summary> Ensures settings store and world editor UI exist on Simulation load. </summary>
public class SimulationSceneBootstrap : MonoBehaviour
{
    void Start()
    {
        EnsureSimulationSettingsStore();
        Canvas canvas = FindFirstObjectByType<Canvas>();
        WorldEditorUIBuilder.EnsureBuilt(canvas);
    }

    static void EnsureSimulationSettingsStore()
    {
        if (FindFirstObjectByType<SimulationSettingsStore>() != null)
            return;

        new GameObject("SimulationSettingsStore").AddComponent<SimulationSettingsStore>();
    }
}
