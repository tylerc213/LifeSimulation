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

public class SimulationSceneBootstrap : MonoBehaviour
{
    void Start()
    {
        if (FindFirstObjectByType<SimulationSettingsStore>() == null)
        {
            GameObject go = new GameObject("SimulationSettingsStore");
            go.AddComponent<SimulationSettingsStore>();
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        WorldEditorUIBuilder.EnsureBuilt(canvas);
    }
}
