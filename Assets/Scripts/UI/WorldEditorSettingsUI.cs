// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Settings modal (world editor)
// Requirement:	Simulation user interface
// Author:		Benjamin Jones
// Date:		04/14/2026
// Version:		0.0.0
//
// Description:
//    Lets the player reshape the live simulation (speed, terrain, populations,
//    traits) from category panels tied to the shared settings store without leaving the scene.
// -----------------------------------------------------------------------------

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static SimulationSettingsValidator.Limits;

/// <summary> In-scene settings modal bound to <see cref="SimulationSettingsStore"/>. </summary>
public class WorldEditorSettingsUI : MonoBehaviour
{
    public static WorldEditorSettingsUI Instance { get; private set; }

    enum Category
    {
        Game,
        Plant,
        Grazer,
        Predator
    }

    [SerializeField] GameObject root;
    [SerializeField] GameObject blocker;
    [SerializeField] GameObject shell;
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] Transform contentRoot;
    [SerializeField] Button resetButton;

    GameObject _panelGame;
    GameObject _panelPlant;
    GameObject _panelGrazer;
    GameObject _panelPredator;

    Category _currentCategory;

    void Awake()
    {
        Instance = this;
    }

    public void InjectBuiltReferences(GameObject rootGo, GameObject blockerGo, GameObject shellGo, TextMeshProUGUI title,
        Transform content, Button reset)
    {
        root = rootGo;
        blocker = blockerGo;
        shell = shellGo;
        titleText = title;
        contentRoot = content;
        resetButton = reset;
    }

    public void BuildCategoryPanels(Transform content, TMP_FontAsset font, LifeSimUITheme theme)
    {
        _panelGame = CreatePanelShell("PanelGame", content, theme);
        _panelPlant = CreatePanelShell("PanelPlant", content, theme);
        _panelGrazer = CreatePanelShell("PanelGrazer", content, theme);
        _panelPredator = CreatePanelShell("PanelPredator", content, theme);

        BuildGame(_panelGame.transform, font, theme);
        BuildPlant(_panelPlant.transform, font, theme);
        BuildGrazer(_panelGrazer.transform, font, theme);
        BuildPredator(_panelPredator.transform, font, theme);

        _panelPlant.SetActive(false);
        _panelGrazer.SetActive(false);
        _panelPredator.SetActive(false);
    }

    public void OpenGameSettings() => Show(Category.Game, "Game settings");

    public void OpenPlantSettings() => Show(Category.Plant, "Plant settings");

    public void OpenGrazerSettings() => Show(Category.Grazer, "Grazer settings");

    public void OpenPredatorSettings() => Show(Category.Predator, "Predator settings");

    public void Close()
    {
        if (root != null)
            root.SetActive(false);
    }

    public void ResetCurrentCategory()
    {
        SimulationSettings def = SimulationSettings.CreateDefaults();
        SimulationSettings cur = SimulationSettingsStore.Instance != null
            ? SimulationSettingsStore.Instance.Current
            : null;
        if (cur == null)
            return;

        switch (_currentCategory)
        {
            case Category.Game:
                cur.game = JsonUtility.FromJson<GameSettingsData>(JsonUtility.ToJson(def.game));
                cur.terrain = JsonUtility.FromJson<TerrainSettingsData>(JsonUtility.ToJson(def.terrain));
                break;
            case Category.Plant:
                cur.plant = JsonUtility.FromJson<PlantSettingsBlock>(JsonUtility.ToJson(def.plant));
                break;
            case Category.Grazer:
                cur.grazer = JsonUtility.FromJson<GrazerSettingsBlock>(JsonUtility.ToJson(def.grazer));
                break;
            case Category.Predator:
                cur.predator = JsonUtility.FromJson<PredatorSettingsBlock>(JsonUtility.ToJson(def.predator));
                break;
        }

        SimulationSettingsStore.Instance.CommitFromCurrent();
    }

    void Show(Category cat, string title)
    {
        _currentCategory = cat;
        if (titleText != null)
            titleText.text = title;
        if (_panelGame != null)
            _panelGame.SetActive(cat == Category.Game);
        if (_panelPlant != null)
            _panelPlant.SetActive(cat == Category.Plant);
        if (_panelGrazer != null)
            _panelGrazer.SetActive(cat == Category.Grazer);
        if (_panelPredator != null)
            _panelPredator.SetActive(cat == Category.Predator);

        if (root != null)
            root.SetActive(true);
    }

    static GameObject CreatePanelShell(string name, Transform parent, LifeSimUITheme theme)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
        go.transform.SetParent(parent, false);
        VerticalLayoutGroup v = go.GetComponent<VerticalLayoutGroup>();
        v.spacing = theme.spacingS;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlWidth = true;
        v.childForceExpandWidth = true;
        return go;
    }

    void BuildGame(Transform parent, TMP_FontAsset font, LifeSimUITheme theme)
    {
        AddSlider(parent, font, theme, "Simulation speed", GameSimulationSpeedMin, GameSimulationSpeedMax, false,
            s => s.game.simulationSpeed, (s, v) => s.game.simulationSpeed = v);
        AddSlider(parent, font, theme, "Rock spawn rate", TerrainRockSpawnMin, TerrainRockSpawnMax, false,
            s => s.terrain.rockSpawnRate, (s, v) => s.terrain.rockSpawnRate = v);
        AddSlider(parent, font, theme, "Obstacle cluster min", TerrainObstacleMinClusterMin, TerrainObstacleMinClusterMax,
            true,
            s => s.terrain.obstacleMinCluster, (s, v) => s.terrain.obstacleMinCluster = Mathf.RoundToInt(v));
        AddSlider(parent, font, theme, "Obstacle cluster max", TerrainObstacleMaxClusterMin, TerrainObstacleMaxClusterMax,
            true,
            s => s.terrain.obstacleMaxCluster, (s, v) => s.terrain.obstacleMaxCluster = Mathf.RoundToInt(v));
        AddSlider(parent, font, theme, "Terrain feature scale", TerrainPerlinScaleMin, TerrainPerlinScaleMax, false,
            s => s.terrain.perlinScale, (s, v) => s.terrain.perlinScale = v);
        AddSlider(parent, font, theme, "Water spawn rate", TerrainWaterSpawnMin, TerrainWaterSpawnMax, false,
            s => s.terrain.waterSpawnRate, (s, v) => s.terrain.waterSpawnRate = v);
    }

    void BuildPlant(Transform parent, TMP_FontAsset font, LifeSimUITheme theme)
    {
        AddSlider(parent, font, theme, "Starting population", PlantStartingPopulationMin, PlantStartingPopulationMax,
            true,
            s => s.plant.startingPopulation, (s, v) => s.plant.startingPopulation = Mathf.RoundToInt(v));
        AddSlider(parent, font, theme, "Max population", PlantMaxPopulationMin, PlantMaxPopulationMax, true,
            s => s.plant.maxPopulation, (s, v) => s.plant.maxPopulation = Mathf.RoundToInt(v));
        AddToggle(parent, font, theme, "Replenish enabled",
            s => s.plant.replenishEnabled, (s, v) => s.plant.replenishEnabled = v);
        AddSlider(parent, font, theme, "Replenish interval (s)", ReplenishIntervalMin, ReplenishIntervalMax, false,
            s => s.plant.replenishIntervalSeconds, (s, v) => s.plant.replenishIntervalSeconds = v);
        AddSlider(parent, font, theme, "Replenish amount (per tick)", 0f, PlantReplenishAmountMax, true,
            s => s.plant.replenishAmount, (s, v) => s.plant.replenishAmount = Mathf.RoundToInt(v));
        AddSlider(parent, font, theme, "Expression: primary / leaf", ExpressionTraitMin, ExpressionTraitMax, false,
            s => s.plant.expression.primaryStats, (s, v) => s.plant.expression.primaryStats = v);
        AddSlider(parent, font, theme, "Expression: secondary", ExpressionTraitMin, ExpressionTraitMax, false,
            s => s.plant.expression.secondaryTraits, (s, v) => s.plant.expression.secondaryTraits = v);
        AddSlider(parent, font, theme, "Expression: defense", ExpressionTraitMin, ExpressionTraitMax, false,
            s => s.plant.expression.defenseTraits, (s, v) => s.plant.expression.defenseTraits = v);
    }

    void BuildGrazer(Transform parent, TMP_FontAsset font, LifeSimUITheme theme)
    {
        AddSlider(parent, font, theme, "Starting population", GrazerStartingPopulationMin, GrazerStartingPopulationMax,
            true,
            s => s.grazer.startingPopulation, (s, v) => s.grazer.startingPopulation = Mathf.RoundToInt(v));
        AddSlider(parent, font, theme, "Max population", GrazerMaxPopulationMin, GrazerMaxPopulationMax, true,
            s => s.grazer.maxPopulation, (s, v) => s.grazer.maxPopulation = Mathf.RoundToInt(v));
        AddToggle(parent, font, theme, "Replenish enabled",
            s => s.grazer.replenishEnabled, (s, v) => s.grazer.replenishEnabled = v);
        AddSlider(parent, font, theme, "Replenish interval (s)", ReplenishIntervalMin, ReplenishIntervalMax, false,
            s => s.grazer.replenishIntervalSeconds, (s, v) => s.grazer.replenishIntervalSeconds = v);
        AddSlider(parent, font, theme, "Replenish amount (per tick)", 0f, GrazerReplenishAmountMax, true,
            s => s.grazer.replenishAmount, (s, v) => s.grazer.replenishAmount = Mathf.RoundToInt(v));
        AddSlider(parent, font, theme, "Expression: stat traits", ExpressionTraitMin, ExpressionTraitMax, false,
            s => s.grazer.expression.statTraits, (s, v) => s.grazer.expression.statTraits = v);
        AddSlider(parent, font, theme, "Expression: rare traits", ExpressionTraitMin, ExpressionTraitMax, false,
            s => s.grazer.expression.rareTraits, (s, v) => s.grazer.expression.rareTraits = v);
        AddSlider(parent, font, theme, "Expression: pack / leader", ExpressionTraitMin, ExpressionTraitMax, false,
            s => s.grazer.expression.packTraits, (s, v) => s.grazer.expression.packTraits = v);
    }

    void BuildPredator(Transform parent, TMP_FontAsset font, LifeSimUITheme theme)
    {
        AddSlider(parent, font, theme, "Starting population", PredatorStartingPopulationMin, PredatorStartingPopulationMax,
            true,
            s => s.predator.startingPopulation, (s, v) => s.predator.startingPopulation = Mathf.RoundToInt(v));
        AddSlider(parent, font, theme, "Max population", PredatorMaxPopulationMin, PredatorMaxPopulationMax, true,
            s => s.predator.maxPopulation, (s, v) => s.predator.maxPopulation = Mathf.RoundToInt(v));
        AddToggle(parent, font, theme, "Replenish enabled",
            s => s.predator.replenishEnabled, (s, v) => s.predator.replenishEnabled = v);
        AddSlider(parent, font, theme, "Replenish interval (s)", ReplenishIntervalMin, ReplenishIntervalMax, false,
            s => s.predator.replenishIntervalSeconds, (s, v) => s.predator.replenishIntervalSeconds = v);
        AddSlider(parent, font, theme, "Replenish amount (per tick)", 0f, PredatorReplenishAmountMax, true,
            s => s.predator.replenishAmount, (s, v) => s.predator.replenishAmount = Mathf.RoundToInt(v));
        AddSlider(parent, font, theme, "Expression: stat traits", ExpressionTraitMin, ExpressionTraitMax, false,
            s => s.predator.expression.statTraits, (s, v) => s.predator.expression.statTraits = v);
        AddSlider(parent, font, theme, "Expression: rare traits", ExpressionTraitMin, ExpressionTraitMax, false,
            s => s.predator.expression.rareTraits, (s, v) => s.predator.expression.rareTraits = v);
        AddSlider(parent, font, theme, "Expression: apex", ExpressionTraitMin, ExpressionTraitMax, false,
            s => s.predator.expression.apexTraits, (s, v) => s.predator.expression.apexTraits = v);
    }

    void AddSlider(Transform parent, TMP_FontAsset font, LifeSimUITheme theme, string label, float min, float max, bool whole,
        Func<SimulationSettings, float> get, Action<SimulationSettings, float> set)
    {
        GameObject row = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(parent, false);
        HorizontalLayoutGroup h = row.GetComponent<HorizontalLayoutGroup>();
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = true;
        h.spacing = theme.spacingXs;
        int py = Mathf.Max(1, Mathf.RoundToInt(theme.spacingXs * 0.33f));
        h.padding = new RectOffset(0, 0, py, py);

        GameObject labGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labGo.transform.SetParent(row.transform, false);
        TextMeshProUGUI lab = labGo.GetComponent<TextMeshProUGUI>();
        lab.text = label;
        lab.fontSize = theme.formRowLabelFontSize;
        lab.color = theme.bodyText;
        if (font != null)
            lab.font = font;
        LayoutElement leL = labGo.AddComponent<LayoutElement>();
        leL.preferredWidth = 148f;
        leL.minWidth = 148f;

        GameObject valGo = new GameObject("Value", typeof(RectTransform), typeof(TextMeshProUGUI));
        valGo.transform.SetParent(row.transform, false);
        TextMeshProUGUI valTmp = valGo.GetComponent<TextMeshProUGUI>();
        valTmp.fontSize = theme.formRowLabelFontSize;
        valTmp.color = theme.bodyText;
        if (font != null)
            valTmp.font = font;
        LayoutElement leV = valGo.AddComponent<LayoutElement>();
        leV.preferredWidth = 48f;

        Slider sl = CreateSliderSimple(row.transform, theme);
        LayoutElement leS = sl.gameObject.AddComponent<LayoutElement>();
        leS.flexibleWidth = 1f;
        leS.minHeight = 22f;

        sl.minValue = min;
        sl.maxValue = max;
        sl.wholeNumbers = whole;

        void RefreshValue()
        {
            if (SimulationSettingsStore.Instance == null)
                return;
            float v = get(SimulationSettingsStore.Instance.Current);
            sl.SetValueWithoutNotify(v);
            valTmp.text = whole ? Mathf.RoundToInt(v).ToString() : v.ToString("0.###");
        }

        sl.onValueChanged.AddListener(v =>
        {
            if (SimulationSettingsStore.Instance == null)
                return;
            float w = whole ? Mathf.Round(v) : v;
            set(SimulationSettingsStore.Instance.Current, w);
            valTmp.text = whole ? Mathf.RoundToInt(w).ToString() : w.ToString("0.###");
            SimulationSettingsStore.Instance.CommitFromCurrent();
        });

        if (SimulationSettingsStore.Instance != null)
            SimulationSettingsStore.Instance.SettingsApplied += RefreshValue;

        RefreshValue();
    }

    void AddToggle(Transform parent, TMP_FontAsset font, LifeSimUITheme theme, string label,
        Func<SimulationSettings, bool> get, Action<SimulationSettings, bool> set)
    {
        const float togglePx = 24f;

        GameObject row = new GameObject("ToggleRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(parent, false);
        HorizontalLayoutGroup h = row.GetComponent<HorizontalLayoutGroup>();
        h.spacing = theme.spacingS;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childForceExpandHeight = true;
        LayoutElement rowLe = row.AddComponent<LayoutElement>();
        rowLe.minHeight = 28f;
        rowLe.preferredHeight = 28f;

        GameObject labGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labGo.transform.SetParent(row.transform, false);
        TextMeshProUGUI lab = labGo.GetComponent<TextMeshProUGUI>();
        lab.text = label;
        lab.fontSize = theme.formRowLabelFontSize;
        lab.color = theme.bodyText;
        if (font != null)
            lab.font = font;
        LayoutElement leL = labGo.AddComponent<LayoutElement>();
        leL.preferredWidth = 148f;
        leL.flexibleWidth = 1f;

        // Root must have explicit size or VerticalLayoutGroup collapses the control to zero width (invisible toggle).
        GameObject tRoot = new GameObject("Toggle", typeof(RectTransform));
        tRoot.transform.SetParent(row.transform, false);
        RectTransform tRt = tRoot.GetComponent<RectTransform>();
        tRt.sizeDelta = new Vector2(togglePx, togglePx);
        LayoutElement tLe = tRoot.AddComponent<LayoutElement>();
        tLe.preferredWidth = togglePx;
        tLe.preferredHeight = togglePx;
        tLe.minWidth = togglePx;
        tLe.minHeight = togglePx;

        Toggle t = tRoot.AddComponent<Toggle>();
        t.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = t.colors;
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
        t.colors = colors;

        GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(tRoot.transform, false);
        RectTransform bgr = bg.GetComponent<RectTransform>();
        bgr.anchorMin = Vector2.zero;
        bgr.anchorMax = Vector2.one;
        bgr.offsetMin = Vector2.zero;
        bgr.offsetMax = Vector2.zero;
        Image bgImg = bg.GetComponent<Image>();
        bgImg.color = theme.toggleBoxBackground;
        t.targetGraphic = bgImg;

        GameObject mark = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        mark.transform.SetParent(bg.transform, false);
        RectTransform mr = mark.GetComponent<RectTransform>();
        mr.anchorMin = new Vector2(0.1f, 0.1f);
        mr.anchorMax = new Vector2(0.9f, 0.9f);
        mr.offsetMin = Vector2.zero;
        mr.offsetMax = Vector2.zero;
        Image markImg = mark.GetComponent<Image>();
        markImg.color = theme.toggleCheckmark;
        t.graphic = markImg;

        void RefreshT()
        {
            if (SimulationSettingsStore.Instance == null)
                return;
            t.SetIsOnWithoutNotify(get(SimulationSettingsStore.Instance.Current));
        }

        t.onValueChanged.AddListener(on =>
        {
            if (SimulationSettingsStore.Instance == null)
                return;
            set(SimulationSettingsStore.Instance.Current, on);
            SimulationSettingsStore.Instance.CommitFromCurrent();
        });

        if (SimulationSettingsStore.Instance != null)
            SimulationSettingsStore.Instance.SettingsApplied += RefreshT;

        RefreshT();
    }

    static Slider CreateSliderSimple(Transform parent, LifeSimUITheme theme)
    {
        GameObject root = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        root.transform.SetParent(parent, false);
        Slider sl = root.GetComponent<Slider>();

        GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(root.transform, false);
        RectTransform bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;
        bg.GetComponent<Image>().color = theme.sliderTrackBackground;

        GameObject fa = new GameObject("Fill Area", typeof(RectTransform));
        fa.transform.SetParent(root.transform, false);
        RectTransform faRt = fa.GetComponent<RectTransform>();
        faRt.anchorMin = Vector2.zero;
        faRt.anchorMax = Vector2.one;
        faRt.sizeDelta = Vector2.zero;

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fa.transform, false);
        RectTransform fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.sizeDelta = Vector2.zero;
        fill.GetComponent<Image>().color = theme.sliderFill;

        GameObject ha = new GameObject("Handle Slide Area", typeof(RectTransform));
        ha.transform.SetParent(root.transform, false);
        RectTransform haRt = ha.GetComponent<RectTransform>();
        haRt.anchorMin = Vector2.zero;
        haRt.anchorMax = Vector2.one;
        haRt.sizeDelta = Vector2.zero;

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(ha.transform, false);
        RectTransform hRt = handle.GetComponent<RectTransform>();
        hRt.sizeDelta = new Vector2(16f, 16f);

        Image hImg = handle.GetComponent<Image>();
        hImg.color = theme.sliderHandle;

        sl.fillRect = fillRt;
        sl.handleRect = hRt;
        sl.targetGraphic = hImg;
        sl.direction = Slider.Direction.LeftToRight;
        return sl;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
