using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public static class SaveManager
{
    private const string strStatisticsSaveName = "/Statistics";

    public static Statistics Stats;

    public static void SaveStats()
    {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(Application.persistentDataPath + strStatisticsSaveName);
        bf.Serialize(file, Stats);
        file.Close();
    }

    public static void LoadStats()
    {
        Stats = new Statistics();
        if (File.Exists(Application.persistentDataPath + strStatisticsSaveName))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(Application.persistentDataPath + strStatisticsSaveName, FileMode.Open);
            Stats = (Statistics)bf.Deserialize(file);
            file.Close();
        }
        else
        {
            ResetStats();
        }
    }

    public static void ResetStats()
    {
        Stats = new Statistics();
        SaveStats();
    }


    //TODO Save/Load Current generated grids
}