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
    public static float PlantPrimary { get; private set; } = 1f;
    public static float PlantSecondary { get; private set; } = 1f;
    public static float PlantDefense { get; private set; } = 1f;

    public static float GrazerStat { get; private set; } = 1f;
    public static float GrazerRare { get; private set; } = 1f;
    public static float GrazerPack { get; private set; } = 1f;

    public static float PredatorStat { get; private set; } = 1f;
    public static float PredatorRare { get; private set; } = 1f;
    public static float PredatorApex { get; private set; } = 1f;

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
}
