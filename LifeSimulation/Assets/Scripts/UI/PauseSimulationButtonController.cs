// -----------------------------------------------------------------------------
// Pause / Play toggle for the simulation strip. Updates label text.
// -----------------------------------------------------------------------------

using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class PauseSimulationButtonController : MonoBehaviour
{
    public TextMeshProUGUI label;

    Button _button;

    void Awake()
    {
        _button = GetComponent<Button>();
        if (label == null)
            label = GetComponentInChildren<TextMeshProUGUI>();
        _button.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        if (SimulationManager.Instance != null)
            SimulationManager.Instance.TogglePause();
        UpdateLabel();
    }

    void OnEnable()
    {
        UpdateLabel();
    }

    public void UpdateLabel()
    {
        if (label == null)
            return;
        bool paused = SimulationManager.Instance != null && SimulationManager.Instance.IsUserPaused;
        label.text = paused ? "Play" : "Pause";
    }
}
