// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Pause / resume control
// Requirement:	Simulation user interface
// Author:		Benjamin Jones
// Date:		04/14/2026
// Version:		0.0.0
//
// Description:
//    Primary affordance for freezing and resuming the sim clock; label tracks
//    state so the strip always matches what the world is doing.
// -----------------------------------------------------------------------------

using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary> Pause / resume strip control wired to <see cref="SimulationManager"/>. </summary>
[RequireComponent(typeof(Button))]
public class PauseSimulationButtonController : MonoBehaviour
{
    public TextMeshProUGUI label;

    Button _button;

    public void UpdateLabel()
    {
        if (label == null)
            return;

        bool paused = SimulationManager.Instance != null && SimulationManager.Instance.IsUserPaused;
        label.text = paused ? "Play" : "Pause";
    }

    void Awake()
    {
        _button = GetComponent<Button>();
        if (label == null)
            label = GetComponentInChildren<TextMeshProUGUI>();
        _button.onClick.AddListener(OnClickPauseToggle);
    }

    void OnEnable()
    {
        UpdateLabel();
    }

    void OnClickPauseToggle()
    {
        if (SimulationManager.Instance == null)
            return;

        SimulationManager.Instance.TogglePause();
        UpdateLabel();
    }
}
