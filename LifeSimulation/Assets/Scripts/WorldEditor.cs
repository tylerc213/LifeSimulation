// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Simulation GUI
// Requirement:	Sim Editor
// Author:		Robert Amborski
// Date:		3/25/2026
// Version:		0.0.0
//
// Description:
//    Allows user to place lifeform placeholders onto map
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
#if ENABLE_LEGACY_INPUT_MANAGER
using LegacyInput = UnityEngine.Input;
#endif

/// <summary> Executes mouse-driven object placement logic </summary>
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

    // Stores selected placement mode (set by UI buttons; spawn only on map click)
    private int selection = 0;

    void Awake()
    {
        if (mapGenerator == null)
        {
            mapGenerator = GetComponent<MapGenerator2D>();
        }

        if (FindFirstObjectByType<SimulationSceneBootstrap>() == null)
        {
            GameObject boot = new GameObject("SimulationSceneBootstrap");
            boot.AddComponent<SimulationSceneBootstrap>();
        }
    }

    private bool CanSpawnOnMap()
    {
        return mapGenerator != null
               && mapGenerator.IsMapReady
               && mapGenerator.HasSimulationStarted
               && squareTilemap != null
               && EcosystemManager.Instance != null;
    }

    /// <summary> Listens for mouse input every frame </summary>
    void Update()
    {
        if (selection == 0)
        {
            return;
        }

        if (!WasPrimaryClickPressedThisFrame())
        {
            return;
        }

        // Input System UI module: must pass device id; parameterless IsPointerOverGameObject often blocks all game-view clicks.
        if (IsPointerOverUiBlockingGame())
        {
            return;
        }

        if (!CanSpawnOnMap())
        {
            return;
        }

        UnityEngine.Debug.Log($"Click detected. Selection: {selection}");
        SpawnAtMouse();
    }

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

    private static bool IsPointerOverUiBlockingGame()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        if (Mouse.current != null)
        {
            return EventSystem.current.IsPointerOverGameObject(Mouse.current.deviceId);
        }

        return EventSystem.current.IsPointerOverGameObject();
    }

    /// <summary> Selects which entity type to place; actual spawn happens on the next valid map click.</summary>
    /// <param name="type"> Integer ID of selection type </param>
    public void SetSelection(int type)
    {
        if (!CanSpawnOnMap())
        {
            return;
        }

        selection = type;
        UnityEngine.Debug.Log("Editor Mode: " + selection);
    }

    /// <summary> UI alias for <see cref="SetSelection"/>.</summary>
    public void SelectAndSpawnOne(int type)
    {
        SetSelection(type);
    }

    /// <summary> creates prefab of lifeform selection at mouse location </summary>
    void SpawnAtMouse()
    {
        UnityEngine.Debug.Log("SpawnAtMouse called");
        if (!CanSpawnOnMap() || Camera.main == null)
        {
            return;
        }

        if (!TryGetWorldPointOnTilemapPlane(GetPointerScreenPosition(), out Vector3 worldOnPlane))
        {
            return;
        }

        TrySpawnAtWorldPosition(worldOnPlane);
    }

    private bool TrySpawnAtWorldPosition(Vector3 worldOnPlane)
    {
        if (!CanSpawnOnMap())
        {
            return false;
        }

        Vector3Int cellPos = squareTilemap.WorldToCell(worldOnPlane);

        UnityEngine.Debug.Log($"HasTile: {squareTilemap.HasTile(cellPos)}, CellPos: {cellPos}");

        if (!squareTilemap.HasTile(cellPos))
        {
            return false;
        }

        Vector3 spawnPos = squareTilemap.GetCellCenterWorld(cellPos);
        spawnPos.z = squareTilemap.transform.position.z;
        string id = "";
        switch (selection)
        {
            case 1:
                id = "Grazer";
                EcosystemManager.Instance.ManualSpawnGrazer(spawnPos);
                break;
            case 2:
                id = "Predator";
                EcosystemManager.Instance.ManualSpawnPredator(spawnPos);
                break;
            case 3:
                id = "Plant";
                EcosystemManager.Instance.ManualSpawnPlant(spawnPos);
                break;
            case 4:
                id = "Obstacle";
                if (obstaclePrefab != null)
                {
                    Instantiate(obstaclePrefab, spawnPos, Quaternion.identity);
                }
                break;
        }

        return true;
    }

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
    /// Orthographic + Input.mouse z=0 breaks ScreenToWorldPoint; intersect the camera ray with the tilemap Z plane.
    /// </summary>
    private bool TryGetWorldPointOnTilemapPlane(Vector2 screenPx, out Vector3 worldPoint)
    {
        worldPoint = default;
        Camera cam = Camera.main;
        if (cam == null || squareTilemap == null)
        {
            return false;
        }

        float planeZ = squareTilemap.transform.position.z;
        Ray ray = cam.ScreenPointToRay(new Vector3(screenPx.x, screenPx.y, 0f));

        if (Mathf.Abs(ray.direction.z) > 1e-5f)
        {
            float t = (planeZ - ray.origin.z) / ray.direction.z;
            if (t >= 0f)
            {
                worldPoint = ray.GetPoint(t);
                return true;
            }
        }

        // Fallback: distance from camera to plane along forward (typical 2D setup).
        float fallbackZ = Mathf.Abs(cam.transform.position.z - planeZ);
        worldPoint = cam.ScreenToWorldPoint(new Vector3(screenPx.x, screenPx.y, fallbackZ));
        return true;
    }
}
