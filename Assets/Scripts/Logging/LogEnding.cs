using UnityEngine;

public class LogEnding : MonoBehaviour
{
    public LogManager logManager;
    public SimulationLogger simulationLogger;

    /// <summary>
    /// Ends the simulation, logs the final snapshot, and generates a summary report.
    /// </summary>
    public void EndSimulation()
    {
        // Ensure final population state is captured
        logManager.LogFinalSnapshot();

        // Generate summary from the log file
        SummaryGenerator.GenerateSummary(simulationLogger.filepath);

        // (Later) Load summary scene here
        Debug.Log("Simulation Ended - Summary Generated");
    }

    // Update is called once per frame
    void Update()
    {
        // Press SPACE to end simulation manually
        if (Input.GetKeyDown(KeyCode.Space))
        {
            EndSimulation();
        }

    }
}
