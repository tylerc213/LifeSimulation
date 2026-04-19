// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Settings store and apply
// Requirement:	Configuration
// Author:		Benjamin Jones
// Date:		04/14/2026
// Version:		0.0.0
//
// Description:
//    Owns the live settings document: load/save JSON, then fan changes out so
//    map generation, creatures, speed, and UI stay aligned with what the player configured.
// -----------------------------------------------------------------------------

using System;
using System.IO;
using UnityEngine;

/// <summary> Loads, saves, and applies <see cref="SimulationSettings"/> in the Simulation scene. </summary>
[DefaultExecutionOrder(-100)]
public class SimulationSettingsStore : MonoBehaviour
{
    public static SimulationSettingsStore Instance { get; private set; }

    [SerializeField] private string localFileName = "simulation_config.json";

    public SimulationSettings Current { get; private set; } = SimulationSettings.CreateDefaults();

    public event Action SettingsApplied;

    public string LocalFilePath => Path.Combine(Application.persistentDataPath, localFileName);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        LoadFromDiskOrDefaults();
        ApplyAll();
    }

    public void LoadFromDiskOrDefaults()
    {
        try
        {
            if (File.Exists(LocalFilePath))
            {
                string json = File.ReadAllText(LocalFilePath);
                if (TryDeserializeAndValidate(json, out SimulationSettings s, out _))
                {
                    Current = s;
                    return;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("SimulationSettingsStore: load failed: " + e.Message);
        }

        Current = SimulationSettings.CreateDefaults();
    }

    public static bool TryDeserializeAndValidate(string json, out SimulationSettings settings, out string error)
    {
        settings = null;
        error = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            error = "Empty JSON.";
            return false;
        }

        try
        {
            settings = JsonUtility.FromJson<SimulationSettings>(json);
        }
        catch (Exception e)
        {
            error = e.Message;
            return false;
        }

        return SimulationSettingsValidator.TryValidate(settings, out error);
    }

    /// <summary>
    /// Loads validated settings from persistent storage without needing <see cref="Instance"/>
    /// (e.g. Configuration scene export/import).
    /// </summary>
    public static SimulationSettings LoadPersistedOrDefaults(string localFileName)
    {
        string path = Path.Combine(Application.persistentDataPath, localFileName);
        try
        {
            if (!File.Exists(path))
                return SimulationSettings.CreateDefaults();

            string json = File.ReadAllText(path);
            if (TryDeserializeAndValidate(json, out SimulationSettings s, out string error))
                return s;

            Debug.LogWarning("SimulationSettingsStore: invalid persisted config, using defaults: " + error);
        }
        catch (Exception e)
        {
            Debug.LogWarning("SimulationSettingsStore: could not read persisted config: " + e.Message);
        }

        return SimulationSettings.CreateDefaults();
    }

    public void ReplaceAndApply(SimulationSettings s, bool saveToDisk)
    {
        if (s == null)
            return;

        if (!SimulationSettingsValidator.TryValidate(s, out string error))
        {
            Debug.LogWarning("SimulationSettingsStore: ReplaceAndApply failed validation: " + error);
            return;
        }

        Current = s;
        ApplyAll();
        if (saveToDisk)
            SaveToDisk();
    }

    public void ResetToDefaults()
    {
        Current = SimulationSettings.CreateDefaults();
        ApplyAll();
        SaveToDisk();
    }

    public void SaveToDisk()
    {
        try
        {
            File.WriteAllText(LocalFilePath, JsonUtility.ToJson(Current, true));
        }
        catch (Exception e)
        {
            Debug.LogWarning("SimulationSettingsStore: save failed: " + e.Message);
        }
    }

    public void ApplyAll()
    {
        if (Current == null)
            Current = SimulationSettings.CreateDefaults();

        ExpressionStrengthRuntime.ApplyFrom(Current);
        ApplyGame();
        ApplyTerrainToGenerators();
        ApplyEcosystem();
        SettingsApplied?.Invoke();
    }

    public void ReapplyEcosystemOnly()
    {
        ApplyEcosystem();
    }

    public void CommitFromCurrent()
    {
        if (!SimulationSettingsValidator.TryValidate(Current, out string error))
        {
            Debug.LogWarning("SimulationSettingsStore: CommitFromCurrent failed validation: " + error);
            return;
        }

        ApplyAll();
        SaveToDisk();
    }

    void ApplyGame()
    {
        if (SimulationManager.Instance == null || Current.game == null)
            return;

        SimulationManager.Instance.ApplySettingsSpeed(Current.game.simulationSpeed);
    }

    void ApplyTerrainToGenerators()
    {
        MapGenerator2D map = FindFirstObjectByType<MapGenerator2D>();
        if (map == null || Current.terrain == null)
            return;

        map.rockSpawnChance = Current.terrain.rockSpawnRate;
        map.waterSpawnRate = Current.terrain.waterSpawnRate;
        map.obstacleMinCluster = Current.terrain.obstacleMinCluster;
        map.obstacleMaxCluster = Current.terrain.obstacleMaxCluster;
        map.perlinScale = Current.terrain.perlinScale;
        map.startPlants = Current.plant.startingPopulation;
        map.startGrazers = Current.grazer.startingPopulation;
        map.startPredators = Current.predator.startingPopulation;
    }

    void ApplyEcosystem()
    {
        EcosystemManager eco = EcosystemManager.Instance ?? FindFirstObjectByType<EcosystemManager>();
        if (eco == null || Current.plant == null || Current.grazer == null || Current.predator == null)
            return;

        eco.ConfigureFromSettings(Current.plant, Current.grazer, Current.predator);
    }
}
