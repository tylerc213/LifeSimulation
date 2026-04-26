using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Plant — static organism. Grows to full size over time, then can spread seeds.
/// When a Grazer eats it, BeingEaten() is called each bite, shrinking and
/// browning the plant until it disappears.
///
/// Attach to a 2D sprite GameObject. Tag it "Plant".
/// </summary>
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
    [SerializeField] private int biteCount = 4;      // how many bites to fully consume
    [SerializeField] private float biteShakeMag = 0.06f;  // how far the plant jolts per bite
    [SerializeField] private float biteShakeTime = 0.08f;  // seconds the jolt lasts

    // Genetics component — optional, applied at spawn
    private PlantGenetics _genetics;

    public float NutritionValue => nutritionValue * (_genetics != null ? _genetics.NutritionMultiplier : 1f);
    public float EatAttractiveness => _genetics != null ? _genetics.EatAttractiveness : 1f;
    public bool IsPoisonous => _genetics != null && _genetics.IsPoisonous;
    public float PoisonDamagePerSec => _genetics != null ? _genetics.PoisonDamagePerSec : 0f;
    public bool IsFullyGrown => _age >= growthDuration;
    public bool IsBeingConsumed => _bitesRemaining < biteCount;

    private float _age = 0f;
    private float _spreadTimer = 0f;
    private int _bitesRemaining;
    private float _fullScale;          // scale at full growth, set once grown
    private SpriteRenderer _sr;
    private Color _baseColor;
    private Vector3 _originPos;        // position before shake
    private Coroutine _shakeCoroutine;

    private void Awake()
    {
        _genetics = GetComponent<PlantGenetics>();
        _sr = GetComponent<SpriteRenderer>();
        _baseColor = _sr != null ? _sr.color : Color.white;
        _bitesRemaining = biteCount;
        _originPos = transform.localPosition;
    }

    private void Update()
    {
        Grow();
        TrySpread();
        CheckWinterSurvival();
    }

    private void Grow()
    {
        if (EnvironmentHandler.Instance == null) return;

        // 1. Get Environmental data
        float sunPower = EnvironmentHandler.Instance.SunlightIntensity;
        float seasonalGrowth = EnvironmentHandler.Instance.GetSeasonalGrowthMultiplier();

        // 2. The "Buffer": Even in pitch black or winter, give a tiny baseline growth 
        // so plants don't completely stall for 12 hours.
        float baselineGrowth = 0.1f;

        // 3. Boosted Photosynthesis
        // We multiply by 2.0f (or higher) to ensure that during the 50% of the day 
        // when the sun is up, they grow fast enough to finish.
        float growthStep = (sunPower + baselineGrowth) * seasonalGrowth * 2.0f;

        _age = Mathf.Min(_age + (Time.deltaTime * growthStep), growthDuration);
        float t = _age / growthDuration;
        float s = Mathf.Lerp(minScale, maxScale, t);

        // Scale is further reduced by eat progress
        float eatFraction = (float)_bitesRemaining / biteCount;
        transform.localScale = Vector3.one * s * eatFraction;

        if (IsFullyGrown) _fullScale = maxScale;
    }

    private void CheckWinterSurvival()
    {
        if (EnvironmentHandler.Instance == null) return;

        // Only check at the start of the day in Winter
        if (EnvironmentHandler.Instance.currentSeason == EnvironmentHandler.Season.Winter)
        {
            // If the plant is NOT resilient, it has a chance to wither away
            bool resilient = _genetics != null && _genetics.IsResilient;

            if (!resilient)
            {
                // We use a small random chance per frame or a timer so they don't 
                // all vanish at the exact same millisecond.
                if (UnityEngine.Random.value < 0.001f)
                {
                    Consume(); // Instant removal
                }
            }
        }
    }

    private void TrySpread()
    {
        if (!IsFullyGrown || IsBeingConsumed) return;

        _spreadTimer += Time.deltaTime;
        if (_spreadTimer < spreadInterval) return;
        _spreadTimer = 0f;

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

    /// <summary>
    /// Called by a Grazer on each bite. Shrinks, browns, and eventually destroys the plant.
    /// Returns the nutrition for this bite (total divided across bites).
    /// </summary>
    public float BeingEaten()
    {
        if (_bitesRemaining <= 0) return 0f;

        _bitesRemaining--;

        // Shift color toward brown/yellow as the plant is consumed
        float eatFraction = (float)_bitesRemaining / biteCount;
        if (_sr != null)
        {
            Color eaten = new Color(0.55f, 0.35f, 0.1f);   // brown
            _sr.color = Color.Lerp(eaten, _baseColor, eatFraction);
        }

        // Shake the plant to show it's being torn
        if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
        _shakeCoroutine = StartCoroutine(ShakeCoroutine());

        // Last bite — destroy
        if (_bitesRemaining <= 0)
        {
            StartCoroutine(DestroyAfterFrame());
        }

        return NutritionValue / biteCount;
    }

    /// <summary>Instant removal — used when the plant dies for non-eating reasons.</summary>
    public void Consume() => Destroy(gameObject);

    // ── Visual Coroutines ─────────────────────────────────────────────────────

    private IEnumerator ShakeCoroutine()
    {
        Vector3 offset = (Vector3)UnityEngine.Random.insideUnitCircle * biteShakeMag;
        transform.localPosition = _originPos + offset;
        yield return new WaitForSeconds(biteShakeTime);
        if (this != null) transform.localPosition = _originPos;
        _shakeCoroutine = null;
    }

    private IEnumerator DestroyAfterFrame()
    {
        yield return null;    // wait one frame so the last bite's nutrition is returned first
        Destroy(gameObject);
    }
}
