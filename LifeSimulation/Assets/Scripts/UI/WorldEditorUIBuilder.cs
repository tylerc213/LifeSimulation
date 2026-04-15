// -----------------------------------------------------------------------------
// Builds world editor settings UI at runtime on the Simulation scene Canvas.
// -----------------------------------------------------------------------------

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class WorldEditorUIBuilder
{
    static readonly Color PanelTint = new Color(0.329f, 0.763f, 0.840f, 0.392f);
    static readonly Color ButtonTint = new Color(0.102f, 0.710f, 0.694f, 1f);
    static readonly Color PopoutTint = new Color(0.329f, 0.763f, 0.840f, 0.92f);
    static readonly Color TextDim = new Color(0.196f, 0.196f, 0.196f, 1f);

    public static Color TextDimPublic => TextDim;

    public static void EnsureBuilt(Canvas canvas)
    {
        if (canvas == null)
            return;

        TMP_FontAsset font = GetSceneFont();

        Transform editorPanel = canvas.transform.Find("EditorPanel");
        if (editorPanel == null)
            return;

        if (editorPanel.Find("PauseSimulationButton") == null)
            BuildPauseButton(editorPanel, font);

        if (editorPanel.Find("SettingsButtonsGrid") == null)
            BuildSettingsButtons(editorPanel, font);

        if (canvas.transform.Find("SettingsPopupsRoot") == null)
            BuildSettingsPopupsRoot(canvas.transform, font);
    }

    static TMP_FontAsset GetSceneFont()
    {
        TextMeshProUGUI any = Object.FindFirstObjectByType<TextMeshProUGUI>();
        return any != null ? any.font : null;
    }

    static int GetInsertIndexAfterGenerateMap(Transform editorPanel)
    {
        Transform gen = editorPanel.Find("GenerateMapButton");
        return gen != null ? gen.GetSiblingIndex() + 1 : editorPanel.childCount;
    }

    static void BuildPauseButton(Transform editorPanel, TMP_FontAsset font)
    {
        GameObject go = CreateButton("PauseSimulationButton", "Pause", editorPanel, font);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200f, 36f);
        go.AddComponent<PauseSimulationButtonController>();
        go.transform.SetSiblingIndex(GetInsertIndexAfterGenerateMap(editorPanel));
    }

    static void BuildSettingsButtons(Transform editorPanel, TMP_FontAsset font)
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
        grid.spacing = new Vector2(8f, 8f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        grid.childAlignment = TextAnchor.MiddleCenter;

        CreateSettingsOpenerButton("GameSettingsButton", "Game Settings", gridGo.transform, font);
        CreateSettingsOpenerButton("PlantSettingsButton", "Plant Settings", gridGo.transform, font);
        CreateSettingsOpenerButton("GrazerSettingsButton", "Grazer Settings", gridGo.transform, font);
        CreateSettingsOpenerButton("PredatorSettingsButton", "Predator Settings", gridGo.transform, font);
    }

    static void CreateSettingsOpenerButton(string name, string label, Transform parent, TMP_FontAsset font)
    {
        GameObject go = CreateButton(name, label, parent, font);
        Button b = go.GetComponent<Button>();
        string n = name;
        b.onClick.AddListener(() =>
        {
            WorldEditorSettingsUI ui = WorldEditorSettingsUI.Instance;
            if (ui == null)
                return;
            if (n.Contains("Game")) ui.OpenGameSettings();
            else if (n.Contains("Plant")) ui.OpenPlantSettings();
            else if (n.Contains("Grazer")) ui.OpenGrazerSettings();
            else if (n.Contains("Predator")) ui.OpenPredatorSettings();
        });
    }

    static GameObject CreateButton(string name, string label, Transform parent, TMP_FontAsset font)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.color = ButtonTint;
        img.type = Image.Type.Sliced;
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
        tmp.fontSize = 16f;
        tmp.color = TextDim;
        tmp.alignment = TextAlignmentOptions.Center;
        if (font != null)
            tmp.font = font;

        return go;
    }

    static void BuildSettingsPopupsRoot(Transform canvas, TMP_FontAsset font)
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
        bi.color = new Color(0f, 0f, 0f, 0.01f);
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
        si.color = PopoutTint;
        si.type = Image.Type.Sliced;
        si.raycastTarget = true;

        GameObject header = new GameObject("HeaderRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        header.transform.SetParent(shell.transform, false);
        RectTransform hr = header.GetComponent<RectTransform>();
        hr.anchorMin = new Vector2(0f, 1f);
        hr.anchorMax = new Vector2(1f, 1f);
        hr.pivot = new Vector2(0.5f, 1f);
        hr.anchoredPosition = new Vector2(0f, -8f);
        hr.sizeDelta = new Vector2(-16f, 36f);
        HorizontalLayoutGroup hg = header.GetComponent<HorizontalLayoutGroup>();
        hg.childForceExpandWidth = false;
        hg.childForceExpandHeight = true;
        hg.spacing = 8f;
        hg.padding = new RectOffset(8, 8, 0, 0);

        GameObject titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(header.transform, false);
        TextMeshProUGUI titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
        titleTmp.text = "Settings";
        titleTmp.fontSize = 20f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = TextDim;
        if (font != null)
            titleTmp.font = font;
        LayoutElement le = titleGo.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;

        GameObject resetBtn = CreateButton("ResetCategoryButton", "Reset", header.transform, font);
        LayoutElement resetLe = resetBtn.AddComponent<LayoutElement>();
        resetLe.preferredWidth = 72f;
        Button resetB = resetBtn.GetComponent<Button>();
        resetB.onClick.AddListener(() =>
        {
            WorldEditorSettingsUI ui = root.GetComponent<WorldEditorSettingsUI>();
            ui.ResetCurrentCategory();
        });

        GameObject scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollGo.transform.SetParent(shell.transform, false);
        RectTransform scr = scrollGo.GetComponent<RectTransform>();
        scr.anchorMin = new Vector2(0f, 0f);
        scr.anchorMax = new Vector2(1f, 1f);
        scr.offsetMin = new Vector2(8f, 12f);
        scr.offsetMax = new Vector2(-8f, -48f);
        scrollGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);

        ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();
        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollGo.transform, false);
        RectTransform vr = viewport.GetComponent<RectTransform>();
        vr.anchorMin = Vector2.zero;
        vr.anchorMax = Vector2.one;
        vr.offsetMin = Vector2.zero;
        vr.offsetMax = Vector2.zero;
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
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
        vg.spacing = 10f;
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
        settingsUi.BuildCategoryPanels(cr, font);

        root.SetActive(false);
    }
}
