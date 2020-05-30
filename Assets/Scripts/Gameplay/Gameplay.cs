using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class Gameplay : MonoBehaviour
{
    private GameGrid gameGrid;

    public EDifficulty Difficulty;
    
	public GameObject CanvasGame;
	public CanvasGameInputsHandler m_inputHandler;
    private float m_fCanvasScaleInvert = 1.0f;

    void Start()
    {
		if (EDifficulty.max != StaticDatas.eCurrentDifficulty)
		{
			Difficulty = StaticDatas.eCurrentDifficulty;
		}

        gameGrid = gameObject.GetComponent<GameGrid>();
        GenerateNewGrid();
    }

    private void GenerateNewGrid()
    {
        gameGrid.Generate(Difficulty);
        SaveManager.Stats.aDifficultyStats[(int)Difficulty].generatedGrids++;
        SaveManager.SaveStats();
    }

    void Update()
    {
		if (Input.GetKeyDown("r"))
        {
            gameGrid.Generate(Difficulty);
        }
        else if (Input.GetKeyDown("v"))
        {
            if (gameGrid.CheckGridFeasibility())
            {
                Debug.Log("Current grid can be completed");
            }
            else
            {
                Debug.Log("Current grid CANNOT be completed !");
            }
        }
        else if (Input.GetKeyDown("h"))
        {
            if (gameGrid.TakeAGridStep())
            {
                Debug.Log("Grid has been advanced");
            }
            else
            {
                Debug.Log("Grid CANNOT be advanced");
            }
        }
    }

	public GameGrid GetGameGrid()
	{
		return gameGrid;
	}
}
