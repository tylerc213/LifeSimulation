using System.IO;
using UnityEngine;

public class SummaryGenerator
{
    public static void GenerateSummary(string filepath)
    {
        string[] lines = File.ReadAllLines(filepath);

        int startPlants = 0, startGrazers = 0, startPredators = 0;
        int endPlants = 0, endGrazers = 0, endPredators = 0;

        int maxPlants = 0, maxGrazers = 0, maxPredators = 0;

        int duration = 0;

        bool firstSnapshot = true;

        foreach (string line in lines)
        {
            LogEntry entry = JsonUtility.FromJson<LogEntry>(line);

            if (entry.entryType == "Snapshot")
            {
                PopSnapshot snapshot = JsonUtility.FromJson<PopSnapshot>(entry.data);

                // First snapshot = starting population
                if (firstSnapshot)
                {
                    startPlants = snapshot.plantCount;
                    startGrazers = snapshot.grazerCount;
                    startPredators = snapshot.predatorCount;
                    firstSnapshot = false;
                }

                // Always update ending values
                endPlants = snapshot.plantCount;
                endGrazers = snapshot.grazerCount;
                endPredators = snapshot.predatorCount;

                // Track max values
                maxPlants = Mathf.Max(maxPlants, snapshot.plantCount);
                maxGrazers = Mathf.Max(maxGrazers, snapshot.grazerCount);
                maxPredators = Mathf.Max(maxPredators, snapshot.predatorCount);

                // Track duration
                duration = snapshot.tick;
            }
        }

        // Print summary (for now)
        Debug.Log("=== Simulation Summary ===");
        Debug.Log("Duration: " + duration + " ticks");

        Debug.Log("Starting Population:");
        Debug.Log("Plants: " + startPlants + ", Grazers: " + startGrazers + ", Predators: " + startPredators);

        Debug.Log("Maximum Population:");
        Debug.Log("Plants: " + maxPlants + ", Grazers: " + maxGrazers + ", Predators: " + maxPredators);

        Debug.Log("Ending Population:");
        Debug.Log("Plants: " + endPlants + ", Grazers: " + endGrazers + ", Predators: " + endPredators);
    }
}
