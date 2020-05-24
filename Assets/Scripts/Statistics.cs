using System.Collections;
using System.Collections.Generic;

// Statistics serialized datas
[System.Serializable]
public class Statistics
{
    // Per difficulty
    public List<LevelStatistics> aDifficultyStats;

    // Global stats
    public float fTotalPlayTime;
    public int totalGeneratedGrids;
    public int totalResolvedGrids;
    public int totalUsedHint;

    public Statistics()
    {
        aDifficultyStats = new List<LevelStatistics>(4);
        aDifficultyStats.Add(new LevelStatistics(EDifficulty.veryeasy));
        aDifficultyStats.Add(new LevelStatistics(EDifficulty.easy));
        aDifficultyStats.Add(new LevelStatistics(EDifficulty.medium));
        aDifficultyStats.Add(new LevelStatistics(EDifficulty.hard));
    }

    public string GetAsString()
    {
        string str = "Global";

        str += "\nTotal played time: " + fTotalPlayTime;
        str += "\nTotal Generated grids: " + totalGeneratedGrids;
        str += "\nTotal resolved grids: " + totalResolvedGrids;
        str += "\nTotal used hints: " + totalUsedHint;

        for (int i = 0; i < aDifficultyStats.Count; ++i)
        {
            str += "\n\n" + aDifficultyStats[i].GetAsString();
        }

        return str;
    }
}

// Stats for each level
[System.Serializable]
public class LevelStatistics
{
    public EDifficulty eDifficulty;

    public int resolvedGrids;
    public int generatedGrids;
    public float fPlayedTime; // store time in seconds
    public float fRecordTime; // Time of quickest grid resolved
    public int usedHints;

    public LevelStatistics(EDifficulty eDiff)
    {
        eDifficulty = eDiff;
    }

    public string GetAsString()
    {
        string str = "";

        switch(eDifficulty)
        {
            case EDifficulty.veryeasy: str = "Very Easy";break;
            case EDifficulty.easy: str = "Easy";break;
            case EDifficulty.medium: str = "Medium";break;
            case EDifficulty.hard: str = "Hard";break;
        }

        str += "\nTotal played time: " + fPlayedTime;
        str += "\nRecord: " + fRecordTime;
        str += "\nTotal Generated grids: " + generatedGrids;
        str += "\nTotal resolved grids: " + resolvedGrids;
        str += "\nTotal used hints: " + usedHints;

        return str;
    }
}