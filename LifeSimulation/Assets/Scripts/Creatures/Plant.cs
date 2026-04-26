// -----------------------------------------------------------------------------
// Project:     EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:        Plant Subtypes
// Requirement: Lifeform Simulation
// Author:      Luke Kivett
// Date:        4/6/2026
// Version:     0.0.1
//
// Description:
//    Static organism that grows over time, spreads seeds when mature, and is
//    consumed bite-by-bite by grazers with shrink and shake visual feedback.
//    Max scale is determined by the LeafSize genetic trait at spawn time.
// -----------------------------------------------------------------------------
using System;
using System.Collections;
using UnityEngine;

/// <summary>Manages plant growth, seed spreading, and incremental consumption.</summary>
/// <remarks>
/// Tag this GameObject "Plant". PlantGenetics is optional; without it the plant
/// uses base nutrition values and default attractiveness.
/// Max scale is overridden by the LeafSize gene: small = 0.7x, medium = 1.0x, large = 1.4x.
/// </remarks>
public class Plant : MonoBehaviour
{
    [Header("Growth")]
    [SerializeField] private float growthDuration = 5f;
    [SerializeField] private float maxScale = 1f;
    [SerializeField] private float minScale = 0.2f;

    [Header("Spreading")]
    [SerializeField] private float spreadInterval = 10f;
    [SerializeField] private float spreadRadius = 3f;
    [SerializeField] private int maxNearbyPlants = 5;
    [SerializeField] private float crowdCheckRadius = 2f;

    [Header("Nutrition")]
    [SerializeField] private float nutritionValue = 40f;

    [Header("Eat Effect")]
    // Total bites before the plant is fully consumed
    [SerializeField] private int biteCount = 4;
    // Lateral jolt distance per bite in world units
    [SerializeField] private float biteShakeMag = 0.06f;
    // Duration of each bite jolt in seconds
    [SerializeField] private float biteShakeTime = 0.08f;

    // Genetics component — optional, applied at spawn
    private PlantGenetics _genetics;

    // Effective max scale after applying the LeafSize gene multiplier
    private float _effectiveMaxScale;

    /// <summary>Nutrition per full consumption, scaled by leaf-size genetics.</summary>
    public float NutritionValue => nutritionValue * (_genetics != null ? _genetics.NutritionMultiplier : 1f);

    /// <summary>Likelihood a grazer will choose to eat this plant; affected by taste traits.</summary>
    public float EatAttractiveness => _genetics != null ? _genetics.EatAttractiveness : 1f;

    /// <summary>True if this plant carries the Poisonous gene.</summary>
    public bool IsPoisonous => _genetics != null && _genetics.IsPoisonous;

    /// <summary>Damage per second applied to a grazer that eats a poisonous plant.</summary>
    public float PoisonDamagePerSec => _genetics != null ? _genetics.PoisonDamagePerSec : 0f;

    /// <summary>True once the plant has finished its growth animation.</summary>
    public bool IsFullyGrown => _age >= growthDuration;

    /// <summary>True once at least one bite has been taken.</summary>
    public bool IsBeingConsumed => _bitesRemaining < biteCount;

    private float _age = 0f;
    private float _spreadTimer = 0f;
    private int _bitesRemaining;
    private float _fullScale;
    private SpriteRenderer _sr;
    private Color _baseColor;
    private Vector3 _originPos;
    private Coroutine _shakeCoroutine;

    /// <summary>Caches components and initialises bite count and origin position.</summary>
    private void Awake()
    {
        _genetics = GetComponent<PlantGenetics>();
        _sr = GetComponent<SpriteRenderer>();
        _baseColor = _sr != null ? _sr.color : Color.white;
        _bitesRemaining = biteCount;
        _originPos = transform.localPosition;

        // Default to base max scale — will be corrected in Start once genetics have run
        _effectiveMaxScale = maxScale;
    }

    /// <summary>Calculates effective max scale after PlantGenetics.Awake has set the genome.</summary>
    private void Start()
    {
        // PlantGenetics.Awake is guaranteed to have run by now, so the genome is available
        _effectiveMaxScale = maxScale * GetLeafSizeScaleMultiplier();
    }

    /// <summary>Recalculates effective max scale when a genome is applied after Start.</summary>
    public void RecalculateMaxScale()
    {
        _effectiveMaxScale = maxScale * GetLeafSizeScaleMultiplier();
    }

    /// <summary>Advances growth and attempts seed spreading each frame.</summary>
    private void Update()
    {
        Grow();
        TrySpread();
        CheckWinterSurvival();
    }

    /// <summary>Scales the plant toward its effective max size, reduced by eat progress.</summary>
    private void Grow()
    {
        if (EnvironmentHandler.Instance == null) return;

        float sunPower = EnvironmentHandler.Instance.SunlightIntensity;
        float seasonalGrowth = EnvironmentHandler.Instance.GetSeasonalGrowthMultiplier();

        // Small baseline ensures plants don't stall completely during night or winter
        float baselineGrowth = 0.1f;

        // Multiply by 2 so plants finish growing during the daylight portion of the cycle
        float growthStep = (sunPower + baselineGrowth) * seasonalGrowth * 2.0f;

        _age = Mathf.Min(_age + (Time.deltaTime * growthStep), growthDuration);
        float t = _age / growthDuration;

        // Use effective max scale (leaf-size adjusted) instead of raw maxScale
        float s = Mathf.Lerp(minScale, _effectiveMaxScale, t);

        // Further reduce scale as the plant is eaten bite-by-bite
        float eatFraction = (float)_bitesRemaining / biteCount;
        transform.localScale = Vector3.one * s * eatFraction;

        if (IsFullyGrown) _fullScale = _effectiveMaxScale;
    }

    /// <summary>Non-resilient plants have a small per-frame chance to die in winter.</summary>
    private void CheckWinterSurvival()
    {
        if (EnvironmentHandler.Instance == null) return;

        if (EnvironmentHandler.Instance.currentSeason == EnvironmentHandler.Season.Winter)
        {
            bool resilient = _genetics != null && _genetics.IsResilient;
            if (!resilient && UnityEngine.Random.value < 0.001f)
                Consume();
        }
    }

    /// <summary>Attempts to spawn a seed nearby when mature, subject to crowding limits.</summary>
    private void TrySpread()
    {
        if (!IsFullyGrown || IsBeingConsumed) return;

        _spreadTimer += Time.deltaTime;
        if (_spreadTimer < spreadInterval) return;
        _spreadTimer = 0f;

        // Count nearby plants to suppress spreading in dense areas
        Collider2D[] nearby = Physics2D.OverlapCircleAll(
            transform.position, crowdCheckRadius,
            LayerMask.GetMask("Default"));

        int plantCount = 0;
        foreach (var col in nearby)
            if (col.CompareTag("Plant")) plantCount++;

        if (plantCount >= maxNearbyPlants) return;

        Vector2 offset = UnityEngine.Random.insideUnitCircle * spreadRadius;
        Vector2 seedPos = (Vector2)transform.position + offset;

        if (BoundaryManager.Instance != null)
            seedPos = BoundaryManager.Instance.Clamp(seedPos);

        EcosystemManager.Instance?.SpawnPlant(seedPos);
    }

    /// <summary>Processes one bite, updates visuals, and destroys the plant on the final bite.</summary>
    /// <returns>Nutrition value for this single bite.</returns>
    public float BeingEaten()
    {
        if (_bitesRemaining <= 0) return 0f;

        _bitesRemaining--;

        // Lerp sprite toward brown as more bites are taken
        float eatFraction = (float)_bitesRemaining / biteCount;
        if (_sr != null)
        {
            Color eaten = new Color(0.55f, 0.35f, 0.1f);
            _sr.color = Color.Lerp(eaten, _baseColor, eatFraction);
        }

        // Trigger a shake jolt to show the plant being torn
        if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
        _shakeCoroutine = StartCoroutine(ShakeCoroutine());

        if (_bitesRemaining <= 0)
        {
            SimulationLogger.Instance.LogInteraction("Predation", "Grazer", "Plant");
            StartCoroutine(DestroyAfterFrame());
        }

        return NutritionValue / biteCount;
    }

    /// <summary>Immediately destroys the plant without bite-by-bite feedback.</summary>
    public void Consume() => Destroy(gameObject);

    /// <summary>Jolts the plant laterally for one frame to simulate being torn.</summary>
    private IEnumerator ShakeCoroutine()
    {
        Vector3 offset = (Vector3)UnityEngine.Random.insideUnitCircle * biteShakeMag;
        transform.localPosition = _originPos + offset;
        yield return new WaitForSeconds(biteShakeTime);
        if (this != null) transform.localPosition = _originPos;
        _shakeCoroutine = null;
    }

    /// <summary>Waits one frame before destroying so the final bite's nutrition is returned first.</summary>
    private IEnumerator DestroyAfterFrame()
    {
        yield return null;
        Destroy(gameObject);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a scale multiplier based on the expressed LeafSize allele count.
    /// aa (small) = 0.7, Aa (medium) = 1.0, AA (large) = 1.4.
    /// Falls back to 1.0 if no genetics component is present.
    /// </summary>
    /// <returns>Scale multiplier for this plant's leaf size.</returns>
    private float GetLeafSizeScaleMultiplier()
    {
        if (_genetics == null || _genetics.Genome == null) return 1f;

        Gene leafGene = _genetics.Genome.Get(TraitType.LeafSize);
        if (leafGene == null) return 1f;

        int leafLevel = (leafGene.AlleleA ? 1 : 0) + (leafGene.AlleleB ? 1 : 0);
        return leafLevel switch
        {
            0 => 0.7f,   // small — less visible, lower nutrition
            1 => 1.0f,   // medium — baseline
            2 => 1.4f,   // large — very visible, high nutrition
            _ => 1.0f,
        };
    }
}
