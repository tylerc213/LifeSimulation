// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Expression strength runtime
// Requirement:	Configuration
// Author:		Benjamin Jones
// Date:		04/14/2026
// Version:		0.0.0
//
// Description:
//    Publishes simple global knobs for how strongly traits express, derived from
//    settings so genetics can stay lightweight while still honoring designer tuning.
// -----------------------------------------------------------------------------

using UnityEngine;

/// <summary> Global expression multipliers; values follow <see cref="SimulationSettingsValidator"/>. </summary>
public static class ExpressionStrengthRuntime
{
    /// <summary> Default slider value in <see cref="SimulationSettings"/> — used to map runtime knobs to trait strength (1.5 ≈ original designed balance). </summary>
    public const float BaselineSliderValue = 1.5f;

    /// <summary> Default matches <see cref="SimulationSettings"/> so trait strength matches authored balance before/without the settings store. </summary>
    public static float PlantPrimary { get; private set; } = BaselineSliderValue;
    public static float PlantSecondary { get; private set; } = BaselineSliderValue;
    public static float PlantDefense { get; private set; } = BaselineSliderValue;

    public static float GrazerStat { get; private set; } = BaselineSliderValue;
    public static float GrazerRare { get; private set; } = BaselineSliderValue;
    public static float GrazerPack { get; private set; } = BaselineSliderValue;

    public static float PredatorStat { get; private set; } = BaselineSliderValue;
    public static float PredatorRare { get; private set; } = BaselineSliderValue;
    public static float PredatorApex { get; private set; } = BaselineSliderValue;

    public static void ApplyFrom(SimulationSettings s)
    {
        if (s?.plant?.expression != null)
        {
            PlantPrimary = s.plant.expression.primaryStats;
            PlantSecondary = s.plant.expression.secondaryTraits;
            PlantDefense = s.plant.expression.defenseTraits;
        }

        if (s?.grazer?.expression != null)
        {
            GrazerStat = s.grazer.expression.statTraits;
            GrazerRare = s.grazer.expression.rareTraits;
            GrazerPack = s.grazer.expression.packTraits;
        }

        if (s?.predator?.expression != null)
        {
            PredatorStat = s.predator.expression.statTraits;
            PredatorRare = s.predator.expression.rareTraits;
            PredatorApex = s.predator.expression.apexTraits;
        }
    }

    /// <summary>
    /// Maps a config slider (typically 0–3, default <see cref="BaselineSliderValue"/>) to a 0+ strength scalar.
    /// At the baseline value, returns 1 so trait bonuses match the original authored magnitudes.
    /// </summary>
    public static float NormalizedStrength(float runtimeMultiplier)
    {
        if (runtimeMultiplier <= 0f) return 0f;
        return runtimeMultiplier / BaselineSliderValue;
    }
}
