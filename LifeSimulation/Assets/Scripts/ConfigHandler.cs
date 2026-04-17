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

        ImportedConfigurationJson = File.ReadAllText(path);
        ValidateConfigurationSchema(ImportedConfigurationJson);
        ApplyConfigurationToDataSources(ImportedConfigurationJson);
    }

    /// <summary> saves a blank JSON stub via save dialog (Editor) or default path (player) </summary>
    public void ExportConfigurationJson()
    {
        Debug.Log("Export Configuration JSON Selected");
        if (!TryGetExportJsonPath(out string path))
            return;

        File.WriteAllText(path, BlankExportJson());
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

    static string BlankExportJson()
    {
        return "{}";
    }

    /// <summary> future: enforce JSON schema </summary>
    void ValidateConfigurationSchema(string json)
    {
    }

    /// <summary> future: push imported values into sim data sources </summary>
    void ApplyConfigurationToDataSources(string json)
    {
    }
}
