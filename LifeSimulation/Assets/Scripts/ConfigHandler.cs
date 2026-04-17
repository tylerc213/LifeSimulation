// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Configuration GUI
// Requirement:	Configuration
// Author:		Benjamin Jones
// Date:		04/05/2026
// Version:		0.0.0
//
// Description:
//    Configuration scene: import/export simulation settings as JSON, hold
//    last imported text for downstream wiring, and navigate back to main menu.
// -----------------------------------------------------------------------------

using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary> handles configuration scene JSON import/export </summary>
public class ConfigHandler : MonoBehaviour
{
    [Header("Scene Configuration")]
    public string mainMenuSceneName = "MainMenu";

    [Header("JSON Configuration")]
    public string defaultConfigFileName = "simulation_config.json";

    /// <summary> Raw JSON from the last successful import; for future use. </summary>
    public string ImportedConfigurationJson { get; private set; }

    /// <summary> picks a JSON file, stores contents in ImportedConfigurationJson </summary>
    public void ImportConfigurationJson()
    {
        Debug.Log("Import Configuration JSON Selected");
        if (!TryGetImportJsonPath(out string path))
            return;

        string json = File.ReadAllText(path);
        ImportedConfigurationJson = json;

        if (!SimulationSettingsStore.TryDeserializeAndValidate(json, out SimulationSettings settings, out string error))
        {
            Debug.LogWarning("Config import failed: " + error);
            return;
        }

        try
        {
            File.WriteAllText(GetDefaultConfigPath(), json);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Could not persist imported config: " + e.Message);
        }

        if (SimulationSettingsStore.Instance != null)
            SimulationSettingsStore.Instance.ReplaceAndApply(settings, saveToDisk: false);
    }

    /// <summary> writes persisted simulation settings (or in-memory store when present) to a chosen file </summary>
    public void ExportConfigurationJson()
    {
        Debug.Log("Export Configuration JSON Selected");
        if (!TryGetExportJsonPath(out string path))
            return;

        SimulationSettings settings = SimulationSettingsStore.Instance != null
            ? SimulationSettingsStore.Instance.Current
            : SimulationSettingsStore.LoadPersistedOrDefaults(defaultConfigFileName);

        File.WriteAllText(path, JsonUtility.ToJson(settings, true));
    }

    /// <summary> returns to main menu scene </summary>
    public void BackToMainMenu()
    {
        Debug.Log("Back To Main Menu Selected");
        SceneManager.LoadScene(mainMenuSceneName);
    }

    bool TryGetImportJsonPath(out string path)
    {
        path = null;
#if UNITY_EDITOR
        path = EditorUtility.OpenFilePanel("Import JSON", "", "json");
        return !string.IsNullOrEmpty(path);
#else
        path = GetDefaultConfigPath();
        if (File.Exists(path))
            return true;
        Debug.LogWarning("Import: place a JSON file at " + path + " or run in the Editor to choose a file.");
        return false;
#endif
    }

    bool TryGetExportJsonPath(out string path)
    {
        path = null;
#if UNITY_EDITOR
        path = EditorUtility.SaveFilePanel("Export JSON", "", defaultConfigFileName, "json");
        return !string.IsNullOrEmpty(path);
#else
        path = GetDefaultConfigPath();
        return true;
#endif
    }

    string GetDefaultConfigPath()
    {
        return Path.Combine(Application.persistentDataPath, defaultConfigFileName);
    }
}
