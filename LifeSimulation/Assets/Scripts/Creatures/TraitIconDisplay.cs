// -----------------------------------------------------------------------------
// TraitIconDisplay.cs
// Shows a single row of small trait icons below a creature or plant.
// Icons shrink to fit if there are many traits expressed.
//
// Setup:
//   1. Attach to the root of any Grazer, Predator, or Plant prefab.
//   2. Assign icon sprites in the Inspector for each trait.
//   3. Set yOffset to position the row below the sprite.
//
// Icons are created as child SpriteRenderers at runtime.
// -----------------------------------------------------------------------------
using UnityEngine;
using System.Collections.Generic;

public class TraitIconDisplay : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private float yOffset = -0.6f;   // below the sprite
    [SerializeField] private float maxRowWidth = 1.4f;    // total row width in world units
    [SerializeField] private float baseIconSize = 0.25f;   // icon size at default scale
    [SerializeField] private int sortingOrder = 3;       // above sprite and cone

    [Header("Grazer Trait Icons")]
    public Sprite iconGrazerNimble;
    public Sprite iconGrazerStrong;
    public Sprite iconGrazerThickSkinned;
    public Sprite iconCamouflage;
    public Sprite iconSpiky;

    [Header("Predator Trait Icons")]
    public Sprite iconPredatorNimble;
    public Sprite iconPredatorStrong;
    public Sprite iconPredatorThickSkinned;
    public Sprite iconVenomous;
    public Sprite iconAmbusher;
    public Sprite iconApexPredator;

    [Header("Plant Trait Icons")]
    public Sprite iconLeafSmall;
    public Sprite iconLeafMedium;
    public Sprite iconLeafLarge;
    public Sprite iconTasty;
    public Sprite iconBitter;
    public Sprite iconPoisonous;
    public Sprite iconResilient;

    private readonly List<GameObject> _icons = new List<GameObject>();

    private void Start()
    {
        // Defer one frame so genetics components have run Awake/Init
        Invoke(nameof(BuildIcons), 0.05f);
    }

    private void BuildIcons()
    {
        // Clear any previously built icons
        foreach (var go in _icons) if (go != null) Destroy(go);
        _icons.Clear();

        List<Sprite> sprites = GatherSprites();
        if (sprites.Count == 0) return;

        // Scale icons down if they don't all fit at base size
        float iconSize = Mathf.Min(baseIconSize, maxRowWidth / sprites.Count);
        float spacing = iconSize;
        float totalWidth = spacing * (sprites.Count - 1);
        float startX = -totalWidth * 0.5f;

        for (int i = 0; i < sprites.Count; i++)
        {
            if (sprites[i] == null) continue;

            GameObject icon = new GameObject($"TraitIcon_{i}");
            icon.transform.SetParent(transform);
            icon.transform.localPosition = new Vector3(startX + spacing * i, yOffset, 0f);
            icon.transform.localScale = Vector3.one * iconSize;
            icon.transform.localRotation = Quaternion.identity;

            SpriteRenderer sr = icon.AddComponent<SpriteRenderer>();
            sr.sprite = sprites[i];
            sr.sortingLayerName = "Default";
            sr.sortingOrder = sortingOrder;

            _icons.Add(icon);
        }
    }

    private List<Sprite> GatherSprites()
    {
        var list = new List<Sprite>();

        // Try Grazer genetics
        GrazerGenetics gg = GetComponent<GrazerGenetics>();
        if (gg != null && gg.Genome != null)
        {
            if (gg.Genome.IsExpressed(TraitType.GrazerNimble)) AddIfNotNull(list, iconGrazerNimble);
            if (gg.Genome.IsExpressed(TraitType.GrazerStrong)) AddIfNotNull(list, iconGrazerStrong);
            if (gg.Genome.IsExpressed(TraitType.GrazerThickSkinned)) AddIfNotNull(list, iconGrazerThickSkinned);
            if (gg.Genome.IsExpressed(TraitType.Camouflage)) AddIfNotNull(list, iconCamouflage);
            if (gg.Genome.IsExpressed(TraitType.Spiky)) AddIfNotNull(list, iconSpiky);
            return list;
        }

        // Try Predator genetics
        PredatorGenetics pg = GetComponent<PredatorGenetics>();
        if (pg != null && pg.Genome != null)
        {
            if (pg.IsApexPredator)
            {
                AddIfNotNull(list, iconApexPredator);
            }
            else
            {
                if (pg.Genome.IsExpressed(TraitType.PredatorNimble)) AddIfNotNull(list, iconPredatorNimble);
                if (pg.Genome.IsExpressed(TraitType.PredatorStrong)) AddIfNotNull(list, iconPredatorStrong);
                if (pg.Genome.IsExpressed(TraitType.PredatorThickSkinned)) AddIfNotNull(list, iconPredatorThickSkinned);
                if (pg.Genome.IsExpressed(TraitType.Venomous)) AddIfNotNull(list, iconVenomous);
                if (pg.Genome.IsExpressed(TraitType.Ambusher)) AddIfNotNull(list, iconAmbusher);
            }
            return list;
        }

        // Try Plant genetics
        PlantGenetics plantG = GetComponent<PlantGenetics>();
        if (plantG != null && plantG.Genome != null)
        {
            // Leaf size uses allele count (0=small,1=med,2=large)
            Gene leafGene = plantG.Genome.Get(TraitType.LeafSize);
            if (leafGene != null)
            {
                int leafLevel = (leafGene.AlleleA ? 1 : 0) + (leafGene.AlleleB ? 1 : 0);
                Sprite leafIcon = leafLevel == 0 ? iconLeafSmall
                                : leafLevel == 1 ? iconLeafMedium
                                : iconLeafLarge;
                AddIfNotNull(list, leafIcon);
            }

            if (plantG.Genome.IsExpressed(TraitType.Tasty)) AddIfNotNull(list, iconTasty);
            if (plantG.Genome.IsExpressed(TraitType.Bitter)) AddIfNotNull(list, iconBitter);
            if (plantG.Genome.IsExpressed(TraitType.Poisonous)) AddIfNotNull(list, iconPoisonous);
            if (plantG.Genome.IsExpressed(TraitType.Resilient)) AddIfNotNull(list, iconResilient);
        }

        return list;
    }

    private static void AddIfNotNull(List<Sprite> list, Sprite s)
    {
        if (s != null) list.Add(s);
    }

    /// <summary>
    /// Call this to rebuild icons after a genome changes (e.g. on inherited offspring).
    /// </summary>
    public void Refresh() => BuildIcons();
}