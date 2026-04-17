// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		World editor UI build
// Requirement:	Simulation user interface
// Author:		Benjamin Jones
// Date:		04/14/2026
// Version:		0.0.0
//
// Description:
//    Materializes the in-scene editor shell (pause, settings entry, popouts) at
//    runtime so the Simulation scene stays sparse in the editor yet still ships a full control surface.
// -----------------------------------------------------------------------------

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary> Builds world editor UI under the Simulation scene Canvas at runtime. </summary>
public static class WorldEditorUIBuilder
{
    /// <summary> Shared body label color; prefer <see cref="LifeSimUI.Theme"/>. </summary>
    public static Color TextDimPublic => LifeSimUI.Theme.bodyText;

    public static void EnsureBuilt(Canvas canvas)
    {
        if (canvas == null)
            return;

        LifeSimUITheme theme = LifeSimUI.Theme;
        TMP_FontAsset font = GetSceneFont();

        Transform editorPanel = canvas.transform.Find("EditorPanel");
        if (editorPanel == null)
            return;

        if (editorPanel.Find("PauseSimulationButton") == null)
            BuildPauseButton(editorPanel, font, theme);

        if (editorPanel.Find("SettingsButtonsGrid") == null)
            BuildSettingsButtons(editorPanel, font, theme);

        if (canvas.transform.Find("SettingsPopupsRoot") == null)
            BuildSettingsPopupsRoot(canvas.transform, font, theme);
    }

    static TMP_FontAsset GetSceneFont()
    {
        TextMeshProUGUI any = UnityEngine.Object.FindFirstObjectByType<TextMeshProUGUI>();
        return any != null ? any.font : null;
    }

    static int GetInsertIndexAfterGenerateMap(Transform editorPanel)
    {
        Transform gen = editorPanel.Find("GenerateMapButton");
        return gen != null ? gen.GetSiblingIndex() + 1 : editorPanel.childCount;
    }

    static void BuildPauseButton(Transform editorPanel, TMP_FontAsset font, LifeSimUITheme theme)
    {
        GameObject go = CreateButton("PauseSimulationButton", "Pause", editorPanel, font, theme);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 36f);
        go.AddComponent<PauseSimulationButtonController>();
        go.transform.SetSiblingIndex(GetInsertIndexAfterGenerateMap(editorPanel));
    }

    static void BuildSettingsButtons(Transform editorPanel, TMP_FontAsset font, LifeSimUITheme theme)
    {
        GameObject gridGo = new GameObject("SettingsButtonsGrid", typeof(RectTransform), typeof(GridLayoutGroup));
        gridGo.transform.SetParent(editorPanel, false);
        Transform pauseBtn = editorPanel.Find("PauseSimulationButton");
        int gridIdx = pauseBtn != null
            ? pauseBtn.GetSiblingIndex() + 1
            : GetInsertIndexAfterGenerateMap(editorPanel);
        gridGo.transform.SetSiblingIndex(gridIdx);
        RectTransform gridRt = gridGo.GetComponent<RectTransform>();
        gridRt.sizeDelta = new Vector2(210f, 90f);
        GridLayoutGroup grid = gridGo.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(100f, 40f);
        grid.spacing = new Vector2(theme.spacingS, theme.spacingS);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        grid.childAlignment = TextAnchor.MiddleCenter;

        CreateSettingsOpenerButton("GameSettingsButton", "Game Settings", gridGo.transform, font, theme,
            ui => ui.OpenGameSettings());
        CreateSettingsOpenerButton("PlantSettingsButton", "Plant Settings", gridGo.transform, font, theme,
            ui => ui.OpenPlantSettings());
        CreateSettingsOpenerButton("GrazerSettingsButton", "Grazer Settings", gridGo.transform, font, theme,
            ui => ui.OpenGrazerSettings());
        CreateSettingsOpenerButton("PredatorSettingsButton", "Predator Settings", gridGo.transform, font, theme,
            ui => ui.OpenPredatorSettings());
    }

    static void CreateSettingsOpenerButton(string name, string label, Transform parent, TMP_FontAsset font,
        LifeSimUITheme theme, Action<WorldEditorSettingsUI> open)
    {
        GameObject go = CreateButton(name, label, parent, font, theme);
        go.GetComponent<Button>().onClick.AddListener(() =>
        {
            WorldEditorSettingsUI ui = WorldEditorSettingsUI.Instance;
            if (ui != null)
                open(ui);
        });
    }

    static GameObject CreateButton(string name, string label, Transform parent, TMP_FontAsset font,
        LifeSimUITheme theme)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.raycastTarget = true;

        GameObject textGo = new GameObject("Text (TMP)", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(go.transform, false);
        RectTransform tr = textGo.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.alignment = TextAlignmentOptions.Center;
        if (font != null)
            tmp.font = font;

        LifeSimUIButtonStyle.ApplyStripButton(go, theme, false);
        return go;
    }

    static void BuildSettingsPopupsRoot(Transform canvas, TMP_FontAsset font, LifeSimUITheme theme)
    {
        GameObject root = new GameObject("SettingsPopupsRoot", typeof(RectTransform), typeof(WorldEditorSettingsUI));
        root.transform.SetParent(canvas, false);
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        GameObject blocker = new GameObject("ClickBlocker", typeof(RectTransform), typeof(Image), typeof(Button));
        blocker.transform.SetParent(root.transform, false);
        RectTransform br = blocker.GetComponent<RectTransform>();
        br.anchorMin = Vector2.zero;
        br.anchorMax = Vector2.one;
        br.offsetMin = Vector2.zero;
        br.offsetMax = Vector2.zero;
        Image bi = blocker.GetComponent<Image>();
        bi.color = theme.modalOverlayDim;
        bi.raycastTarget = true;
        Button bb = blocker.GetComponent<Button>();
        bb.transition = Selectable.Transition.None;
        bb.onClick.AddListener(() =>
        {
            if (root.TryGetComponent(out WorldEditorSettingsUI ui))
                ui.Close();
        });

        GameObject shell = new GameObject("SettingsPopoutShell", typeof(RectTransform), typeof(Image));
        shell.transform.SetParent(root.transform, false);
        RectTransform sr = shell.GetComponent<RectTransform>();
        sr.anchorMin = new Vector2(1f, 0.5f);
        sr.anchorMax = new Vector2(1f, 0.5f);
        sr.pivot = new Vector2(1f, 0.5f);
        sr.anchoredPosition = new Vector2(-265f, 0f);
        sr.sizeDelta = new Vector2(300f, 520f);
        Image si = shell.GetComponent<Image>();
        si.color = theme.modalShellBackground;
        si.type = Image.Type.Sliced;
        si.raycastTarget = true;

        GameObject header = new GameObject("HeaderRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        header.transform.SetParent(shell.transform, false);
        RectTransform hr = header.GetComponent<RectTransform>();
        hr.anchorMin = new Vector2(0f, 1f);
        hr.anchorMax = new Vector2(1f, 1f);
        hr.pivot = new Vector2(0.5f, 1f);
        hr.anchoredPosition = new Vector2(0f, -theme.spacingS);
        hr.sizeDelta = new Vector2(-16f, 36f);
        HorizontalLayoutGroup hg = header.GetComponent<HorizontalLayoutGroup>();
        hg.childForceExpandWidth = false;
        hg.childForceExpandHeight = true;
        hg.spacing = theme.spacingS;
        int pad = Mathf.RoundToInt(theme.spacingS);
        hg.padding = new RectOffset(pad, pad, 0, 0);

        GameObject titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(header.transform, false);
        TextMeshProUGUI titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
        titleTmp.text = "Settings";
        titleTmp.fontSize = theme.modalTitleFontSize;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = theme.bodyText;
        if (font != null)
            titleTmp.font = font;
        LayoutElement le = titleGo.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;

        GameObject resetBtn = CreateButton("ResetCategoryButton", "Reset", header.transform, font, theme);
        LifeSimUIButtonStyle.ApplyStripDangerButton(resetBtn, theme);
        LayoutElement resetLe = resetBtn.AddComponent<LayoutElement>();
        resetLe.preferredWidth = 72f;
        Button resetB = resetBtn.GetComponent<Button>();
        resetB.onClick.AddListener(() => root.GetComponent<WorldEditorSettingsUI>().ResetCurrentCategory());

        GameObject scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollGo.transform.SetParent(shell.transform, false);
        RectTransform scr = scrollGo.GetComponent<RectTransform>();
        scr.anchorMin = new Vector2(0f, 0f);
        scr.anchorMax = new Vector2(1f, 1f);
        scr.offsetMin = new Vector2(theme.spacingS, theme.spacingL);
        scr.offsetMax = new Vector2(-theme.spacingS, -48f);
        scrollGo.GetComponent<Image>().color = theme.modalScrollWell;

        ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();
        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollGo.transform, false);
        RectTransform vr = viewport.GetComponent<RectTransform>();
        vr.anchorMin = Vector2.zero;
        vr.anchorMax = Vector2.one;
        vr.offsetMin = Vector2.zero;
        vr.offsetMax = Vector2.zero;
        viewport.GetComponent<Image>().color = theme.modalViewportTint;
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        RectTransform cr = content.GetComponent<RectTransform>();
        cr.anchorMin = new Vector2(0f, 1f);
        cr.anchorMax = new Vector2(1f, 1f);
        cr.pivot = new Vector2(0.5f, 1f);
        cr.anchoredPosition = Vector2.zero;
        cr.sizeDelta = new Vector2(0f, 0f);
        VerticalLayoutGroup vg = content.GetComponent<VerticalLayoutGroup>();
        vg.spacing = theme.spacingM;
        vg.childAlignment = TextAnchor.UpperCenter;
        vg.childControlWidth = true;
        vg.childForceExpandWidth = true;
        ContentSizeFitter fit = content.GetComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = vr;
        scroll.content = cr;
        scroll.horizontal = false;
        scroll.vertical = true;

        WorldEditorSettingsUI settingsUi = root.GetComponent<WorldEditorSettingsUI>();
        settingsUi.InjectBuiltReferences(root, blocker, shell, titleTmp, cr, resetB);
        settingsUi.BuildCategoryPanels(cr, font, theme);

        root.SetActive(false);
    }
}
