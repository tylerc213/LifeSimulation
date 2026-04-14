// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Score Summary Data
// Requirement:	Leaderboard
// Author:		Benjamin Jones
// Date:		04/14/2026
// Version:		0.0.0
//
// Description:
//    Leaderboard board IDs and display labels for the five categories; static
//    ScoreSummaryPayload handoff from Simulation (summary generation) to the
//    ScoreSummary scene before Nakama submission.
// -----------------------------------------------------------------------------

using System;
using UnityEngine;

/// <summary> Defines leaderboard categories and board ids. </summary>
public static class LeaderboardBoards
{
    public const string OverallScore = "overall_score";
    public const string LongestSurvivalTime = "longest_survival_time";
    public const string HighestStability = "highest_stability";
    public const string HighestDiversity = "highest_diversity";
    public const string HighestPopulationPeak = "highest_population_peak";

    public static readonly string[] DisplayNames =
    {
        "Overall Score",
        "Longest Survival Time",
        "Highest Stability",
        "Highest Diversity",
        "Highest Population Peak"
    };

    public static readonly string[] BoardIds =
    {
        OverallScore,
        LongestSurvivalTime,
        HighestStability,
        HighestDiversity,
        HighestPopulationPeak
    };
}

/// <summary> Stores numeric values for the five leaderboard categories. </summary>
[Serializable]
public class ScoreSummaryPayload
{
    public long overallScore;
    public long longestSurvivalTime;
    public long highestStability;
    public long highestDiversity;
    public long highestPopulationPeak;

    public long[] ToOrderedArray()
    {
        return new[]
        {
            overallScore,
            longestSurvivalTime,
            highestStability,
            highestDiversity,
            highestPopulationPeak
        };
    }
}

/// <summary> Runtime container that carries summary values across scene loads. </summary>
public static class ScoreSummaryData
{
    public static ScoreSummaryPayload CurrentRun = new ScoreSummaryPayload();

    public static void SetCurrentRun(ScoreSummaryPayload payload)
    {
        CurrentRun = payload ?? new ScoreSummaryPayload();
    }
}
