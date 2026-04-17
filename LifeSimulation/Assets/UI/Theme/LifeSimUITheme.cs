// -----------------------------------------------------------------------------
// Central UI tokens for Life Simulation (uGUI + TMP). Optional asset:
// place an instance at Resources/LifeSimUITheme to override defaults.
// -----------------------------------------------------------------------------

using UnityEngine;

[CreateAssetMenu(fileName = "LifeSimUITheme", menuName = "Life Simulation/UI Theme")]
public class LifeSimUITheme : ScriptableObject
{
    [Header("Toolbar — Simulation EditorPanel")]
    [Tooltip("Spawn / map / pause / settings opener buttons on the strip.")]
    public Color toolbarButtonBackground = new Color(0.102f, 0.710f, 0.694f, 1f);

    [Tooltip("Stronger accent for the primary strip action (e.g. Generate Map).")]
    public Color toolbarPrimaryBackground = new Color(0.078f, 0.62f, 0.58f, 1f);

    public Color toolbarButtonLabel = new Color(0.196f, 0.196f, 0.196f, 1f);
    public float toolbarButtonFontSize = 14f;

    [Header("Toolbar — danger / reset")]
    public Color toolbarDangerBackground = new Color(0.62f, 0.22f, 0.22f, 1f);
    public Color toolbarDangerLabel = new Color(0.98f, 0.98f, 0.98f, 1f);

    [Header("Settings popout (modal)")]
    [Tooltip("Full-screen blocker behind settings popout. Alpha controls dim strength.")]
    public Color modalOverlayDim = new Color(0f, 0f, 0f, 0.22f);
    public Color modalShellBackground = new Color(0.329f, 0.763f, 0.840f, 0.92f);
    public Color modalScrollWell = new Color(1f, 1f, 1f, 0.06f);
    public Color modalViewportTint = new Color(1f, 1f, 1f, 0.02f);

    [Header("Typography — popout")]
    public float modalTitleFontSize = 20f;
    public float formRowLabelFontSize = 13f;

    [Header("Spacing (dp-style units)")]
    public float spacingXs = 6f;
    public float spacingS = 8f;
    public float spacingM = 10f;
    public float spacingL = 12f;

    [Header("Form controls")]
    public Color sliderTrackBackground = new Color(0.15f, 0.15f, 0.15f, 0.6f);
    public Color sliderFill = new Color(0.18f, 0.65f, 0.62f, 1f);
    public Color sliderHandle = Color.white;

    public Color toggleBoxBackground = new Color(1f, 1f, 1f, 0.95f);
    public Color toggleCheckmark = new Color(0.15f, 0.65f, 0.55f, 1f);

    [Header("Body text (labels, values)")]
    public Color bodyText = new Color(0.196f, 0.196f, 0.196f, 1f);

#if UNITY_EDITOR
    void Reset()
    {
        ApplyEmbeddedDefaults();
    }
#endif

    /// <summary>Populate fields when no asset exists (runtime fallback instance).</summary>
    public void ApplyEmbeddedDefaults()
    {
        toolbarButtonBackground = new Color(0.102f, 0.710f, 0.694f, 1f);
        toolbarPrimaryBackground = new Color(0.078f, 0.62f, 0.58f, 1f);
        toolbarButtonLabel = new Color(0.196f, 0.196f, 0.196f, 1f);
        toolbarButtonFontSize = 14f;
        toolbarDangerBackground = new Color(0.62f, 0.22f, 0.22f, 1f);
        toolbarDangerLabel = new Color(0.98f, 0.98f, 0.98f, 1f);
        modalOverlayDim = new Color(0f, 0f, 0f, 0.22f);
        modalShellBackground = new Color(0.329f, 0.763f, 0.840f, 0.92f);
        modalScrollWell = new Color(1f, 1f, 1f, 0.06f);
        modalViewportTint = new Color(1f, 1f, 1f, 0.02f);
        modalTitleFontSize = 20f;
        formRowLabelFontSize = 13f;
        spacingXs = 6f;
        spacingS = 8f;
        spacingM = 10f;
        spacingL = 12f;
        sliderTrackBackground = new Color(0.15f, 0.15f, 0.15f, 0.6f);
        sliderFill = new Color(0.18f, 0.65f, 0.62f, 1f);
        sliderHandle = Color.white;
        toggleBoxBackground = new Color(1f, 1f, 1f, 0.95f);
        toggleCheckmark = new Color(0.15f, 0.65f, 0.55f, 1f);
        bodyText = new Color(0.196f, 0.196f, 0.196f, 1f);
    }
}
