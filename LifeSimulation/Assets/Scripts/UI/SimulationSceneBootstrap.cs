// -----------------------------------------------------------------------------
// Ensures SimulationSettingsStore exists and builds world editor settings UI
// once per Simulation scene load.
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
