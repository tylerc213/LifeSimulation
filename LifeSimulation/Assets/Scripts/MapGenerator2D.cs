// -----------------------------------------------------------------------------
// Project:     EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:        Simulation GUI
// Requirement: Sim Editor
// Author:      Robert Amborski
// Date:        03/25/2026
//
// Description:
//    Procedurally generates the simulation world using tilemaps, Perlin noise,
//    and obstacle clustering. Initializes environmental layout and spawns the
//    starting populations of plants, grazers, and predators.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Handles terrain generation and initial ecosystem setup.
/// </summary>
/// <remarks>
/// Acts as the entry point for each simulation run. Responsible for resetting
/// previous state, generating terrain, and spawning initial life forms.
/// </remarks>
public class MapGenerator2D : MonoBehaviour
{
    public static MapGenerator2D Instance { get; private set; }

    /// <summary>
    /// Indicates whether a valid simulation run is currently active.
    /// </summary>
    public static bool IsSimulationRunActive =>
        Instance != null && Instance.IsMapReady && Instance.HasSimulationStarted;

    /// <summary>
    /// Fired after map generation completes (terrain + obstacles ready).
    /// </summary>
    public static event Action OnMapGenerated;

    [Header("Tilemap References")]
    public Tilemap squareTilemap;
    public TileBase baseSquareTile;

    [Header("Obstacle Generation")]
    [Tooltip("Prefab used for obstacle placement.")]
    public GameObject obstaclePrefab;

    [Tooltip("Chance per tile to attempt spawning an obstacle cluster.")]
    [Range(0.0005f, 0.015f)]
    public float rockSpawnChance = 0.004f;

    public int obstacleMinCluster = 1;
    public int obstacleMaxCluster = 2;

    [Header("Terrain Coloring (Perlin Noise)")]
    public float perlinScale = 0.1f;

    [Tooltip("Controls ratio of water vs land tiles.")]
    [Range(0f, 1f)]
    public float waterSpawnRate = 0.35f;

    [Header("Initial Population")]
    public int startPlants = 15;
    public int startGrazers = 8;
    public int startPredators = 2;

    private float _currentOffsetX;
    private float _currentOffsetY;

    // Stores valid spawn positions (non-obstacle tiles)
    private List<Vector3> _openTiles = new List<Vector3>();

    /// <summary> True when terrain and obstacles are fully generated </summary>
    public bool IsMapReady { get; private set; }

    /// <summary> True once at least one simulation run has started </summary>
    public bool HasSimulationStarted { get; private set; }

    private void Awake()
    {
        // Enforce singleton pattern to prevent conflicting generators
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("MapGenerator2D: Duplicate instance detected — destroying.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Generates terrain, obstacles, and initializes simulation entities.
    /// </summary>
    public void GenerateMap()
    {
        // Reset simulation state flags
        IsMapReady = false;
        HasSimulationStarted = false;

        SimulationManager sim = SimulationManager.Instance;

        // Prevent population systems from reacting during reset
        sim?.ResetPopulationState();
        sim?.SetSuppressPopulationSync(true);

        // Determine map size based on camera preset
        int sizeDim = 300;
        var camHandler = FindFirstObjectByType<CameraHandler>();

        if (camHandler != null)
        {
            switch (camHandler.selectedSize)
            {
                case CameraHandler.MapSize.Small: sizeDim = 50; break;
                case CameraHandler.MapSize.Medium: sizeDim = 100; break;
                case CameraHandler.MapSize.Large: sizeDim = 300; break;
            }
        }

        // Clear previous terrain and cached spawn positions
        squareTilemap.ClearAllTiles();
        _openTiles.Clear();

        // Remove leftover entities from prior simulation runs
        DestroyTagged("Plant");
        DestroyTagged("Grazer");
        DestroyTagged("Predator");
        DestroyTagged("Obstacle");

        sim?.SetSuppressPopulationSync(false);

        // Seed randomness for unique terrain generation
        UnityEngine.Random.InitState((int)DateTime.Now.Ticks);
        _currentOffsetX = UnityEngine.Random.Range(0f, 99999f);
        _currentOffsetY = UnityEngine.Random.Range(0f, 99999f);

        int halfDim = sizeDim / 2;
        Vector3Int startPos = new Vector3Int(-halfDim, -halfDim, 0);
        Vector3Int size = new Vector3Int(sizeDim, sizeDim, 1);

        // Batch tile assignment for performance
        BoundsInt bounds = new BoundsInt(startPos, size);
        TileBase[] tileArray = new TileBase[sizeDim * sizeDim];

        for (int i = 0; i < tileArray.Length; i++)
            tileArray[i] = baseSquareTile;

        squareTilemap.SetTilesBlock(bounds, tileArray);

        // Fetch seasonal palette for terrain coloring
        var palette = EnvironmentHandler.Instance != null
            ? EnvironmentHandler.Instance.GetCurrentPalette()
            : new SeasonalPalette(Color.tan, new Color(0.1f, 0.2f, 0.5f));

        // Generate terrain using Perlin noise
        for (int x = -halfDim; x < halfDim; x++)
        {
            for (int y = -halfDim; y < halfDim; y++)
            {
                Vector3Int tilePos = new Vector3Int(x, y, 0);

                float perlin = Mathf.PerlinNoise(
                    (x + halfDim) * perlinScale + _currentOffsetX,
                    (y + halfDim) * perlinScale + _currentOffsetY
                );

                // Use noise threshold to classify terrain type
                Color tileColor = (perlin > waterSpawnRate)
                    ? palette.waterColor
                    : palette.landColor;

                squareTilemap.SetTileFlags(tilePos, TileFlags.None);
                squareTilemap.SetColor(tilePos, tileColor);

                // Cache valid spawn location
                Vector3 worldCenter = squareTilemap.GetCellCenterWorld(tilePos);
                _openTiles.Add(worldCenter);
            }
        }

        // Sync world bounds for movement and spawning systems
        if (BoundaryManager.Instance != null)
            BoundaryManager.Instance.SetMapBounds(squareTilemap);

        if (camHandler != null)
            camHandler.UpdateTiers();

        // Generate obstacles after terrain is established
        if (obstaclePrefab != null)
            SpawnObstacles();

        IsMapReady = true;
        HasSimulationStarted = true;

        // Notify systems that simulation world is ready
        OnMapGenerated?.Invoke();

        // Delay entity spawning to ensure all systems are initialized
        Invoke(nameof(SpawnInitialEntities), 0.1f);
    }

    /// <summary>
    /// Spawns clustered obstacles across the terrain.
    /// </summary>
    private void SpawnObstacles()
    {
        List<Vector3> shuffled = new List<Vector3>(_openTiles);
        Shuffle(shuffled);

        HashSet<Vector3> occupied = new HashSet<Vector3>();

        foreach (Vector3 tileCenter in shuffled)
        {
            if (occupied.Contains(tileCenter)) continue;
            if (UnityEngine.Random.value > rockSpawnChance) continue;

            int clusterSize = UnityEngine.Random.Range(obstacleMinCluster, obstacleMaxCluster + 1);
            PlaceObstacleCluster(tileCenter, clusterSize, occupied);
        }

        // Remove occupied tiles to prevent spawning entities inside obstacles
        _openTiles.RemoveAll(t => occupied.Contains(t));
    }

    /// <summary>
    /// Places a cluster of obstacles around a starting tile.
    /// </summary>
    private void PlaceObstacleCluster(Vector3 origin, int count, HashSet<Vector3> occupied)
    {
        Vector3Int[] dirs = {
            Vector3Int.zero,
            Vector3Int.right, Vector3Int.left,
            Vector3Int.up, Vector3Int.down
        };

        Vector3Int originCell = squareTilemap.WorldToCell(origin);
        int placed = 0;

        foreach (Vector3Int dir in dirs)
        {
            if (placed >= count) break;

            Vector3Int cell = originCell + dir;
            Vector3 worldPos = squareTilemap.GetCellCenterWorld(cell);

            if (!squareTilemap.HasTile(cell)) continue;
            if (occupied.Contains(worldPos)) continue;

            Instantiate(obstaclePrefab, worldPos, Quaternion.identity);
            occupied.Add(worldPos);
            placed++;
        }
    }

    /// <summary>
    /// Spawns initial ecosystem populations.
    /// </summary>
    private void SpawnInitialEntities()
    {
        if (EcosystemManager.Instance == null)
        {
            Debug.LogWarning("MapGenerator2D: EcosystemManager missing — skipping spawn.");
            return;
        }

        for (int i = 0; i < startPlants; i++)
            EcosystemManager.Instance.ManualSpawnPlant(RandomOpenTile());

        for (int i = 0; i < startGrazers; i++)
            EcosystemManager.Instance.ManualSpawnGrazer(RandomOpenTile());

        for (int i = 0; i < startPredators; i++)
            EcosystemManager.Instance.ManualSpawnPredator(RandomOpenTile());
    }

    /// <summary>
    /// Returns a valid random spawn position.
    /// </summary>
    private Vector2 RandomOpenTile()
    {
        if (_openTiles.Count > 0)
            return _openTiles[UnityEngine.Random.Range(0, _openTiles.Count)];

        return BoundaryManager.Instance != null
            ? BoundaryManager.Instance.RandomPosition()
            : Vector2.zero;
    }

    /// <summary>
    /// Destroys all GameObjects with a given tag.
    /// </summary>
    private static void DestroyTagged(string tag)
    {
        foreach (GameObject go in GameObject.FindGameObjectsWithTag(tag))
            Destroy(go);
    }

    /// <summary>
    /// Randomizes list order for distribution fairness.
    /// </summary>
    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary>
    /// Recolors terrain tiles based on current season.
    /// </summary>
    public void RefreshTileColors()
    {
        if (squareTilemap == null) return;

        var palette = EnvironmentHandler.Instance != null
            ? EnvironmentHandler.Instance.GetCurrentPalette()
            : new SeasonalPalette(Color.tan, new Color(0.1f, 0.2f, 0.5f));

        int halfDim = squareTilemap.cellBounds.size.x / 2;

        foreach (var pos in squareTilemap.cellBounds.allPositionsWithin)
        {
            if (!squareTilemap.HasTile(pos)) continue;

            float perlin = Mathf.PerlinNoise(
                (pos.x + halfDim) * perlinScale + _currentOffsetX,
                (pos.y + halfDim) * perlinScale + _currentOffsetY
            );

            bool isWater = perlin > waterSpawnRate;

            Color newColor = isWater ? palette.waterColor : palette.landColor;

            squareTilemap.SetTileFlags(pos, TileFlags.None);
            squareTilemap.SetColor(pos, newColor);
        }
    }
}

