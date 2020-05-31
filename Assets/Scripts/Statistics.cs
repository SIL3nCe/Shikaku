using System.Collections;
using System.Collections.Generic;

// Statistics serialized datas
[System.Serializable]
public class Statistics
{
    // Per difficulty
    private List<LevelStatistics> aDifficultyStats;

    // Global stats
    public long totalPlayTime; // In seconds
    public int totalGeneratedGrids;
    public int totalResolvedGrids;
    public int totalCreatedAreas;
    public int totalUsedHint;

    public Statistics()
    {
        aDifficultyStats = new List<LevelStatistics>(4);
        aDifficultyStats.Add(new LevelStatistics(EDifficulty.veryeasy));
        aDifficultyStats.Add(new LevelStatistics(EDifficulty.easy));
        aDifficultyStats.Add(new LevelStatistics(EDifficulty.medium));
        aDifficultyStats.Add(new LevelStatistics(EDifficulty.hard));
    }

    public void AddGeneratedGrid(EDifficulty eDiff)
    {
        aDifficultyStats[(int)eDiff].generatedGrids++;
        totalGeneratedGrids++;
        SaveManager.SaveStats();
    }
    public void AddCreatedArea(EDifficulty eDiff)
    {
        aDifficultyStats[(int)eDiff].createdAreas++;
        totalCreatedAreas++;
        SaveManager.SaveStats();
    }
    public void AddUsedHint(EDifficulty eDiff)
    {
        aDifficultyStats[(int)eDiff].usedHints++;
        totalUsedHint++;
        SaveManager.SaveStats();
    }
    public void AddFinishedGrid(EDifficulty eDiff, int time, bool bResolved)
    {
        aDifficultyStats[(int)eDiff].playedTime += time;
        totalPlayTime += time;

        if (bResolved)
        {
            if (aDifficultyStats[(int)eDiff].recordTime > time
                || aDifficultyStats[(int)eDiff].recordTime == -1)
            {
                aDifficultyStats[(int)eDiff].recordTime = time;
            }

            aDifficultyStats[(int)eDiff].resolvedGrids++;
            totalResolvedGrids++;
        }

        SaveManager.SaveStats();
    }

    public string GetAsString()
    {
        string str = "Global";

        str += "\nTotal played time: " + FormatFloatToTime(totalPlayTime);
        str += "\nTotal Generated grids: " + totalGeneratedGrids;
        str += "\nTotal resolved grids: " + totalResolvedGrids;
        str += "\nTotal created areas: " + totalCreatedAreas;
        str += "\nTotal used hints: " + totalUsedHint;

        for (int i = 0; i < aDifficultyStats.Count; ++i)
        {
            str += "\n\n" + aDifficultyStats[i].GetAsString();
        }

        return str;
    }
    public static string FormatFloatToTime(long time)
    {
        if (time == -1)
            return "-";

        long hours = time / 3600;
        time %= 3600;
        long minutes = time / 60;
        time %= 60;

        return hours.ToString("00") + ":" + minutes.ToString("00") + ":" + time.ToString("00");
    }
}

// Stats for each level
[System.Serializable]
public class LevelStatistics
{
    public EDifficulty eDifficulty;

    public long playedTime; // In seconds
    public long recordTime = -1; // Time of quickest grid resolved in seconds
    public int resolvedGrids;
    public int generatedGrids;
    public int createdAreas;
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

        str += "\nPlayed time: " + Statistics.FormatFloatToTime(playedTime);
        str += "\nRecord: " + Statistics.FormatFloatToTime(recordTime);
        str += "\nGenerated grids: " + generatedGrids;
        str += "\nResolved grids: " + resolvedGrids;
        str += "\nCreated areas: " + createdAreas;
        str += "\nTotal used hints: " + usedHints;

        return str;
    }
}