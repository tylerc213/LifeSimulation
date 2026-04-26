// -----------------------------------------------------------------------------
// Project:     EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:        [FROM GITHUB KANBAN BOARD]
// Requirement: [FROM GITHUB KANBAN BOARD]
// Author:      [Name]
// Date:        [MM/DD/YYYY]
// Version:     [#.#.#]
//
// Description:
//    Displays a world-space text label above a creature showing its current AI
//    state. Uses a TextMesh found automatically in children; billboards toward
//    the camera each LateUpdate.
// -----------------------------------------------------------------------------
using UnityEngine;

/// <summary>Displays and colors a world-space state label above a creature.</summary>
/// <remarks>
/// Add a child GameObject with a TextMesh component to each creature prefab.
/// This script finds the TextMesh automatically via GetComponentInChildren.
/// SetState should be called each frame from the creature's ExecuteState method.
/// </remarks>
public class StateLabel : MonoBehaviour
{
    [SerializeField] private float yOffset = 0.7f;

    // State string constants used by creature scripts and VisionCone
    public const string Wander = "Wander";
    public const string Flee   = "Flee!";
    public const string Seek   = "Seek Plant";
    public const string Eat    = "Eating";
    public const string Hunt   = "Hunt";
    public const string Stalk  = "Stalking";
    public const string Dash   = "Dash!";
    public const string Patrol = "Patrol";
    public const string Dead   = "Dead";

    private TextMesh  _label;
    private Transform _cam;
    private string    _currentState = "";

    /// <summary>Finds the TextMesh child and caches the main camera transform.</summary>
    private void Awake()
    {
        _label = GetComponentInChildren<TextMesh>();
        if (Camera.main != null) _cam = Camera.main.transform;
        if (_label != null) _label.text = "";
    }

    /// <summary>Keeps the label positioned above the creature and facing the camera.</summary>
    private void LateUpdate()
    {
        if (_label == null) return;

        // Keep label above sprite in local space
        _label.transform.localPosition = new Vector3(0f, yOffset, 0f);

        // Billboard so the label always faces the camera regardless of creature rotation
        if (_cam != null)
            _label.transform.rotation = Quaternion.LookRotation(
                _label.transform.position - _cam.position, Vector3.up);
    }

    /// <summary>Updates the label text and color when the state changes.</summary>
    /// <param name="state">State string constant from this class.</param>
    public void SetState(string state)
    {
        // Skip TMP update if state hasn't changed to avoid per-frame allocation
        if (_label == null || state == _currentState) return;
        _currentState = state;
        _label.text   = state;
        _label.color  = state switch
        {
            Wander => new Color(0.3f, 0.9f, 0.3f),
            Flee   => new Color(1f,   0.3f, 0.3f),
            Seek   => new Color(0.4f, 0.7f, 1f),
            Eat    => new Color(0.2f, 1f,   0.4f),
            Hunt   => new Color(1f,   0.55f, 0.1f),
            Stalk  => new Color(0.5f, 0.2f, 0.7f),
            Dash   => new Color(1f,   0f,   0.5f),
            Patrol => new Color(1f,   0.9f,  0.3f),
            Dead   => new Color(0.5f, 0.5f,  0.5f),
            _      => Color.white,
        };
    }

    /// <summary>Shows or hides the label GameObject.</summary>
    /// <param name="visible">True to show, false to hide.</param>
    public void SetVisible(bool visible)
    {
        if (_label != null) _label.gameObject.SetActive(visible);
    }
}
