using System;
using UnityEngine;

/// <summary>
/// Plant — static organism. Grows to full size over time, then can spread seeds.
/// When a Grazer overlaps it, the Grazer calls Consume() which destroys the plant.
///
/// Attach to a 2D sprite GameObject.  Tag it "Plant".
/// </summary>
public class Plant : MonoBehaviour
{
    [Header("Growth")]
    [SerializeField] private float growthDuration = 5f;     // seconds to reach full size
    [SerializeField] private float maxScale = 1f;
    [SerializeField] private float minScale = 0.2f;

    [Header("Spreading")]
    [SerializeField] private float spreadInterval = 10f;   // seconds between seed attempts
    [SerializeField] private float spreadRadius = 3f;    // how far seeds land
    [SerializeField] private int maxNearbyPlants = 5;     // suppress spreading if crowded
    [SerializeField] private float crowdCheckRadius = 2f;

    [Header("Nutrition")]
    [SerializeField] private float nutritionValue = 40f;    // hunger restored when eaten

    // Genetics component — optional, applied at spawn
    private PlantGenetics _genetics;

    public float NutritionValue => nutritionValue * (_genetics != null ? _genetics.NutritionMultiplier : 1f);
    public float EatAttractiveness => _genetics != null ? _genetics.EatAttractiveness : 1f;
    public bool IsPoisonous => _genetics != null && _genetics.IsPoisonous;
    public float PoisonDamagePerSec => _genetics != null ? _genetics.PoisonDamagePerSec : 0f;
    public bool IsFullyGrown => _age >= growthDuration;

    private float _age = 0f;
    private float _spreadTimer = 0f;

    private void Awake()
    {
        _genetics = GetComponent<PlantGenetics>();
    }

    private void Update()
    {
        Grow();
        TrySpread();
    }

    private void Grow()
    {
        _age = Mathf.Min(_age + Time.deltaTime, growthDuration);
        float t = _age / growthDuration;
        float s = Mathf.Lerp(minScale, maxScale, t);
        transform.localScale = Vector3.one * s;
    }

    private void TrySpread()
    {
        if (!IsFullyGrown) return;

        _spreadTimer += Time.deltaTime;
        if (_spreadTimer < spreadInterval) return;
        _spreadTimer = 0f;

        // Count nearby plants to avoid overcrowding
        Collider2D[] nearby = Physics2D.OverlapCircleAll(
            transform.position, crowdCheckRadius,
            LayerMask.GetMask("Default"));   // adjust layer if needed

        int plantCount = 0;
        foreach (var col in nearby)
            if (col.CompareTag("Plant")) plantCount++;

        if (plantCount >= maxNearbyPlants) return;

        // Spawn a seed nearby
        Vector2 offset = UnityEngine.Random.insideUnitCircle * spreadRadius;
        Vector2 seedPos = (Vector2)transform.position + offset;

        // Clamp within camera bounds
        if (BoundaryManager.Instance != null)
            seedPos = BoundaryManager.Instance.Clamp(seedPos);

        // Ask EcosystemManager to spawn (respects population caps)
        EcosystemManager.Instance?.SpawnPlant(seedPos);
    }

    /// <summary>Called by a Grazer when it eats this plant.</summary>
    public void Consume()
    {
        Destroy(gameObject);
    }
}