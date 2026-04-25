// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Environment & Resource Management
// Requirement:	Environmental Variation, Seasonal Growth, Lighting
// Author:		Robert Amborski
// Date:		04/17/2026
// Version:		0.0.1
//
// Description:
//    Manages the global simulation clock, seasonal cycles, and lighting.
//    Provides sunlight intensity data to the Ecosystem for plant growth logic.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary> Manages global environmental states including time and seasons </summary>
[DefaultExecutionOrder(-50)]
public class EnvironmentHandler : MonoBehaviour
{
    public static EnvironmentHandler Instance { get; private set; }

    public enum Season { Spring, Summer, Autumn, Winter }

    [Header("Time Settings")]
    public float dayLengthInSeconds = 60f;
    [Range(0, 1)] public float timeOfDay;
    public int totalDaysPassed { get; private set; }

    [Header("Seasonal Settings")]
    public float seasonLengthInDays = 5f;
    public Season currentSeason;

    [Header("Lighting References")]
    public Light2D globalLight;
    public Gradient dayNightGradient;
    public Color winterTint = new Color(0.8f, 0.9f, 1.0f); // Slight blue tint for winter

    [Header("Seasonal Visual Palettes")]
    public SeasonalPalette springPalette = new SeasonalPalette(new Color(0.2f, 0.8f, 0.2f), new Color(0.2f, 0.5f, 1.0f));
    public SeasonalPalette summerPalette = new SeasonalPalette(new Color(0.82f, 0.71f, 0.55f), new Color(0.1f, 0.2f, 0.5f)); // Classic Tan/Blue
    public SeasonalPalette autumnPalette = new SeasonalPalette(new Color(0.7f, 0.4f, 0.1f), new Color(0.1f, 0.2f, 0.3f));
    public SeasonalPalette winterPalette = new SeasonalPalette(Color.white, new Color(0.7f, 0.9f, 1.0f));

    [Header("Background Art References")]
    [Tooltip("The UI Image component fanned out behind the map.")]
    public UnityEngine.UI.Image backgroundDisplay;

    [Tooltip("The 8-bit AI art sprites for each season.")]
    public Sprite springBackground;
    public Sprite summerBackground;
    public Sprite autumnBackground;
    public Sprite winterBackground;

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

    /// <summary> 0.0 to 1.0 representing current sun strength (0 at night) </summary>
    public float SunlightIntensity { get; private set; }

    /// <summary> Returns a multiplier for animal vision based on sunlight. 1.0 during day, 0.2 during peak night. </summary>
    public float VisibilityMultiplier => Mathf.Lerp(0.2f, 1.0f, SunlightIntensity);

    private void Awake()
    {
        Instance = this;
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

        // Initialize the background art to the starting season
        UpdateBackgroundArt();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        UpdateClock();
        UpdateLighting();

        // DEBUG: Press 'N' to skip to the next season
        if (Input.GetKeyDown(KeyCode.N))
        {
            totalDaysPassed += (int)seasonLengthInDays;
            CheckSeasonChange();
            Debug.Log($"Season skipped! Current Season: {currentSeason}");
        }
    }

    /// <summary> Increments time and handles day/season rollovers </summary>
    private void UpdateClock()
    {
        timeOfDay += Time.deltaTime / dayLengthInSeconds;

        if (timeOfDay >= 1)
        {
            timeOfDay = 0;
            totalDaysPassed++;
            CheckSeasonChange();
        }

        // Sunlight follows a bell curve: Max at 0.5 (Noon), 0 at 0 (Midnight)
        // Using Mathf.Max to ensure intensity stays at 0 during "Night" hours
        SunlightIntensity = Mathf.Max(0, Mathf.Sin(timeOfDay * Mathf.PI * 2 - Mathf.PI / 2));
    }

    /// <summary> Transitions seasons based on day count </summary>
    private void CheckSeasonChange()
    {
        Season previousSeason = currentSeason;
        int seasonIndex = (int)(totalDaysPassed / seasonLengthInDays) % 4;
        currentSeason = (Season)seasonIndex;

        // If the season actually changed, tell the visual systems to update
        if (currentSeason != previousSeason)
        {
            // 1. Update the Tilemap Colors (Existing Logic)
            if (MapGenerator2D.Instance != null)
            {
                MapGenerator2D.Instance.RefreshTileColors();
            }

            // 2. Update the UI Background Art (New Logic)
            UpdateBackgroundArt();
        }
    }

    /// <summary> Updates global light color and intensity </summary>
    private void UpdateLighting()
    {
        if (globalLight == null) return;

        Color baseColor = dayNightGradient.Evaluate(timeOfDay);

        // Dynamic seasonal lighting tints
        Color seasonalTint = currentSeason switch
        {
            Season.Spring => new Color(0.9f, 1.0f, 0.9f), // Fresh/Bright
            Season.Summer => new Color(1.0f, 1.0f, 0.8f), // Warm/Golden
            Season.Autumn => new Color(1.0f, 0.85f, 0.7f), // Sepia/Orange
            Season.Winter => winterTint,                   // Original blue tint
            _ => Color.white
        };

        globalLight.color = baseColor * seasonalTint;

        float seasonMultiplier = GetSeasonalGrowthMultiplier();
        globalLight.intensity = Mathf.Lerp(0.2f, 1.2f, SunlightIntensity * seasonMultiplier);
    }

    /// <summary> 
    /// Returns a growth coefficient based on the season. 
    /// Used by Plants to calculate energy production.
    /// </summary>
    public float GetSeasonalGrowthMultiplier()
    {
        return currentSeason switch
        {
            Season.Spring => 1.0f,
            Season.Summer => 1.5f, // Peak growth
            Season.Autumn => 0.7f, // Dying off
            Season.Winter => 0.2f, // Dormancy
            _ => 1.0f
        };
    }

    private void UpdateBackgroundArt()
    {
        // Safety check in case you forgot to drag the Image component in
        if (backgroundDisplay == null) return;

        // Pick the sprite based on the current season
        Sprite targetSprite = currentSeason switch
        {
            Season.Spring => springBackground,
            Season.Autumn => autumnBackground,
            Season.Winter => winterBackground,
            _ => summerBackground // Summer is the default
        };

        // Swap the sprite on the UI component
        if (targetSprite != null && backgroundDisplay.sprite != targetSprite)
        {
            backgroundDisplay.sprite = targetSprite;

            // Optional: Trigger a simple fade-in effect
            StopAllCoroutines();
            StartCoroutine(FadeBackground(0.5f));
        }
    }

    private System.Collections.IEnumerator FadeBackground(float duration)
    {
        float elapsed = 0;
        Color c = backgroundDisplay.color;

        // Start transparent and fade in
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Clamp01(elapsed / duration);
            backgroundDisplay.color = c;
            yield return null;
        }
    }

    /// <summary> Returns a speed multiplier for Cold-Blooded (Reptile) creatures </summary>
    public float GetReptileSpeedMultiplier()
    {
        return currentSeason switch
        {
            Season.Summer => 1.3f, // Fast in heat
            Season.Spring => 1.0f, // Normal
            Season.Autumn => 0.7f, // Slowing down
            Season.Winter => 0.4f, // Lethargic/Hibernation mode
            _ => 1.0f
        };
    }
}

[System.Serializable]
public struct SeasonalPalette
{
    public Color landColor;
    public Color waterColor;
    public SeasonalPalette(Color land, Color water) { landColor = land; waterColor = water; }
}
