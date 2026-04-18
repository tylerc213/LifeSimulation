using UnityEngine;

public class StateLabel : MonoBehaviour
{
    [SerializeField] private float yOffset = 0.7f;

    public const string Wander = "Wander";
    public const string Flee = "Flee!";
    public const string Seek = "Seek Plant";
    public const string Eat = "Eating";
    public const string Hunt = "Hunt";
    public const string Patrol = "Patrol";
    public const string Dead = "Dead";

    private TextMesh _label;
    private Transform _cam;
    private string _currentState = "";

    private void Awake()
    {
        // Find the TextMesh anywhere in this GameObject's children automatically
        _label = GetComponentInChildren<TextMesh>();
        if (_cam == null && Camera.main != null) _cam = Camera.main.transform;
        if (_label != null) _label.text = "";
    }

    private void LateUpdate()
    {
        if (_label == null) return;
        _label.transform.localPosition = new Vector3(0f, yOffset, 0f);
        if (_cam != null)
            _label.transform.rotation = Quaternion.LookRotation(
                _label.transform.position - _cam.position, Vector3.up);
    }

    public void SetState(string state)
    {
        if (_label == null || state == _currentState) return;
        _currentState = state;
        _label.text = state;
        _label.color = state switch
        {
            Wander => new Color(0.3f, 0.9f, 0.3f),
            Flee => new Color(1f, 0.3f, 0.3f),
            Seek => new Color(0.4f, 0.7f, 1f),
            Eat => new Color(0.2f, 1f, 0.4f),
            Hunt => new Color(1f, 0.55f, 0.1f),
            Patrol => new Color(1f, 0.9f, 0.3f),
            Dead => new Color(0.5f, 0.5f, 0.5f),
            _ => Color.white,
        };
    }

    public void SetVisible(bool visible)
    {
        if (_label != null) _label.gameObject.SetActive(visible);
    }
}