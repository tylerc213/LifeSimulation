// -----------------------------------------------------------------------------
// Project:     EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:        Lifeforms
// Requirement: Lifeform Simulation
// Author:      Luke Kivett
// Date:        4/6/2026
// Version:     0.0.0
//
// Description:
//    Central spawner and population controller for the simulation. Manages
//    plant, grazer, and predator population caps, manual and reproductive
//    spawning, and periodic plant replenishment.
// -----------------------------------------------------------------------------
using System;
using UnityEngine;

/// <summary>Controls all entity spawning and enforces population caps.</summary>
/// <remarks>
/// Attach to a single Managers GameObject. Assign Plant, Grazer, and Predator
/// prefabs in the Inspector. Each prefab must have its matching tag set.
/// Live integer counters are used instead of FindGameObjectsWithTag to avoid
/// the one-frame registration lag that caused cascade spawning.
/// </remarks>
public class EcosystemManager : MonoBehaviour
{
    /// <summary>Singleton instance.</summary>
    public static EcosystemManager Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private GameObject plantPrefab;
    [SerializeField] private GameObject grazerprefab;
    [SerializeField] private GameObject predatorPrefab;

    [Header("Population Caps")]
    [SerializeField] private int maxPlants = 40;
    [SerializeField] private int maxGrazers = 20;
    [SerializeField] private int maxPredators = 6;

    [Header("Auto-replenish Plants")]
    [SerializeField] private bool autoReplenishPlants = true;
    [SerializeField] private float plantReplenishInterval = 5f;
    [SerializeField] private int plantReplenishAmount = 3;

    [Header("Auto-replenish Grazers")]
    [SerializeField] private bool autoReplenishGrazers = false;
    [SerializeField] private float grazerReplenishInterval = 8f;
    [SerializeField] private int grazerReplenishAmount = 1;

    [Header("Auto-replenish Predators")]
    [SerializeField] private bool autoReplenishPredators = false;
    [SerializeField] private float predatorReplenishInterval = 15f;
    [SerializeField] private int predatorReplenishAmount = 1;

    // Live counters — incremented immediately on spawn, decremented on death.
    // This avoids the one-frame lag of FindGameObjectsWithTag, which caused
    // multiple entities to spawn before Unity registered the previous ones.
    private int _plantCount;
    private int _grazerCount;
    private int _predatorCount;

    // Must match PopTracker keys / tags used for SimulationManager.population.
    private const string PlantPopulationKey = "Plant";
    private const string GrazerPopulationKey = "Grazer";
    private const string PredatorPopulationKey = "Predator";

    /// <summary> Current counts for logging / UI; kept in sync with <see cref="SimulationManager.population"/> on spawn/death. </summary>
    public int PlantCount => _plantCount;
    public int GrazerCount => _grazerCount;
    public int PredatorCount => _predatorCount;

    private float _plantReplenishTimer;
    private float _grazerReplenishTimer;
    private float _predatorReplenishTimer;

    /// <summary>Initialises singleton reference.</summary>
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        MapGenerator2D.OnMapGenerated += ResetReplenishTimersAfterNewMap;
    }

    void OnDisable()
    {
        MapGenerator2D.OnMapGenerated -= ResetReplenishTimersAfterNewMap;
    }

    void ResetReplenishTimersAfterNewMap()
    {
        _plantReplenishTimer = 0f;
        _grazerReplenishTimer = 0f;
        _predatorReplenishTimer = 0f;
    }
    
    /// <summary>Periodically replenishes plants up to the population cap.</summary>
    private void Update()
    {
        // Auto-replenish must not run before Generate Map — otherwise plants spawn in empty space.
        if (!MapGenerator2D.IsSimulationRunActive)
            return;

        float dt = Time.deltaTime;

        if (autoReplenishPlants)
        {
            _plantReplenishTimer += dt;
            if (_plantReplenishTimer >= plantReplenishInterval)
            {
                _plantReplenishTimer = 0f;
                for (int i = 0; i < plantReplenishAmount; i++)
                    SpawnPlant(RandomPosition());
            }
        }

        if (autoReplenishGrazers)
        {
            _grazerReplenishTimer += dt;
            if (_grazerReplenishTimer >= grazerReplenishInterval)
            {
                _grazerReplenishTimer = 0f;
                for (int i = 0; i < grazerReplenishAmount; i++)
                    SpawnGrazer(RandomPosition());
            }
        }

        if (autoReplenishPredators)
        {
            _predatorReplenishTimer += dt;
            if (_predatorReplenishTimer >= predatorReplenishInterval)
            {
                _predatorReplenishTimer = 0f;
                for (int i = 0; i < predatorReplenishAmount; i++)
                    SpawnPredator(RandomPosition());
            }
        }
    }

    public void ConfigureFromSettings(PlantSettingsBlock plant, GrazerSettingsBlock grazer, PredatorSettingsBlock predator)
    {
        maxPlants = plant.maxPopulation;
        maxGrazers = grazer.maxPopulation;
        maxPredators = predator.maxPopulation;

        autoReplenishPlants = plant.replenishEnabled;
        plantReplenishInterval = plant.replenishIntervalSeconds;
        plantReplenishAmount = plant.replenishAmount;

        autoReplenishGrazers = grazer.replenishEnabled;
        grazerReplenishInterval = grazer.replenishIntervalSeconds;
        grazerReplenishAmount = grazer.replenishAmount;

        autoReplenishPredators = predator.replenishEnabled;
        predatorReplenishInterval = predator.replenishIntervalSeconds;
        predatorReplenishAmount = predator.replenishAmount;
    }

    /// <summary>Spawns a plant at a position, respecting the population cap.</summary>
    /// <param name="position">World-space spawn position.</param>
    public void SpawnPlant(Vector2 position)
    {
        if (_plantCount >= maxPlants) return;
        DoSpawnPlant(position);
    }

    /// <summary>Spawns a grazer at a position, respecting the population cap.</summary>
    /// <param name="position">World-space spawn position.</param>
    public void SpawnGrazer(Vector2 position)
    {
        if (_grazerCount >= maxGrazers) return;
        DoSpawnGrazer(Offset(position), null);
    }

    /// <summary>Spawns a predator at a position, respecting the population cap.</summary>
    /// <param name="position">World-space spawn position.</param>
    public void SpawnPredator(Vector2 position)
    {
        if (_predatorCount >= maxPredators) return;
        DoSpawnPredator(Offset(position), null);
    }

    /// <summary>Spawns exactly one entity ignoring caps; honours player intent from click spawner.</summary>
    /// <param name="position">World-space spawn position.</param>
    public void ManualSpawnPlant(Vector2 position) => DoSpawnPlant(position);

    /// <summary>Spawns exactly one grazer ignoring caps; honours player intent from click spawner.</summary>
    /// <param name="position">World-space spawn position.</param>
    public void ManualSpawnGrazer(Vector2 position) => DoSpawnGrazer(position, null);
    
    /// <summary>Spawns exactly one predator ignoring caps; honours player intent from click spawner.</summary>
    /// <param name="position">World-space spawn position.</param>
    public void ManualSpawnPredator(Vector2 position) => DoSpawnPredator(position, null);

    /// <summary>Spawns a grazer offspring with an inherited genome from two parents.</summary>
    /// <param name="position">World-space spawn position.</param>
    /// <param name="parentA">First parent genome.</param>
    /// <param name="parentB">Second parent genome.</param>
    public void SpawnGrazerOffspring(Vector2 position, Genome parentA, Genome parentB)
    {
        if (_grazerCount >= maxGrazers) return;
        DoSpawnGrazer(Offset(position), Genome.Inherit(parentA, parentB));
    }

    /// <summary>Spawns a predator offspring with an inherited genome from two parents.</summary>
    /// <param name="position">World-space spawn position.</param>
    /// <param name="parentA">First parent genome.</param>
    /// <param name="parentB">Second parent genome.</param>
    public void SpawnPredatorOffspring(Vector2 position, Genome parentA, Genome parentB)
    {
        if (_predatorCount >= maxPredators) return;
        DoSpawnPredator(Offset(position), Genome.Inherit(parentA, parentB));
    }

    /// <summary>Instantiates a plant, registers its death callback, and triggers icon refresh.</summary>
    /// <param name="position">World-space spawn position.</param>
    private void DoSpawnPlant(Vector2 position)
    {
        if (plantPrefab == null) return;
        GameObject go = Instantiate(plantPrefab, position, Quaternion.identity);
        _plantCount++;
        SimulationManager.Instance?.UpdatePopulation(PlantPopulationKey, 1);
        PlantDeathProxy proxy = go.AddComponent<PlantDeathProxy>();
        proxy.Init(() =>
        {
            _plantCount--;
            SimulationManager.Instance?.UpdatePopulation(PlantPopulationKey, -1);
        });
        // Icons built after one frame so PlantGenetics.Awake has run
        go.GetComponent<TraitIconDisplay>()?.Refresh();
    }

    /// <summary>Instantiates a grazer, registers its death callback, and optionally applies a genome.</summary>
    /// <param name="spawnPos">World-space spawn position.</param>
    /// <param name="genome">Inherited genome, or null for a random one.</param>
    private void DoSpawnGrazer(Vector2 spawnPos, Genome genome)
    {
        if (grazerprefab == null) return;
        if (BoundaryManager.Instance != null)
            spawnPos = BoundaryManager.Instance.Clamp(spawnPos);
        GameObject go = Instantiate(grazerprefab, spawnPos, Quaternion.identity);
        _grazerCount++;
        SimulationManager.Instance?.UpdatePopulation(GrazerPopulationKey, 1);
        go.GetComponent<EntityBase>()?.OnDeath.AddListener(() =>
        {
            _grazerCount--;
            SimulationManager.Instance?.UpdatePopulation(GrazerPopulationKey, -1);
        });
        if (genome != null)
        {
            go.GetComponent<GrazerGenetics>()?.Init(genome);
            go.GetComponent<TraitIconDisplay>()?.Refresh();
        }
    }

    /// <summary>Instantiates a predator, registers its death callback, and optionally applies a genome.</summary>
    /// <param name="spawnPos">World-space spawn position.</param>
    /// <param name="genome">Inherited genome, or null for a random one.</param>
    private void DoSpawnPredator(Vector2 spawnPos, Genome genome)
    {
        if (predatorPrefab == null) return;
        if (BoundaryManager.Instance != null)
            spawnPos = BoundaryManager.Instance.Clamp(spawnPos);
        GameObject go = Instantiate(predatorPrefab, spawnPos, Quaternion.identity);
        _predatorCount++;
        SimulationManager.Instance?.UpdatePopulation(PredatorPopulationKey, 1);
        go.GetComponent<EntityBase>()?.OnDeath.AddListener(() =>
        {
            _predatorCount--;
            SimulationManager.Instance?.UpdatePopulation(PredatorPopulationKey, -1);
        });
        if (genome != null)
        {
            go.GetComponent<PredatorGenetics>()?.Init(genome);
            go.GetComponent<TraitIconDisplay>()?.Refresh();
        }
    }

    /// <summary>Returns a position slightly offset from an origin to separate offspring from parent.</summary>
    /// <param name="origin">Parent world-space position.</param>
    /// <returns>Offset world-space position.</returns>
    private Vector2 Offset(Vector2 origin) => origin + UnityEngine.Random.insideUnitCircle * 1.5f;

    /// <summary>Returns a random position within the current map bounds.</summary>
    /// <returns>Random world-space position inside boundaries.</returns>
    private Vector2 RandomPosition()
    {
        if (BoundaryManager.Instance != null)
            return BoundaryManager.Instance.RandomPosition();
        return UnityEngine.Random.insideUnitCircle * 5f;    // fallback before camera initialises
    }
}

/// <summary>Fires a callback when its GameObject is destroyed.</summary>
/// <remarks>
/// Added to plant GameObjects at spawn so EcosystemManager can decrement
/// its plant counter without Plant extending EntityBase.
/// </remarks>
public class PlantDeathProxy : MonoBehaviour
{
    private System.Action _onDestroy;
    private bool _fired;

    /// <summary>Registers the callback to invoke on destruction.</summary>
    /// <param name="callback">Action to call when this object is destroyed.</param>
    public void Init(System.Action callback) => _onDestroy = callback;

    /// <summary>Invokes the registered callback once on destruction.</summary>
    private void OnDestroy()
    {
        if (_fired) return;
        _fired = true;
        _onDestroy?.Invoke();
    }
}

