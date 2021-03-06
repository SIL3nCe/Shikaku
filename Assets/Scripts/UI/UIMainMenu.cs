﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIMainMenu : MonoBehaviour
{
    // Midle layout with content
    public GameObject ContentMain;
    public GameObject ContentOptions;
    public GameObject ContentHelp;
    public GameObject ContentStats;

    // Bottom layout with buttons
    public GameObject ButtonMain;
    public GameObject ButtonBack;

    public void Start()
    {
        // Init saved datas
        SaveManager.LoadStats();

        // Write stats in text zone
        ContentStats.GetComponentInChildren<Text>().text = SaveManager.Stats.GetAsString();

        // In case layout is not well setup in scene
        OnBackEvent();
    }

    // Play button event
    public void OnPlayEvent(int level)
    {
        // 0>3 : Very easy, easy, medium, hard
        StaticDatas.eCurrentDifficulty = (EDifficulty)level;
        SceneManager.LoadScene("SampleScene");
    }
    
    public void OnCustomEvent()
    {
    }

    public void OnHelpEvent()
    {
        ContentMain.SetActive(false);
        ButtonMain.SetActive(false);

        ContentHelp.SetActive(true);
        ButtonBack.SetActive(true);
    }

    public void OnStatisticsEvent()
    {
        ContentMain.SetActive(false);
        ButtonMain.SetActive(false);

        ContentStats.SetActive(true);
        ButtonBack.SetActive(true);
    }

    public void OnOptionsEvent()
    {
        ContentMain.SetActive(false);
        ButtonMain.SetActive(false);

        ContentOptions.SetActive(true);
        ButtonBack.SetActive(true);
    }

    public void OnBackEvent()
    {
        ContentStats.SetActive(false);
        ContentHelp.SetActive(false);
        ContentOptions.SetActive(false);
        ButtonBack.SetActive(false);

        ContentMain.SetActive(true);
        ButtonMain.SetActive(true);
    }

    public void ResetStatistics()
    {
        SaveManager.ResetStats();
        ContentStats.GetComponentInChildren<Text>().text = SaveManager.Stats.GetAsString();
    }
}
