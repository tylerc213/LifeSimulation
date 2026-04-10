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
            SpawnAtMouse();
        }
    }

    /// <summary> Updates current selection of lifeform to place from UI buttons </summary>
    /// <param name="type"> Integer ID of selection type </param>
    public void SetSelection(int type)
    {
        selection = type;
        Debug.Log("Editor Mode: " + selection);
    }

    /// <summary> creates prefab of lifeform selection at mouse location </summary>
    void SpawnAtMouse()
    {
        // convert screen-space mouse position to 2D coordinates
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        Vector3Int cellPos = squareTilemap.WorldToCell(mousePos);

        if (squareTilemap.HasTile(cellPos))
        {
            // finds the center of the closest cell 
            Vector3 spawnPos = squareTilemap.GetCellCenterWorld(cellPos);
            spawnPos.z = 0;

            GameObject toSpawn = null;
            string id = "";

            // select correct prefab
            switch (selection)
            {
                case 1:
                    toSpawn = grazerPrefab;
                    id = "Grazer";
                    break;
                case 2:
                    toSpawn = predatorPrefab;
                    id = "Predator";
                    break;
                case 3:
                    toSpawn = plantPrefab;
                    id = "Plant";
                    break;
                case 4:
                    toSpawn = obstaclePrefab;
                    id = "Obstacle";
                    break;
            }

            // create clone of selected prefab at target location
            if (toSpawn != null)
            {
                Instantiate(toSpawn, spawnPos, Quaternion.identity);
                SimulationManager.Instance.UpdatePopulation(id, 1);
            }
        }
    }
}
