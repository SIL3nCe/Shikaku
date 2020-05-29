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

    public void AddResolvedGrid(EDifficulty eDiff)
    {
        aDifficultyStats[(int)eDiff].resolvedGrids++;
        totalResolvedGrids++;
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
    public void AddTimePassedInGrid(EDifficulty eDiff, float fTime)
    {
        aDifficultyStats[(int)eDiff].fPlayedTime += fTime;
        fTotalPlayTime += fTime;

        if (aDifficultyStats[(int)eDiff].fRecordTime > fTime)
            aDifficultyStats[(int)eDiff].fRecordTime = fTime;

        SaveManager.SaveStats();
    }

    public string GetAsString()
    {
        string str = "Global";

        str += "\nTotal played time: " + fTotalPlayTime;
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
}

// Stats for each level
[System.Serializable]
public class LevelStatistics
{
    public EDifficulty eDifficulty;

    public float fPlayedTime; // store time in seconds
    public float fRecordTime; // Time of quickest grid resolved
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

        str += "\nPlayed time: " + fPlayedTime;
        str += "\nRecord: " + fRecordTime;
        str += "\nGenerated grids: " + generatedGrids;
        str += "\nResolved grids: " + resolvedGrids;
        str += "\nCreated areas: " + createdAreas;
        str += "\nTotal used hints: " + usedHints;

        return str;
    }
}