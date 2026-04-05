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
    public Tilemap hexTilemap;

    [Header("Placeholders")]
    public GameObject grazerPrefab;
    public GameObject predatorPrefab;
    public GameObject plantPrefab;

    // Stores selected placement mode
    private int selection = 0;

    /// <summary> Updates current selection of lifeform to place from UI buttons </summary>
    /// <param name="type"> Integer ID of selection type </param>
    public void SetSelection(int type)
    {
        selection = type;
        string mode = "None";

        // maping integer ID to human-readable string for console feedback
        if (type == 1) mode = "Grazer";
        if (type == 2) mode = "Predator";
        if (type == 3) mode = "Plant";

        Debug.Log("Editor Mode: " + mode);
    }

    /// <summary> Listens for mouse input every frame </summary>
    void Update()
    {
        // prevent spawning while mousing over UI
        if (Mouse.current.leftButton.wasPressedThisFrame && selection != 0 && !EventSystem.current.IsPointerOverGameObject())
        {
            SpawnAtMouse();
        }
    }

    /// <summary> creates prefab of lifeform selection at mouse location </summary>
    void SpawnAtMouse()
    {
        // convert screen-space mouse position to 2D coordinates
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        Vector3Int cellPos = hexTilemap.WorldToCell(mousePos);

        if (hexTilemap.HasTile(cellPos))
        {
            // finds the center of the closest cell 
            Vector3 spawnPos = hexTilemap.GetCellCenterWorld(cellPos);
            spawnPos.z = 0;

            GameObject toSpawn = null;

            // select correct prefab
            if (selection == 1) toSpawn = grazerPrefab;
            else if (selection == 2) toSpawn = predatorPrefab;
            else if (selection == 3) toSpawn = plantPrefab;

            // create clone of selected prefab at target location
            if (toSpawn != null)
            {
                Instantiate(toSpawn, spawnPos, Quaternion.identity);
            }
        }
    }
}
