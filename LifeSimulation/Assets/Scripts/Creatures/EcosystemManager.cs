using System;
using System.Diagnostics;
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

    [Header("Initial Counts")]
    [SerializeField] private int startPlants = 15;
    [SerializeField] private int startGrazers = 8;
    [SerializeField] private int startPredators = 2;

    [Header("Auto-replenish Plants")]
    [SerializeField] private bool autoReplenishPlants = true;
    [SerializeField] private float plantReplenishInterval = 5f;
    [SerializeField] private int plantReplenishAmount = 3;

    // Live counters — incremented immediately on spawn, decremented on death.
    // This avoids the one-frame lag of FindGameObjectsWithTag, which caused
    // multiple entities to spawn before Unity registered the previous ones.
    private int _plantCount;
    private int _grazerCount;
    private int _predatorCount;

    private float _replenishTimer;

    // ── Unity ─────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Wait one frame so BoundaryManager has initialised
        Invoke(nameof(SpawnInitial), 0.05f);
    }

    private void Update()
    {
        if (!autoReplenishPlants) return;
        _replenishTimer += Time.deltaTime;
        if (_replenishTimer >= plantReplenishInterval)
        {
            _replenishTimer = 0f;
            for (int i = 0; i < plantReplenishAmount; i++)
                SpawnPlant(RandomPosition());
        }
    }

    // ── Initial Spawning ──────────────────────────────────────────────────────
    private void SpawnInitial()
    {
        for (int i = 0; i < startPlants; i++) DoSpawnPlant(RandomPosition());
        for (int i = 0; i < startGrazers; i++) DoSpawnGrazer(RandomPosition());
        for (int i = 0; i < startPredators; i++) DoSpawnPredator(RandomPosition());
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
        Vector2 spawnPos = Offset(position);
        DoSpawnGrazer(spawnPos);
    }

    public void SpawnPredator(Vector2 position)
    {
        if (_predatorCount >= maxPredators) return;
        Vector2 spawnPos = Offset(position);
        DoSpawnPredator(spawnPos);
    }

    /// <summary>
    /// Called by your click-spawner. Spawns exactly ONE entity, ignoring caps,
    /// so the player's intent is always respected.
    /// </summary>
    public void ManualSpawnPlant(Vector2 position) => DoSpawnPlant(position);
    public void ManualSpawnGrazer(Vector2 position) => DoSpawnGrazer(position);
    public void ManualSpawnPredator(Vector2 position) => DoSpawnPredator(position);

    // ── Internal Spawn Helpers (each spawns exactly ONE) ─────────────────────

    private void DoSpawnPlant(Vector2 position)
    {
        UnityEngine.Debug.Log($"DoSpawnGrazer called. Prefab null? {grazerprefab == null}, Count: {_grazerCount}, Max: {maxGrazers}");
        if (plantPrefab == null) return;
        GameObject go = Instantiate(plantPrefab, position, Quaternion.identity);
        _plantCount++;
        // Plant doesn't extend EntityBase, so use PlantDeathProxy to catch Destroy
        PlantDeathProxy proxy = go.AddComponent<PlantDeathProxy>();
        proxy.Init(() => _plantCount--);
    }

    private void DoSpawnGrazer(Vector2 spawnPos)
    {
        if (grazerprefab == null) return;
        if (BoundaryManager.Instance != null)
            spawnPos = BoundaryManager.Instance.Clamp(spawnPos);
        GameObject go = Instantiate(grazerprefab, spawnPos, Quaternion.identity);
        _grazerCount++;
        go.GetComponent<EntityBase>()?.OnDeath.AddListener(() => _grazerCount--);
    }

    private void DoSpawnPredator(Vector2 spawnPos)
    {
        if (predatorPrefab == null) return;
        if (BoundaryManager.Instance != null)
            spawnPos = BoundaryManager.Instance.Clamp(spawnPos);
        GameObject go = Instantiate(predatorPrefab, spawnPos, Quaternion.identity);
        _predatorCount++;
        go.GetComponent<EntityBase>()?.OnDeath.AddListener(() => _predatorCount--);
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