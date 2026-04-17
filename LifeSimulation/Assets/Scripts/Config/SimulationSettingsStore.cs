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

/// <summary> Single instance in Simulation scene (e.g. on Managers). </summary>
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

        if (!SimulationSettingsValidator.TryValidate(settings, out error))
            return false;

        return true;
    }

    public void ReplaceAndApply(SimulationSettings s, bool saveToDisk)
    {
        if (s == null) return;
        SimulationSettingsValidator.TryValidate(s, out _);
        Current = s;
        ApplyAll();
        if (saveToDisk)
            SaveToDisk();
    }

    public void ResetToDefaults()
    {
        Current = SimulationSettings.CreateDefaults();
        SimulationSettingsValidator.TryValidate(Current, out _);
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

        UIHandler ui = FindFirstObjectByType<UIHandler>();
        if (ui != null)
        {
            if (ui.widthInput != null)
                ui.widthInput.SetTextWithoutNotify(Current.terrain.mapWidth.ToString());
            if (ui.heightInput != null)
                ui.heightInput.SetTextWithoutNotify(Current.terrain.mapHeight.ToString());
        }
    }

    void ApplyEcosystem()
    {
        EcosystemManager eco = EcosystemManager.Instance ?? FindFirstObjectByType<EcosystemManager>();
        if (eco == null || Current.plant == null || Current.grazer == null || Current.predator == null)
            return;

        eco.ConfigureFromSettings(Current.plant, Current.grazer, Current.predator);
    }

    /// <summary> Call after EcosystemManager has finished Awake — fixes load order where ApplyAll ran before Instance existed. </summary>
    public void ReapplyEcosystemOnly()
    {
        ApplyEcosystem();
    }

    /// <summary> Call after a slider changes a subset of Current (mutate Current then call). </summary>
    public void CommitFromCurrent()
    {
        SimulationSettingsValidator.TryValidate(Current, out _);
        ApplyAll();
        SaveToDisk();
    }
}
