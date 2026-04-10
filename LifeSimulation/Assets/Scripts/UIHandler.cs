// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Simulation GUI
// Requirement:	Sim Editor
// Author:		Robert Amborski
// Date:		3/25/2026
// Version:		0.0.0
//
// Description:
//    Captures user inputs from TextMeshPro and triggers map generation
// -----------------------------------------------------------------------------

using UnityEngine;
using TMPro;

/// <summary> Handles UI element and simulation interaction </summary>
public class UIHandler : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField seedInput;
    public TMP_InputField widthInput;
    public TMP_InputField heightInput;

    [Header("Generator Reference")]
    public MapGenerator2D mapGenerator;

    /// <summary> Triggers upon Map Generate button being clicked </summary>
    public void OnClickGenerate()
    {
        // Takes string data from seed field input to be used in map generation
        string seed = seedInput.text;

        // Takes width and height from width and height field inputs to be used in map generation
        int width = int.TryParse(widthInput.text, out int w) ? w : 250;
        int height = int.TryParse(heightInput.text, out int h) ? h : 250;

        // Pass taken data to map generation script
        mapGenerator.GenerateMap(seed, width, height);
    }
}
