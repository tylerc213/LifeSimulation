// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Simulation GUI
// Requirement:	Sim Editor
// Author:		Robert Amborski
// Date:		3/25/2026
//
// Description:
//    Allows user to place entities onto the simulation map using mouse input.
//    Validates simulation state and tile positions before spawning.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
#if ENABLE_LEGACY_INPUT_MANAGER
using LegacyInput = UnityEngine.Input;
#endif

/// <summary>
/// Handles mouse-based entity placement on the map.
/// </summary>
/// <remarks>
/// Only allows placement after the simulation has started and on valid tiles.
/// </remarks>
public class WorldEditor : MonoBehaviour
{
    [Header("Dependancies")]
    public Tilemap squareTilemap;

    [Tooltip("If unset, uses MapGenerator2D on the same GameObject. Spawns are blocked until Generate Map has finished.")]
    [SerializeField] private MapGenerator2D mapGenerator;

    public GameObject grazerPrefab;
    public GameObject predatorPrefab;
    public GameObject plantPrefab;
    public GameObject obstaclePrefab;

    // Stores current selection mode (0 = none)
    private int selection = 0;

    /// <summary>
    /// Initializes references and ensures simulation bootstrap exists.
    /// </summary>
    void Awake()
    {
        // Auto-assign generator if not set
        if (mapGenerator == null)
        {
            mapGenerator = GetComponent<MapGenerator2D>();
        }

        // Ensure simulation bootstrap exists for proper scene initialization
        if (FindFirstObjectByType<SimulationSceneBootstrap>() == null)
        {
            GameObject boot = new GameObject("SimulationSceneBootstrap");
            boot.AddComponent<SimulationSceneBootstrap>();
        }
    }

    /// <summary>
    /// Validates whether spawning is currently allowed.
    /// </summary>
    /// <returns>True if spawning conditions are met.</returns>
    private bool CanSpawnOnMap()
    {
        return mapGenerator != null
               && mapGenerator.IsMapReady
               && mapGenerator.HasSimulationStarted
               && squareTilemap != null
               && EcosystemManager.Instance != null;
    }

    /// <summary>
    /// Processes input and triggers spawn on valid click.
    /// </summary>
    void Update()
    {
        // Do nothing if no selection is active
        if (selection == 0)
        {
            return;
        }

        // Only respond to initial click press
        if (!WasPrimaryClickPressedThisFrame())
        {
            return;
        }

        // Prevent spawning when clicking UI elements
        if (IsPointerOverUiBlockingGame())
        {
            return;
        }

        // Ensure simulation is ready for spawning
        if (!CanSpawnOnMap())
        {
            return;
        }

        SpawnAtMouse();
    }

    /// <summary>
    /// Detects primary mouse click using new or legacy input systems.
    /// </summary>
    /// <returns>True if click occurred this frame.</returns>
    private static bool WasPrimaryClickPressedThisFrame()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }
#if ENABLE_LEGACY_INPUT_MANAGER
        return LegacyInput.GetMouseButtonDown(0);
#else
        return false;
#endif
    }

    /// <summary>
    /// Checks if pointer is over UI to block world interaction.
    /// </summary>
    /// <returns>True if UI is blocking input.</returns>
    private static bool IsPointerOverUiBlockingGame()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        // Use device-specific check for new input system
        if (Mouse.current != null)
        {
            return EventSystem.current.IsPointerOverGameObject(Mouse.current.deviceId);
        }

        return EventSystem.current.IsPointerOverGameObject();
    }

    /// <summary>
    /// Sets current placement selection.
    /// </summary>
    /// <param name="type">Entity selection type.</param>
    public void SetSelection(int type)
    {
        // Prevent selection before simulation is ready
        if (!CanSpawnOnMap())
        {
            return;
        }

        selection = type;
    }

    /// <summary>
    /// UI wrapper for selection.
    /// </summary>
    /// <param name="type">Entity selection type.</param>
    public void SelectAndSpawnOne(int type)
    {
        SetSelection(type);
    }

    /// <summary>
    /// Attempts to spawn selected entity at mouse position.
    /// </summary>
    void SpawnAtMouse()
    {
        // Ensure valid camera and simulation state
        if (!CanSpawnOnMap() || Camera.main == null)
        {
            return;
        }

        // Convert screen position to world position on tilemap plane
        if (!TryGetWorldPointOnTilemapPlane(GetPointerScreenPosition(), out Vector3 worldOnPlane))
        {
            return;
        }

        TrySpawnAtWorldPosition(worldOnPlane);
    }

    /// <summary>
    /// Attempts to spawn entity at a given world position.
    /// </summary>
    /// <param name="worldOnPlane">World position on tilemap plane.</param>
    /// <returns>True if spawn succeeded.</returns>
    private bool TrySpawnAtWorldPosition(Vector3 worldOnPlane)
    {
        if (!CanSpawnOnMap())
        {
            return false;
        }

        // Convert world position to tilemap cell
        Vector3Int cellPos = squareTilemap.WorldToCell(worldOnPlane);

        // Prevent spawning outside valid tiles
        if (!squareTilemap.HasTile(cellPos))
        {
            return false;
        }

        // Snap to tile center for consistent placement
        Vector3 spawnPos = squareTilemap.GetCellCenterWorld(cellPos);
        spawnPos.z = squareTilemap.transform.position.z;

        // Spawn based on current selection
        switch (selection)
        {
            case 1:
                EcosystemManager.Instance.ManualSpawnGrazer(spawnPos);
                break;
            case 2:
                EcosystemManager.Instance.ManualSpawnPredator(spawnPos);
                break;
            case 3:
                EcosystemManager.Instance.ManualSpawnPlant(spawnPos);
                break;
            case 4:
                if (obstaclePrefab != null)
                {
                    Instantiate(obstaclePrefab, spawnPos, Quaternion.identity);
                }
                break;
        }

        return true;
    }

    /// <summary>
    /// Retrieves pointer screen position for input systems.
    /// </summary>
    /// <returns>Screen position of pointer.</returns>
    private static Vector2 GetPointerScreenPosition()
    {
        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }
#if ENABLE_LEGACY_INPUT_MANAGER
        return LegacyInput.mousePosition;
#else
        return Vector2.zero;
#endif
    }

    /// <summary>
    /// Converts screen position to world position on tilemap plane.
    /// </summary>
    /// <param name="screenPx">Screen position.</param>
    /// <param name="worldPoint">Resulting world point.</param>
    /// <returns>True if conversion succeeded.</returns>
    private bool TryGetWorldPointOnTilemapPlane(Vector2 screenPx, out Vector3 worldPoint)
    {
        worldPoint = default;
        Camera cam = Camera.main;

        if (cam == null || squareTilemap == null)
        {
            return false;
        }

        float planeZ = squareTilemap.transform.position.z;

        // Cast ray from camera through screen point
        Ray ray = cam.ScreenPointToRay(new Vector3(screenPx.x, screenPx.y, 0f));

        // Intersect ray with tilemap plane
        if (Mathf.Abs(ray.direction.z) > 1e-5f)
        {
            float t = (planeZ - ray.origin.z) / ray.direction.z;

            if (t >= 0f)
            {
                worldPoint = ray.GetPoint(t);
                return true;
            }
        }

        // Fallback for typical 2D orthographic setup
        float fallbackZ = Mathf.Abs(cam.transform.position.z - planeZ);
        worldPoint = cam.ScreenToWorldPoint(new Vector3(screenPx.x, screenPx.y, fallbackZ));
        return true;
    }
}