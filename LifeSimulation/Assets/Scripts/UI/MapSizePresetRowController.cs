// -----------------------------------------------------------------------------
// Project:     EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:        Simulation UI — map size presets
// Requirement: Map size before Generate Map (Leaderboard category row styling)
// -----------------------------------------------------------------------------

using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Three mutually exclusive buttons (Small / Default / Large) that set
/// <see cref="CameraHandler.selectedSize"/> and mirror dimensions into
/// <see cref="SimulationSettings"/> when the store exists.
/// </summary>
public class MapSizePresetRowController : MonoBehaviour
{
    static readonly string[] Labels = { "Small", "Default", "Large" };

    [SerializeField] CameraHandler cameraHandler;

    Button[] _buttons;
    int _selectedIndex = 1;

    void Awake()
    {
        if (cameraHandler == null)
            cameraHandler = FindFirstObjectByType<CameraHandler>();

        CollectButtons();
        _selectedIndex = MapSizeToIndex(cameraHandler != null ? cameraHandler.selectedSize : CameraHandler.MapSize.Medium);
        WireButtons();
        UpdateVisuals();
    }

    void OnEnable()
    {
        MapGenerator2D.OnMapGenerated += OnMapGenerated;
    }

    void OnDisable()
    {
        MapGenerator2D.OnMapGenerated -= OnMapGenerated;
    }

    void OnMapGenerated()
    {
        if (cameraHandler == null)
            cameraHandler = FindFirstObjectByType<CameraHandler>();
    }

    void CollectButtons()
    {
        int n = transform.childCount;
        _buttons = new Button[n];
        for (int i = 0; i < n; i++)
            _buttons[i] = transform.GetChild(i).GetComponent<Button>();
    }

    void WireButtons()
    {
        if (_buttons == null)
            return;

        for (int i = 0; i < _buttons.Length; i++)
        {
            if (_buttons[i] == null)
                continue;
            int idx = i;
            _buttons[i].onClick.RemoveAllListeners();
            _buttons[i].onClick.AddListener(() => OnPresetClicked(idx));
        }
    }

    void OnPresetClicked(int index)
    {
        index = Mathf.Clamp(index, 0, 2);
        if (_selectedIndex == index)
            return;

        _selectedIndex = index;
        ApplySizeToHandlers(IndexToMapSize(index));
        UpdateVisuals();
    }

    static int MapSizeToIndex(CameraHandler.MapSize size)
    {
        return size switch
        {
            CameraHandler.MapSize.Small => 0,
            CameraHandler.MapSize.Medium => 1,
            CameraHandler.MapSize.Large => 2,
            _ => 1
        };
    }

    static CameraHandler.MapSize IndexToMapSize(int index)
    {
        return index switch
        {
            0 => CameraHandler.MapSize.Small,
            1 => CameraHandler.MapSize.Medium,
            2 => CameraHandler.MapSize.Large,
            _ => CameraHandler.MapSize.Medium
        };
    }

    void ApplySizeToHandlers(CameraHandler.MapSize size)
    {
        if (cameraHandler == null)
            cameraHandler = FindFirstObjectByType<CameraHandler>();
        if (cameraHandler != null)
        {
            cameraHandler.selectedSize = size;
            cameraHandler.ApplyPresetHalfMap();
        }
    }

    void UpdateVisuals()
    {
        if (_buttons == null)
            return;

        LifeSimUITheme theme = LifeSimUI.Theme;
        if (theme == null)
            return;

        Color normalBg = theme.toolbarButtonBackground;
        Color selectedBg = theme.toolbarPrimaryBackground;
        Color labelColor = theme.toolbarButtonLabel;

        for (int i = 0; i < _buttons.Length; i++)
        {
            if (_buttons[i] == null)
                continue;

            bool sel = i == _selectedIndex;
            Image img = _buttons[i].GetComponent<Image>();
            if (img != null)
                img.color = sel ? selectedBg : normalBg;

            TextMeshProUGUI tmp = _buttons[i].GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null)
            {
                tmp.color = labelColor;
                tmp.fontStyle = sel ? FontStyles.Bold : FontStyles.Normal;
            }
        }
    }

    /// <summary> Builds three strip-style preset toggles under <paramref name="rowRoot"/>. </summary>
    public static void BuildRowContent(Transform rowRoot, TMP_FontAsset font, LifeSimUITheme theme)
    {
        for (int c = rowRoot.childCount - 1; c >= 0; c--)
            Object.Destroy(rowRoot.GetChild(c).gameObject);

        if (theme == null)
            theme = LifeSimUI.Theme;

        for (int i = 0; i < 3; i++)
        {
            GameObject go = new GameObject($"MapSizeButton{i}", typeof(RectTransform), typeof(Image), typeof(Button),
                typeof(LayoutElement));
            go.transform.SetParent(rowRoot, false);

            LayoutElement le = go.GetComponent<LayoutElement>();
            le.minHeight = 28f;
            le.preferredHeight = 28f;
            le.flexibleWidth = 1f;

            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            RectTransform lr = labelGo.GetComponent<RectTransform>();
            lr.anchorMin = Vector2.zero;
            lr.anchorMax = Vector2.one;
            lr.offsetMin = new Vector2(4f, 2f);
            lr.offsetMax = new Vector2(-4f, -2f);

            LifeSimUIButtonStyle.ApplyStripButton(go, theme, false);
            if (font != null)
            {
                TextMeshProUGUI tmp = go.GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmp != null)
                    tmp.font = font;
            }

            TextMeshProUGUI label = go.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = Labels[i];
                label.enableAutoSizing = false;
                label.fontSize = theme.toolbarButtonFontSize;
                label.alignment = TextAlignmentOptions.Center;
            }
        }
    }
}
