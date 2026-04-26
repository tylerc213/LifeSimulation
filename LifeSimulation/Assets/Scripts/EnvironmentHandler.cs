// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Environment & Resource Management
// Requirement:	Environmental Variation, Seasonal Growth, Lighting
// Author:		Robert Amborski
// Date:		04/17/2026
//
// Description:
//    Controls simulation time, seasonal transitions, and environmental lighting.
//    Provides global environmental data (sunlight, seasons, palettes) used by
//    organisms and world systems to drive behavior and growth.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Central controller for simulation time, seasons, and environmental effects.
/// </summary>
/// <remarks>
/// Acts as a global singleton so all systems (plants, creatures, map) can
/// access consistent environmental data.
/// </remarks>
[DefaultExecutionOrder(-50)]
public class EnvironmentHandler : MonoBehaviour
{
    /// <summary> Global instance for shared environment access </summary>
    public static EnvironmentHandler Instance { get; private set; }

    /// <summary> Defines seasonal cycle states </summary>
    public enum Season { Spring, Summer, Autumn, Winter }

    [Header("Time Settings")]
    public float dayLengthInSeconds = 60f;

    /// <summary> Current time of day (0–1 normalized cycle) </summary>
    [Range(0, 1)] public float timeOfDay;

    /// <summary> Total completed days since simulation start </summary>
    public int totalDaysPassed { get; private set; }

    [Header("Seasonal Settings")]
    public float seasonLengthInDays = 5f;
    public Season currentSeason;

    [Header("Lighting References")]
    public Light2D globalLight;
    public Gradient dayNightGradient;

    /// <summary> Color tint applied during winter for colder visual tone </summary>
    public Color winterTint = new Color(0.8f, 0.9f, 1.0f); // Slight blue tint for winter

    [Header("Seasonal Visual Palettes")]
    public SeasonalPalette springPalette = new SeasonalPalette(new Color(0.2f, 0.8f, 0.2f), new Color(0.2f, 0.5f, 1.0f));
    public SeasonalPalette summerPalette = new SeasonalPalette(new Color(0.82f, 0.71f, 0.55f), new Color(0.1f, 0.2f, 0.5f)); // Classic Tan/Blue
    public SeasonalPalette autumnPalette = new SeasonalPalette(new Color(0.7f, 0.4f, 0.1f), new Color(0.1f, 0.2f, 0.3f));
    public SeasonalPalette winterPalette = new SeasonalPalette(Color.white, new Color(0.7f, 0.9f, 1.0f));

    [Header("Background Art References")]
    /// <summary> Renderer used for background visuals behind simulation </summary>
    public SpriteRenderer backgroundRenderer;

    [Tooltip("The 8-bit AI art sprites for each season.")]
    public Sprite springBackground;
    public Sprite summerBackground;
    public Sprite autumnBackground;
    public Sprite winterBackground;

    // Prevents simulation from running before map is generated
    bool simulationStarted = false;

    /// <summary>
    /// Gets the active palette for the current season.
    /// </summary>
    /// <returns>Seasonal color palette</returns>
    public SeasonalPalette GetCurrentPalette()
    {
        return currentSeason switch
        {
            Season.Spring => springPalette,
            Season.Autumn => autumnPalette,
            Season.Winter => winterPalette,
            _ => summerPalette
        };
    }

    /// <summary> Current sunlight strength (0 = night, 1 = peak daylight) </summary>
    public float SunlightIntensity { get; private set; }

    /// <summary> Visibility scaling factor based on light level </summary>
    public float VisibilityMultiplier => Mathf.Lerp(0.2f, 1.0f, SunlightIntensity);

    private void Awake()
    {
        // Initialize to midday so simulation starts visible and active
        timeOfDay = 0.5f;

        UpdateLighting();

        // Assign singleton instance
        Instance = this;

        // Create fallback gradient if none provided
        if (dayNightGradient == null)
        {
            dayNightGradient = new Gradient();
            dayNightGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.15f, 0.15f, 0.35f), 0f),
                    new GradientColorKey(new Color(1f, 0.92f, 0.75f), 0.5f),
                    new GradientColorKey(new Color(0.12f, 0.12f, 0.3f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                });
        }

        UpdateBackgroundArt();
    }

    private void OnDestroy()
    {
        // Ensure singleton reference is cleared correctly
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        // Prevent environment updates before simulation initialization
        if (!simulationStarted) return;

        UpdateClock();
        UpdateLighting();

        // Debug shortcut to force seasonal transitions
        if (Input.GetKeyDown(KeyCode.N))
        {
            totalDaysPassed += (int)seasonLengthInDays;
            CheckSeasonChange();
            Debug.Log($"Season skipped! Current Season: {currentSeason}");
        }
    }

    /// <summary>
    /// Advances time and calculates sunlight intensity.
    /// </summary>
    private void UpdateClock()
    {
        // Advance normalized day cycle
        timeOfDay += Time.deltaTime / dayLengthInSeconds;

        // Handle day rollover
        if (timeOfDay >= 1)
        {
            timeOfDay = 0;
            totalDaysPassed++;
            CheckSeasonChange();
        }

        // Simulates sun arc using sine wave (dark at night, bright at noon)
        SunlightIntensity = Mathf.Max(0, Mathf.Sin(timeOfDay * Mathf.PI * 2 - Mathf.PI / 2));
    }

    /// <summary>
    /// Updates season based on elapsed days.
    /// </summary>
    private void CheckSeasonChange()
    {
        Season previousSeason = currentSeason;

        int seasonIndex = (int)(totalDaysPassed / seasonLengthInDays) % 4;
        currentSeason = (Season)seasonIndex;

        // Only update visuals if season actually changed
        if (currentSeason != previousSeason)
        {
            if (MapGenerator2D.Instance != null)
            {
                MapGenerator2D.Instance.RefreshTileColors();
            }

            UpdateBackgroundArt();
        }
    }

    /// <summary>
    /// Updates global lighting color and intensity.
    /// </summary>
    private void UpdateLighting()
    {
        if (globalLight == null) return;

        // Base color from time of day gradient
        Color baseColor = dayNightGradient.Evaluate(timeOfDay);

        // Apply seasonal tint for visual variation
        Color seasonalTint = currentSeason switch
        {
            Season.Spring => new Color(0.9f, 1.0f, 0.9f),
            Season.Summer => new Color(1.0f, 1.0f, 0.8f),
            Season.Autumn => new Color(1.0f, 0.85f, 0.7f),
            Season.Winter => winterTint,
            _ => Color.white
        };

        globalLight.color = baseColor * seasonalTint;

        // Adjust brightness based on sunlight + seasonal growth conditions
        float seasonMultiplier = GetSeasonalGrowthMultiplier();
        globalLight.intensity = Mathf.Lerp(0.2f, 1.2f, SunlightIntensity * seasonMultiplier);
    }

    /// <summary>
    /// Provides plant growth modifier based on season.
    /// </summary>
    /// <returns>Growth multiplier</returns>
    public float GetSeasonalGrowthMultiplier()
    {
        return currentSeason switch
        {
            Season.Spring => 1.0f,
            Season.Summer => 1.5f,
            Season.Autumn => 0.7f,
            Season.Winter => 0.2f,
            _ => 1.0f
        };
    }

    /// <summary>
    /// Updates seasonal background visuals.
    /// </summary>
    private void UpdateBackgroundArt()
    {
        if (backgroundRenderer == null) return;

        Sprite targetSprite = currentSeason switch
        {
            Season.Spring => springBackground,
            Season.Autumn => autumnBackground,
            Season.Winter => winterBackground,
            _ => summerBackground
        };

        // Swap sprite only when needed to avoid unnecessary updates
        if (targetSprite != null && backgroundRenderer.sprite != targetSprite)
        {
            backgroundRenderer.sprite = targetSprite;

            StopAllCoroutines();
            StartCoroutine(FadeBackground(0.5f));
        }
    }

    /// <summary>
    /// Smoothly fades in background after seasonal change.
    /// </summary>
    /// <param name="duration">Fade duration in seconds</param>
    private System.Collections.IEnumerator FadeBackground(float duration)
    {
        float elapsed = 0;
        Color c = backgroundRenderer.color;

        // Gradually increase alpha for fade-in effect
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Clamp01(elapsed / duration);
            backgroundRenderer.color = c;
            yield return null;
        }
    }

    /// <summary>
    /// Provides movement modifier for cold-blooded creatures.
    /// </summary>
    /// <returns>Speed multiplier</returns>
    public float GetReptileSpeedMultiplier()
    {
        return currentSeason switch
        {
            Season.Summer => 1.3f,
            Season.Spring => 1.0f,
            Season.Autumn => 0.7f,
            Season.Winter => 0.4f,
            _ => 1.0f
        };
    }

    void OnEnable()
    {
        // Subscribe to map generation event to start simulation
        MapGenerator2D.OnMapGenerated += HandleSimulationStarted;
    }

    void OnDisable()
    {
        // Unsubscribe to prevent dangling references
        MapGenerator2D.OnMapGenerated -= HandleSimulationStarted;
    }

    /// <summary>
    /// Enables simulation updates once map is ready.
    /// </summary>
    void HandleSimulationStarted()
    {
        simulationStarted = true;
    }
}

/// <summary>
/// Defines terrain and water colors for a season.
/// </summary>
[System.Serializable]
public struct SeasonalPalette
{
    public Color landColor;
    public Color waterColor;
    public SeasonalPalette(Color land, Color water) { landColor = land; waterColor = water; }
}
