using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIMainMenu : MonoBehaviour
{
    // Play button event
    public void OnPlayEvent(int level)
    {
        // 0>3 : Very easy, easy, medium, hard
        StaticDatas.eCurrentDifficulty = (EDifficulty)level;
        SceneManager.LoadScene("GameGrid");
    }
    
    public void OnCustomEvent()
    {
    }

    public void OnHelpEvent()
    {
    }

    public void OnStatisticsEvent()
    {
    }

    public void OnOptionsEvent()
    {
    }
}
