// -----------------------------------------------------------------------------
// Category settings popout: sliders bound to SimulationSettingsStore.
// Does not pause the simulation (no timeScale changes).
// -----------------------------------------------------------------------------

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    public void BuildCategoryPanels(Transform content, TMP_FontAsset font)
    {
        _panelGame = CreatePanelShell("PanelGame", content);
        _panelPlant = CreatePanelShell("PanelPlant", content);
        _panelGrazer = CreatePanelShell("PanelGrazer", content);
        _panelPredator = CreatePanelShell("PanelPredator", content);

        BuildGame(_panelGame.transform, font);
        BuildPlant(_panelPlant.transform, font);
        BuildGrazer(_panelGrazer.transform, font);
        BuildPredator(_panelPredator.transform, font);

        _panelPlant.SetActive(false);
        _panelGrazer.SetActive(false);
        _panelPredator.SetActive(false);
    }

    static GameObject CreatePanelShell(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
        go.transform.SetParent(parent, false);
        VerticalLayoutGroup v = go.GetComponent<VerticalLayoutGroup>();
        v.spacing = 8f;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlWidth = true;
        v.childForceExpandWidth = true;
        return go;
    }

    void BuildGame(Transform parent, TMP_FontAsset font)
    {
        AddSlider(parent, font, "Simulation speed", 0f, 10f, false,
            s => s.game.simulationSpeed, (s, v) => s.game.simulationSpeed = v);
        AddSlider(parent, font, "Rock spawn rate", 0f, 0.03f, false,
            s => s.terrain.rockSpawnRate, (s, v) => s.terrain.rockSpawnRate = v);
        AddSlider(parent, font, "Obstacle cluster min", 1f, 4f, true,
            s => s.terrain.obstacleMinCluster, (s, v) => s.terrain.obstacleMinCluster = Mathf.RoundToInt(v));
        AddSlider(parent, font, "Obstacle cluster max", 1f, 6f, true,
            s => s.terrain.obstacleMaxCluster, (s, v) => s.terrain.obstacleMaxCluster = Mathf.RoundToInt(v));
        AddSlider(parent, font, "Map width (tiles)", 20f, 500f, true,
            s => s.terrain.mapWidth, (s, v) => s.terrain.mapWidth = Mathf.RoundToInt(v));
        AddSlider(parent, font, "Map height (tiles)", 20f, 500f, true,
            s => s.terrain.mapHeight, (s, v) => s.terrain.mapHeight = Mathf.RoundToInt(v));
        AddSlider(parent, font, "Terrain feature scale", 0.02f, 0.3f, false,
            s => s.terrain.perlinScale, (s, v) => s.terrain.perlinScale = v);
        AddSlider(parent, font, "Water spawn rate", 0f, 1f, false,
            s => s.terrain.waterSpawnRate, (s, v) => s.terrain.waterSpawnRate = v);
    }

    void BuildPlant(Transform parent, TMP_FontAsset font)
    {
        AddSlider(parent, font, "Starting population", 0f, 60f, true,
            s => s.plant.startingPopulation, (s, v) => s.plant.startingPopulation = Mathf.RoundToInt(v));
        AddSlider(parent, font, "Max population", 10f, 90f, true,
            s => s.plant.maxPopulation, (s, v) => s.plant.maxPopulation = Mathf.RoundToInt(v));
        AddToggle(parent, font, "Replenish enabled",
            s => s.plant.replenishEnabled, (s, v) => s.plant.replenishEnabled = v);
        AddSlider(parent, font, "Replenish interval (s)", 1f, 30f, false,
            s => s.plant.replenishIntervalSeconds, (s, v) => s.plant.replenishIntervalSeconds = v);
        AddSlider(parent, font, "Replenish amount", 0f, 20f, true,
            s => s.plant.replenishAmount, (s, v) => s.plant.replenishAmount = Mathf.RoundToInt(v));
        AddSlider(parent, font, "Expression: primary / leaf", 0f, 3f, false,
            s => s.plant.expression.primaryStats, (s, v) => s.plant.expression.primaryStats = v);
        AddSlider(parent, font, "Expression: secondary", 0f, 3f, false,
            s => s.plant.expression.secondaryTraits, (s, v) => s.plant.expression.secondaryTraits = v);
        AddSlider(parent, font, "Expression: defense", 0f, 3f, false,
            s => s.plant.expression.defenseTraits, (s, v) => s.plant.expression.defenseTraits = v);
    }

    void BuildGrazer(Transform parent, TMP_FontAsset font)
    {
        AddSlider(parent, font, "Starting population", 0f, 40f, true,
            s => s.grazer.startingPopulation, (s, v) => s.grazer.startingPopulation = Mathf.RoundToInt(v));
        AddSlider(parent, font, "Max population", 10f, 50f, true,
            s => s.grazer.maxPopulation, (s, v) => s.grazer.maxPopulation = Mathf.RoundToInt(v));
        AddToggle(parent, font, "Replenish enabled",
            s => s.grazer.replenishEnabled, (s, v) => s.grazer.replenishEnabled = v);
        AddSlider(parent, font, "Replenish interval (s)", 0.5f, 30f, false,
            s => s.grazer.replenishIntervalSeconds, (s, v) => s.grazer.replenishIntervalSeconds = v);
        AddSlider(parent, font, "Replenish amount", 0f, 20f, true,
            s => s.grazer.replenishAmount, (s, v) => s.grazer.replenishAmount = Mathf.RoundToInt(v));
        AddSlider(parent, font, "Expression: stat traits", 0f, 3f, false,
            s => s.grazer.expression.statTraits, (s, v) => s.grazer.expression.statTraits = v);
        AddSlider(parent, font, "Expression: rare traits", 0f, 3f, false,
            s => s.grazer.expression.rareTraits, (s, v) => s.grazer.expression.rareTraits = v);
        AddSlider(parent, font, "Expression: pack / leader", 0f, 3f, false,
            s => s.grazer.expression.packTraits, (s, v) => s.grazer.expression.packTraits = v);
    }

    void BuildPredator(Transform parent, TMP_FontAsset font)
    {
        AddSlider(parent, font, "Starting population", 0f, 12f, true,
            s => s.predator.startingPopulation, (s, v) => s.predator.startingPopulation = Mathf.RoundToInt(v));
        AddSlider(parent, font, "Max population", 2f, 18f, true,
            s => s.predator.maxPopulation, (s, v) => s.predator.maxPopulation = Mathf.RoundToInt(v));
        AddToggle(parent, font, "Replenish enabled",
            s => s.predator.replenishEnabled, (s, v) => s.predator.replenishEnabled = v);
        AddSlider(parent, font, "Replenish interval (s)", 0.5f, 60f, false,
            s => s.predator.replenishIntervalSeconds, (s, v) => s.predator.replenishIntervalSeconds = v);
        AddSlider(parent, font, "Replenish amount", 0f, 20f, true,
            s => s.predator.replenishAmount, (s, v) => s.predator.replenishAmount = Mathf.RoundToInt(v));
        AddSlider(parent, font, "Expression: stat traits", 0f, 3f, false,
            s => s.predator.expression.statTraits, (s, v) => s.predator.expression.statTraits = v);
        AddSlider(parent, font, "Expression: rare traits", 0f, 3f, false,
            s => s.predator.expression.rareTraits, (s, v) => s.predator.expression.rareTraits = v);
        AddSlider(parent, font, "Expression: apex", 0f, 3f, false,
            s => s.predator.expression.apexTraits, (s, v) => s.predator.expression.apexTraits = v);
    }

    void AddSlider(Transform parent, TMP_FontAsset font, string label, float min, float max, bool whole,
        Func<SimulationSettings, float> get, Action<SimulationSettings, float> set)
    {
        GameObject row = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(parent, false);
        HorizontalLayoutGroup h = row.GetComponent<HorizontalLayoutGroup>();
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = true;
        h.spacing = 6f;
        h.padding = new RectOffset(0, 0, 2, 2);

        GameObject labGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labGo.transform.SetParent(row.transform, false);
        TextMeshProUGUI lab = labGo.GetComponent<TextMeshProUGUI>();
        lab.text = label;
        lab.fontSize = 13f;
        lab.color = WorldEditorUIBuilder.TextDimPublic;
        if (font != null)
            lab.font = font;
        LayoutElement leL = labGo.AddComponent<LayoutElement>();
        leL.preferredWidth = 148f;
        leL.minWidth = 148f;

        GameObject valGo = new GameObject("Value", typeof(RectTransform), typeof(TextMeshProUGUI));
        valGo.transform.SetParent(row.transform, false);
        TextMeshProUGUI valTmp = valGo.GetComponent<TextMeshProUGUI>();
        valTmp.fontSize = 13f;
        valTmp.color = WorldEditorUIBuilder.TextDimPublic;
        if (font != null)
            valTmp.font = font;
        LayoutElement leV = valGo.AddComponent<LayoutElement>();
        leV.preferredWidth = 48f;

        Slider sl = CreateSliderSimple(row.transform);
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

    void AddToggle(Transform parent, TMP_FontAsset font, string label,
        Func<SimulationSettings, bool> get, Action<SimulationSettings, bool> set)
    {
        GameObject row = new GameObject("ToggleRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(parent, false);
        HorizontalLayoutGroup h = row.GetComponent<HorizontalLayoutGroup>();
        h.spacing = 8f;

        GameObject labGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labGo.transform.SetParent(row.transform, false);
        TextMeshProUGUI lab = labGo.GetComponent<TextMeshProUGUI>();
        lab.text = label;
        lab.fontSize = 13f;
        lab.color = WorldEditorUIBuilder.TextDimPublic;
        if (font != null)
            lab.font = font;
        LayoutElement leL = labGo.AddComponent<LayoutElement>();
        leL.preferredWidth = 200f;

        GameObject tRoot = new GameObject("Toggle", typeof(RectTransform));
        tRoot.transform.SetParent(row.transform, false);
        Toggle t = tRoot.AddComponent<Toggle>();
        GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(tRoot.transform, false);
        RectTransform bgr = bg.GetComponent<RectTransform>();
        bgr.sizeDelta = new Vector2(22f, 22f);
        Image bgImg = bg.GetComponent<Image>();
        bgImg.color = new Color(1f, 1f, 1f, 0.9f);
        t.graphic = bgImg;
        t.targetGraphic = bgImg;

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

    static Slider CreateSliderSimple(Transform parent)
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
        bg.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.6f);

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
        fill.GetComponent<Image>().color = new Color(0.18f, 0.65f, 0.62f, 1f);

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
        hImg.color = Color.white;

        sl.fillRect = fillRt;
        sl.handleRect = hRt;
        sl.targetGraphic = hImg;
        sl.direction = Slider.Direction.LeftToRight;
        return sl;
    }

    public void OpenGameSettings() => Show(Category.Game, "Game settings");
    public void OpenPlantSettings() => Show(Category.Plant, "Plant settings");
    public void OpenGrazerSettings() => Show(Category.Grazer, "Grazer settings");
    public void OpenPredatorSettings() => Show(Category.Predator, "Predator settings");

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

        SimulationSettingsValidator.TryValidate(cur, out _);
        SimulationSettingsStore.Instance.CommitFromCurrent();
        RefreshAllSliders();
    }

    public void RefreshAllSliders()
    {
        if (SimulationSettingsStore.Instance != null)
            SimulationSettingsStore.Instance.ApplyAll();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
