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
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
using SFB;
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
#if UNITY_EDITOR
        if (!TryGetImportJsonPathEditor(out string path))
            return;
        ApplyImportedJsonFromPath(path);
#elif UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
        var jsonFilters = new ExtensionFilter[]
        {
            new ExtensionFilter("JSON", "json"),
            new ExtensionFilter("All files", "*"),
        };
        StandaloneFileBrowser.OpenFilePanelAsync(
            "Import JSON",
            "",
            jsonFilters,
            false,
            paths =>
            {
                if (paths == null || paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
                    return;
                ApplyImportedJsonFromPath(paths[0]);
            });
#else
        if (!TryGetImportJsonPathFallback(out string path))
            return;
        ApplyImportedJsonFromPath(path);
#endif
    }

    /// <summary> writes persisted simulation settings (or in-memory store when present) to a chosen file </summary>
    public void ExportConfigurationJson()
    {
        Debug.Log("Export Configuration JSON Selected");
#if UNITY_EDITOR
        if (!TryGetExportJsonPathEditor(out string path))
            return;
        WriteExportToPath(path);
#elif UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
        var jsonFilters = new ExtensionFilter[]
        {
            new ExtensionFilter("JSON", "json"),
            new ExtensionFilter("All files", "*"),
        };
        StandaloneFileBrowser.SaveFilePanelAsync(
            "Export JSON",
            "",
            defaultConfigFileName,
            jsonFilters,
            path =>
            {
                if (string.IsNullOrEmpty(path))
                    return;
                WriteExportToPath(path);
            });
#else
        if (!TryGetExportJsonPathFallback(out string path))
            return;
        WriteExportToPath(path);
#endif
    }

    /// <summary> returns to main menu scene </summary>
    public void BackToMainMenu()
    {
        Debug.Log("Back To Main Menu Selected");
        SceneManager.LoadScene(mainMenuSceneName);
    }

    void ApplyImportedJsonFromPath(string path)
    {
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

    void WriteExportToPath(string path)
    {
        SimulationSettings settings = SimulationSettingsStore.Instance != null
            ? SimulationSettingsStore.Instance.Current
            : SimulationSettingsStore.LoadPersistedOrDefaults(defaultConfigFileName);

        File.WriteAllText(path, JsonUtility.ToJson(settings, true));
    }

#if UNITY_EDITOR
    bool TryGetImportJsonPathEditor(out string path)
    {
        path = EditorUtility.OpenFilePanel("Import JSON", "", "json");
        return !string.IsNullOrEmpty(path);
    }

    bool TryGetExportJsonPathEditor(out string path)
    {
        path = EditorUtility.SaveFilePanel("Export JSON", "", defaultConfigFileName, "json");
        return !string.IsNullOrEmpty(path);
    }
#endif

#if !(UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX)
    bool TryGetImportJsonPathFallback(out string path)
    {
        path = null;
        path = GetDefaultConfigPath();
        if (File.Exists(path))
            return true;
        Debug.LogWarning("Import: place a JSON file at " + path + " or use a desktop build to choose a file.");
        return false;
    }

    bool TryGetExportJsonPathFallback(out string path)
    {
        path = GetDefaultConfigPath();
        return true;
    }
#endif

    string GetDefaultConfigPath()
    {
        return Path.Combine(Application.persistentDataPath, defaultConfigFileName);
    }
}
