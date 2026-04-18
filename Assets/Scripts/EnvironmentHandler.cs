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

    /// <summary> 0.0 to 1.0 representing current sun strength (0 at night) </summary>
    public float SunlightIntensity { get; private set; }

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
        int seasonIndex = (int)(totalDaysPassed / seasonLengthInDays) % 4;
        currentSeason = (Season)seasonIndex;
    }

    /// <summary> Updates global light color and intensity </summary>
    private void UpdateLighting()
    {
        if (globalLight == null) return;

        Color baseColor = dayNightGradient.Evaluate(timeOfDay);

        // Apply a subtle seasonal tint (e.g., Summer is warmer, Winter is cooler)
        if (currentSeason == Season.Winter)
            globalLight.color = Color.Lerp(baseColor, winterTint, 0.3f);
        else
            globalLight.color = baseColor;

        // Dim the light slightly in winter or autumn to reflect shorter/weaker days
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
}
