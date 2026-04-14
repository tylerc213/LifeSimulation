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
using System.Diagnostics;

/// <summary> Executes mouse-driven object placement logic </summary>
public class WorldEditor : MonoBehaviour
{
    [Header("Dependancies")]
    public Tilemap squareTilemap;
    public GameObject grazerPrefab;
    public GameObject predatorPrefab;
    public GameObject plantPrefab;
    public GameObject obstaclePrefab;

    // Stores selected placement mode
    private int selection = 0;

    /// <summary> Listens for mouse input every frame </summary>
    void Update()
    {
        // prevent spawning while mousing over UI
        if (Mouse.current.leftButton.wasPressedThisFrame && selection != 0 && !EventSystem.current.IsPointerOverGameObject())
        {
            UnityEngine.Debug.Log($"Click detected. Selection: {selection}, OverUI: {EventSystem.current.IsPointerOverGameObject()}");
            SpawnAtMouse();
        }
    }

    /// <summary> Updates current selection of lifeform to place from UI buttons </summary>
    /// <param name="type"> Integer ID of selection type </param>
    public void SetSelection(int type)
    {
        selection = type;
        UnityEngine.Debug.Log("Editor Mode: " + selection);
    }

    /// <summary> creates prefab of lifeform selection at mouse location </summary>
    void SpawnAtMouse()
    {
        // convert screen-space mouse position to 2D coordinates
        UnityEngine.Debug.Log("SpawnAtMouse called");
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        Vector3Int cellPos = squareTilemap.WorldToCell(mousePos);

        UnityEngine.Debug.Log($"HasTile: {squareTilemap.HasTile(cellPos)}, CellPos: {cellPos}, toSpawn null: {plantPrefab == null}");


            if (squareTilemap.HasTile(cellPos))
            {
                Vector3 spawnPos = squareTilemap.GetCellCenterWorld(cellPos);
                spawnPos.z = 0;
                string id = "";
                UnityEngine.Debug.Log($"EcosystemManager null: {EcosystemManager.Instance == null}");
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
                        Instantiate(obstaclePrefab, spawnPos, Quaternion.identity);
                        break;
                }

                if (id != "")
                    SimulationManager.Instance.UpdatePopulation(id, 1);
            }
    }
}
