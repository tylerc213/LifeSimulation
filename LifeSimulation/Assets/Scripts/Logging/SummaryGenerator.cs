using System.IO;
using UnityEngine;

public class SummaryGenerator
{
    public static ScoreSummaryPayload GenerateSummaryPayload(string filepath)
    {
        if (string.IsNullOrEmpty(filepath) || !File.Exists(filepath))
        {
            return new ScoreSummaryPayload();
        }

        string[] lines = File.ReadAllLines(filepath);

        int startPlants = 0, startGrazers = 0, startPredators = 0;
        int endPlants = 0, endGrazers = 0, endPredators = 0;
        int maxPlants = 0, maxGrazers = 0, maxPredators = 0;
        int duration = 0;

        bool firstSnapshot = true;
        int stableTicks = 0;
        int lastTotal = -1;

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            LogEntry entry = JsonUtility.FromJson<LogEntry>(line);
            if (entry == null || entry.entryType != "Snapshot")
            {
                continue;
            }

            PopSnapshot snapshot = JsonUtility.FromJson<PopSnapshot>(entry.data);
            if (snapshot == null)
            {
                continue;
            }

            if (firstSnapshot)
            {
                startPlants = snapshot.plantCount;
                startGrazers = snapshot.grazerCount;
                startPredators = snapshot.predatorCount;
                firstSnapshot = false;
            }

            endPlants = snapshot.plantCount;
            endGrazers = snapshot.grazerCount;
            endPredators = snapshot.predatorCount;

            maxPlants = Mathf.Max(maxPlants, snapshot.plantCount);
            maxGrazers = Mathf.Max(maxGrazers, snapshot.grazerCount);
            maxPredators = Mathf.Max(maxPredators, snapshot.predatorCount);
            duration = snapshot.tick;

            if (lastTotal >= 0 && Mathf.Abs(snapshot.totalPop - lastTotal) <= 5)
            {
                stableTicks++;
            }
            lastTotal = snapshot.totalPop;
        }

        int peakPopulation = maxPlants + maxGrazers + maxPredators;
        int diversity = 0;
        diversity += endPlants > 0 ? 1 : 0;
        diversity += endGrazers > 0 ? 1 : 0;
        diversity += endPredators > 0 ? 1 : 0;

        // Minimal placeholder formula for current milestone.
        long overall = duration + peakPopulation + (stableTicks * 2) + (diversity * 100);

        return new ScoreSummaryPayload
        {
            overallScore = overall,
            longestSurvivalTime = duration,
            highestStability = stableTicks,
            highestDiversity = diversity,
            highestPopulationPeak = peakPopulation
        };
    }

    public static void GenerateSummary(string filepath)
    {
        ScoreSummaryPayload payload = GenerateSummaryPayload(filepath);

        // Print summary (for now)
        Debug.Log("=== Simulation Summary ===");
        Debug.Log("Overall Score: " + payload.overallScore);
        Debug.Log("Longest Survival Time: " + payload.longestSurvivalTime);
        Debug.Log("Highest Stability: " + payload.highestStability);
        Debug.Log("Highest Diversity: " + payload.highestDiversity);
        Debug.Log("Highest Population Peak: " + payload.highestPopulationPeak);
    }
}
