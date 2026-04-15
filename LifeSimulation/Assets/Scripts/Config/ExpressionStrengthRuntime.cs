// -----------------------------------------------------------------------------
// Runtime expression strength multipliers (updated by SimulationSettingsStore).
// Genetics read these when applying traits.
// -----------------------------------------------------------------------------

using UnityEngine;

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
            PlantPrimary = Mathf.Clamp(s.plant.expression.primaryStats, 0f, 3f);
            PlantSecondary = Mathf.Clamp(s.plant.expression.secondaryTraits, 0f, 3f);
            PlantDefense = Mathf.Clamp(s.plant.expression.defenseTraits, 0f, 3f);
        }

        if (s?.grazer?.expression != null)
        {
            GrazerStat = Mathf.Clamp(s.grazer.expression.statTraits, 0f, 3f);
            GrazerRare = Mathf.Clamp(s.grazer.expression.rareTraits, 0f, 3f);
            GrazerPack = Mathf.Clamp(s.grazer.expression.packTraits, 0f, 3f);
        }

        if (s?.predator?.expression != null)
        {
            PredatorStat = Mathf.Clamp(s.predator.expression.statTraits, 0f, 3f);
            PredatorRare = Mathf.Clamp(s.predator.expression.rareTraits, 0f, 3f);
            PredatorApex = Mathf.Clamp(s.predator.expression.apexTraits, 0f, 3f);
        }
    }
}
