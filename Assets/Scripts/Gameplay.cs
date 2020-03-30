using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gameplay : MonoBehaviour
{
    private GameGrid gameGrid;

    public EDifficulty Difficulty;

    void Start()
    {
        gameGrid = gameObject.GetComponent<GameGrid>();
        gameGrid.Generate(Difficulty);
    }

    void Update()
    {
        if (Input.GetKeyDown("r"))
        {
            gameGrid.Generate(Difficulty);
        }
        else if (Input.GetKeyDown("v"))
        {
            if (gameGrid.CheckGridFeasbility())
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
}
