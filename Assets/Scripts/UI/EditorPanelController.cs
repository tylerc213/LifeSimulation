// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Editor panel collapse / expand
// Requirement:	Simulation user interface
// Author:		Benjamin Jones
// Date:		04/17/2026
// Version:		0.0.0
//
// Description:
//    Collapse toggle lives on the canvas (not inside the panel). Hides the editor
//    panel, positions the toggle at the panel's outer top-left when expanded and
//    at the right screen edge when collapsed. Kept in its own file so Unity does
//    not confuse this MonoBehaviour with <see cref="WorldEditorShell"/> in the same MonoScript.
// -----------------------------------------------------------------------------

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lives on the collapse toggle (sibling of <see cref="WorldEditorShell.EditorPanelContentName"/> under the canvas).
/// </summary>
public class EditorPanelController : MonoBehaviour
{
    public const string CollapseToggleName = "EditorPanelCollapseToggle";
    const string PauseButtonName = "PauseSimulationButton";
    const string GenerateMapButtonName = "GenerateMapButton";
    const string CollapseGlyphChildName = "Glyph";

    [SerializeField] RectTransform _editorPanelRoot;

    TextMeshProUGUI _collapseGlyph;
    RectTransform _toggleRt;
    RectTransform _canvasRt;
    bool _collapsed;
    bool _simulationStarted;

    public static EditorPanelController Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        _toggleRt = transform as RectTransform;
        if (_editorPanelRoot == null)
            _editorPanelRoot = GameObject.Find("EditorPanel")?.GetComponent<RectTransform>();
        Canvas cv = GetComponentInParent<Canvas>();
        _canvasRt = cv != null ? cv.transform as RectTransform : null;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        StartCoroutine(CoLayoutAfterBuild());
    }

    IEnumerator CoLayoutAfterBuild()
    {
        yield return null;
        yield return null;
        RefreshTogglePlacement();
        if (_canvasRt != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(_canvasRt);
    }

    /// <summary> Ensures controller on the toggle and wiring; call after other editor panel UI is built. </summary>
    public static void EnsureOnPanel(Transform editorPanel, TMP_FontAsset font, LifeSimUITheme theme)
    {
        if (editorPanel == null)
            return;

        Canvas canvas = editorPanel.GetComponentInParent<Canvas>();
        RectTransform canvasRt = canvas != null ? canvas.transform as RectTransform : null;
        Transform toggleTransform = canvasRt != null
            ? WorldEditorShell.FindDeepChild(canvasRt, CollapseToggleName)
            : editorPanel.Find(CollapseToggleName);

        if (toggleTransform == null && canvasRt != null)
        {
            GameObject go = new GameObject(CollapseToggleName, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(canvasRt, false);
            toggleTransform = go.transform;
        }

        EditorPanelController c = toggleTransform != null
            ? toggleTransform.GetComponent<EditorPanelController>()
            : null;
        if (c == null && toggleTransform != null)
            c = toggleTransform.gameObject.AddComponent<EditorPanelController>();

        if (c == null)
            return;

        RectTransform panelRt = editorPanel as RectTransform;
        c.FinalizeSetup(panelRt, toggleTransform as RectTransform, canvasRt, font, theme);
    }

    void FinalizeSetup(RectTransform panelRt, RectTransform toggleRt, RectTransform canvasRt, TMP_FontAsset font,
        LifeSimUITheme theme)
    {
        if (panelRt != null)
            _editorPanelRoot = panelRt;
        if (canvasRt != null)
            _canvasRt = canvasRt;
        if (toggleRt != null)
            _toggleRt = toggleRt;

        if (_toggleRt == null || _editorPanelRoot == null)
            return;

        LifeSimUITheme t = theme != null ? theme : LifeSimUI.Theme;

        if (_canvasRt != null && _toggleRt.parent != _canvasRt)
            _toggleRt.SetParent(_canvasRt, false);

        if (_toggleRt.TryGetComponent(out LayoutElement le))
            Destroy(le);

        WireGlyphAndButton(_toggleRt, font, t);
        LifeSimUIButtonStyle.ApplyStripButton(_toggleRt.gameObject, t, false);
        RefreshTogglePlacement();
    }

    void WireGlyphAndButton(Transform root, TMP_FontAsset font, LifeSimUITheme theme)
    {
        Transform glyphTransform = root.Find(CollapseGlyphChildName);
        if (glyphTransform == null)
        {
            GameObject textGo = new GameObject(CollapseGlyphChildName, typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(root, false);
            RectTransform tr = textGo.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;
            glyphTransform = textGo.transform;
        }

        _collapseGlyph = glyphTransform.GetComponent<TextMeshProUGUI>();
        if (_collapseGlyph == null)
            _collapseGlyph = glyphTransform.gameObject.AddComponent<TextMeshProUGUI>();

        _collapseGlyph.text = ">";
        _collapseGlyph.alignment = TextAlignmentOptions.Center;
        if (font != null)
            _collapseGlyph.font = font;
        _collapseGlyph.enableAutoSizing = false;
        _collapseGlyph.fontSize = Mathf.Max(16f, theme.toolbarButtonFontSize);

        if (root.TryGetComponent(out Button button))
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(ToggleCollapsed);
        }
    }

    /// <summary> Called when Generate Map has started the simulation; reveals pause when the panel is expanded. </summary>
    public void NotifySimulationStarted()
    {
        _simulationStarted = true;
        if (_editorPanelRoot != null && _editorPanelRoot.gameObject.activeInHierarchy)
            ApplyGenerateMapVisibility();
        ApplyPauseVisibility();
    }

    void ToggleCollapsed()
    {
        _collapsed = !_collapsed;
        ApplyCollapsedVisuals();
    }

    void ApplyCollapsedVisuals()
    {
        if (_collapseGlyph != null)
            _collapseGlyph.text = _collapsed ? "<" : ">";

        if (_editorPanelRoot != null)
            _editorPanelRoot.gameObject.SetActive(!_collapsed);

        RefreshTogglePlacement();

        if (!_collapsed)
            ApplyGenerateMapVisibility();
        ApplyPauseVisibility();
    }

    void RefreshTogglePlacement()
    {
        if (_toggleRt == null || _canvasRt == null || _editorPanelRoot == null)
            return;

        _toggleRt.SetParent(_canvasRt, false);
        _toggleRt.SetAsLastSibling();

        const float buttonSize = 40f;

        if (_collapsed)
        {
            _toggleRt.anchorMin = new Vector2(1f, 0.5f);
            _toggleRt.anchorMax = new Vector2(1f, 0.5f);
            _toggleRt.pivot = new Vector2(1f, 0.5f);
            _toggleRt.sizeDelta = new Vector2(buttonSize, buttonSize);
            _toggleRt.anchoredPosition = new Vector2(-16f, 0f);
        }
        else
        {
            Vector3[] corners = new Vector3[4];
            _editorPanelRoot.GetWorldCorners(corners);
            Vector2 panelTopLeftCanvas = _canvasRt.InverseTransformPoint(corners[1]);

            _toggleRt.anchorMin = _toggleRt.anchorMax = new Vector2(0.5f, 0.5f);
            _toggleRt.pivot = new Vector2(1f, 1f);
            _toggleRt.sizeDelta = new Vector2(buttonSize, buttonSize);
            _toggleRt.anchoredPosition = panelTopLeftCanvas + new Vector2(-8f, -6f);
        }
    }

    Transform FindUnderPanel(string objectName)
    {
        return _editorPanelRoot != null
            ? WorldEditorShell.FindDeepChild(_editorPanelRoot, objectName)
            : null;
    }

    void ApplyGenerateMapVisibility()
    {
        Transform gen = FindUnderPanel(GenerateMapButtonName);
        if (gen == null)
            return;

        gen.gameObject.SetActive(!_simulationStarted);
    }

    void ApplyPauseVisibility()
    {
        Transform pause = FindUnderPanel(PauseButtonName);
        if (pause == null)
            return;

        pause.gameObject.SetActive(_simulationStarted && !_collapsed && _editorPanelRoot != null &&
                                     _editorPanelRoot.gameObject.activeInHierarchy);
    }
}
