using System;
using UnityEngine;

/// <summary>
/// EcosystemManager — central spawner and population controller.
/// Attach to a single "Managers" GameObject in the scene.
///
/// Drag your Plant, Grazer, and Predator prefabs into the Inspector.
/// Make sure each prefab has the matching Tag set ("Plant", "Grazer", "Predator").
/// </summary>
public class EcosystemManager : MonoBehaviour
{
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

    // ── Unity ─────────────────────────────────────────────────────────────────
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

    // ── Public Spawn Methods ──────────────────────────────────────────────────

    /// <summary>
    /// Called by reproduction logic (birth). Respects population caps.
    /// </summary>
    public void SpawnPlant(Vector2 position)
    {
        if (_plantCount >= maxPlants) return;
        DoSpawnPlant(position);
    }

    public void SpawnGrazer(Vector2 position)
    {
        if (_grazerCount >= maxGrazers) return;
        DoSpawnGrazer(Offset(position), null);
    }

    public void SpawnPredator(Vector2 position)
    {
        if (_predatorCount >= maxPredators) return;
        DoSpawnPredator(Offset(position), null);
    }

    /// <summary>
    /// Called by click-spawner. Spawns exactly ONE entity, ignoring caps,
    /// so the player's intent is always respected.
    /// </summary>
    public void ManualSpawnPlant(Vector2 position) => DoSpawnPlant(position);
    public void ManualSpawnGrazer(Vector2 position) => DoSpawnGrazer(position, null);
    public void ManualSpawnPredator(Vector2 position) => DoSpawnPredator(position, null);

    /// <summary>Called by Grazer.TryReproduce — passes parent genomes for Mendelian inheritance.</summary>
    public void SpawnGrazerOffspring(Vector2 position, Genome parentA, Genome parentB)
    {
        if (_grazerCount >= maxGrazers) return;
        DoSpawnGrazer(Offset(position), Genome.Inherit(parentA, parentB));
    }

    /// <summary>Called by Predator.TryReproduce — passes parent genomes for Mendelian inheritance.</summary>
    public void SpawnPredatorOffspring(Vector2 position, Genome parentA, Genome parentB)
    {
        if (_predatorCount >= maxPredators) return;
        DoSpawnPredator(Offset(position), Genome.Inherit(parentA, parentB));
    }

    // ── Internal Spawn Helpers (each spawns exactly ONE) ─────────────────────

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

    // ── Helpers ───────────────────────────────────────────────────────────────
    private Vector2 Offset(Vector2 origin) => origin + UnityEngine.Random.insideUnitCircle * 1.5f;

    private Vector2 RandomPosition()
    {
        if (BoundaryManager.Instance != null)
            return BoundaryManager.Instance.RandomPosition();
        return UnityEngine.Random.insideUnitCircle * 5f;    // fallback before camera initialises
    }
}

/// <summary>
/// Tiny helper that fires a callback when its GameObject is destroyed.
/// Used so EcosystemManager can decrement _plantCount even though Plant
/// doesn't extend EntityBase (which has OnDeath built in).
/// </summary>
public class PlantDeathProxy : MonoBehaviour
{
    private System.Action _onDestroy;
    private bool _fired;

    public void Init(System.Action callback) => _onDestroy = callback;

    private void OnDestroy()
    {
        if (_fired) return;
        _fired = true;
        _onDestroy?.Invoke();
    }
}

