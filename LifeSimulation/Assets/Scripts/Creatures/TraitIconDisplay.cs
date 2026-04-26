// -----------------------------------------------------------------------------
// Project:     EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:        Lifeform Visuals
// Requirement: Lifeform Requirements
// Author:      Luke Kivett
// Date:        04/06/2026
// Version:     0.0.0
//
// Description:
//    Displays a single row of small sprite icons below a creature or plant to
//    indicate which genetic traits are expressed. Icons shrink to fit one row
//    when many traits are active simultaneously.
// -----------------------------------------------------------------------------
using UnityEngine;
using System.Collections.Generic;

/// <summary>Renders a row of trait icons below any entity with a genetics component.</summary>
/// <remarks>
/// Attach to the root of Grazer, Predator, or Plant prefabs. Assign icon sprites
/// in the Inspector for each trait. Unassigned slots produce no icon. Call
/// Refresh after genome assignment to rebuild the icon row.
/// </remarks>
public class TraitIconDisplay : MonoBehaviour
{
    [Header("Layout")]
    // Vertical offset below the sprite in world units
    [SerializeField] private float yOffset      = -0.6f;
    // Maximum total width of the icon row; icons shrink to fit within this
    [SerializeField] private float maxRowWidth  = 1.4f;
    // Icon size at default scale before shrinking
    [SerializeField] private float baseIconSize = 0.25f;
    // Sorting order places icons above sprite and cone layers
    [SerializeField] private int   sortingOrder = 3;

    [Header("Grazer Trait Icons")]
    public Sprite iconGrazerNimble;
    public Sprite iconGrazerStrong;
    public Sprite iconGrazerThickSkinned;
    public Sprite iconCamouflage;
    public Sprite iconSpiky;
    public Sprite iconHerdMentality;
    public Sprite iconHerdLeader;

    [Header("Predator Trait Icons")]
    public Sprite iconPredatorNimble;
    public Sprite iconPredatorStrong;
    public Sprite iconPredatorThickSkinned;
    public Sprite iconVenomous;
    public Sprite iconAmbusher;
    public Sprite iconHerdHunter;
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

    /// <summary>Defers icon build by one frame to allow genetics Awake to complete.</summary>
    private void Start()
    {
        Invoke(nameof(BuildIcons), 0.05f);
    }

    /// <summary>Destroys existing icons and rebuilds the row from the current genome.</summary>
    private void BuildIcons()
    {
        foreach (var go in _icons) if (go != null) Destroy(go);
        _icons.Clear();

        List<Sprite> sprites = GatherSprites();
        if (sprites.Count == 0) return;

        // Shrink icons when the row would exceed maxRowWidth at base size
        float iconSize   = Mathf.Min(baseIconSize, maxRowWidth / sprites.Count);
        float spacing    = iconSize;
        float totalWidth = spacing * (sprites.Count - 1);
        float startX     = -totalWidth * 0.5f;

        for (int i = 0; i < sprites.Count; i++)
        {
            if (sprites[i] == null) continue;

            GameObject icon = new GameObject($"TraitIcon_{i}");
            icon.transform.SetParent(transform);
            icon.transform.localPosition = new Vector3(startX + spacing * i, yOffset, 0f);
            icon.transform.localScale    = Vector3.one * iconSize;
            icon.transform.localRotation = Quaternion.identity;

            SpriteRenderer sr      = icon.AddComponent<SpriteRenderer>();
            sr.sprite              = sprites[i];
            sr.sortingLayerName    = "Default";
            sr.sortingOrder        = sortingOrder;

            _icons.Add(icon);
        }
    }

    /// <summary>Collects the ordered list of sprites for all expressed traits on this entity.</summary>
    /// <returns>List of sprites to display, in trait priority order.</returns>
    private List<Sprite> GatherSprites()
    {
        var list = new List<Sprite>();

        GrazerGenetics gg = GetComponent<GrazerGenetics>();
        if (gg != null && gg.Genome != null)
        {
            // Herd Leader replaces all other grazer icons
            if (gg.IsHerdLeader)
            {
                AddIfNotNull(list, iconHerdLeader);
            }
            else
            {
                if (gg.Genome.IsExpressed(TraitType.GrazerNimble))       AddIfNotNull(list, iconGrazerNimble);
                if (gg.Genome.IsExpressed(TraitType.GrazerStrong))       AddIfNotNull(list, iconGrazerStrong);
                if (gg.Genome.IsExpressed(TraitType.GrazerThickSkinned)) AddIfNotNull(list, iconGrazerThickSkinned);
                if (gg.Genome.IsExpressed(TraitType.Camouflage))         AddIfNotNull(list, iconCamouflage);
                if (gg.Genome.IsExpressed(TraitType.Spiky))              AddIfNotNull(list, iconSpiky);
                if (gg.Genome.IsExpressed(TraitType.HerdMentality))      AddIfNotNull(list, iconHerdMentality);
            }
            return list;
        }

        PredatorGenetics pg = GetComponent<PredatorGenetics>();
        if (pg != null && pg.Genome != null)
        {
            // Apex Predator replaces all other predator icons
            if (pg.IsApexPredator)
            {
                AddIfNotNull(list, iconApexPredator);
            }
            else
            {
                if (pg.Genome.IsExpressed(TraitType.PredatorNimble))       AddIfNotNull(list, iconPredatorNimble);
                if (pg.Genome.IsExpressed(TraitType.PredatorStrong))       AddIfNotNull(list, iconPredatorStrong);
                if (pg.Genome.IsExpressed(TraitType.PredatorThickSkinned)) AddIfNotNull(list, iconPredatorThickSkinned);
                if (pg.Genome.IsExpressed(TraitType.Venomous))             AddIfNotNull(list, iconVenomous);
                if (pg.Genome.IsExpressed(TraitType.Ambusher))             AddIfNotNull(list, iconAmbusher);
                if (pg.Genome.IsExpressed(TraitType.HerdHunter))           AddIfNotNull(list, iconHerdHunter);
            }
            return list;
        }

        PlantGenetics plantG = GetComponent<PlantGenetics>();
        if (plantG != null && plantG.Genome != null)
        {
            // Leaf size uses allele count to select the correct size icon
            Gene leafGene = plantG.Genome.Get(TraitType.LeafSize);
            if (leafGene != null)
            {
                int    leafLevel = (leafGene.AlleleA ? 1 : 0) + (leafGene.AlleleB ? 1 : 0);
                Sprite leafIcon  = leafLevel == 0 ? iconLeafSmall
                                 : leafLevel == 1 ? iconLeafMedium
                                 :                  iconLeafLarge;
                AddIfNotNull(list, leafIcon);
            }

            if (plantG.Genome.IsExpressed(TraitType.Tasty))    AddIfNotNull(list, iconTasty);
            if (plantG.Genome.IsExpressed(TraitType.Bitter))    AddIfNotNull(list, iconBitter);
            if (plantG.Genome.IsExpressed(TraitType.Poisonous)) AddIfNotNull(list, iconPoisonous);
            if (plantG.Genome.IsExpressed(TraitType.Resilient)) AddIfNotNull(list, iconResilient);
        }

        return list;
    }

    /// <summary>Adds a sprite to the list if it is not null.</summary>
    /// <param name="list">Target list to append to.</param>
    /// <param name="s">Sprite to add.</param>
    private static void AddIfNotNull(List<Sprite> list, Sprite s)
    {
        if (s != null) list.Add(s);
    }

    /// <summary>Rebuilds icons after a genome change such as on an inherited offspring.</summary>
    public void Refresh() => BuildIcons();
}
