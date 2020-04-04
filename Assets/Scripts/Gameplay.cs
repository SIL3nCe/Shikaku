using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class Gameplay : MonoBehaviour
{
    private GameGrid gameGrid;

    public EDifficulty Difficulty;
    
    public Image SelectionRectangle;
    [Range(1, 50)]
    public float fSelecRectScale;
    private float fSelecRectInvScale;

    private bool bSelection;
    private Vector2 vMouseStart;

    void Start()
    {
        gameGrid = gameObject.GetComponent<GameGrid>();
        gameGrid.Generate(Difficulty);

        SelectionRectangle.enabled = false;
        bSelection = false;

        fSelecRectInvScale = 1.0f / fSelecRectScale;
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

        if (Input.GetMouseButton(0))
        {
            if (!bSelection)
            {
                // Check game grid can begin selection before
                if (gameGrid.BeginSelection())
                {
                    vMouseStart = Input.mousePosition;
                    SelectionRectangle.transform.position = vMouseStart;
                    SelectionRectangle.enabled = true;
                    bSelection = true;
                }
            }
            else
            {
                Vector2 vCurrentMouseLoc = Input.mousePosition;

                float fWidth = (vMouseStart.x - vCurrentMouseLoc.x) * fSelecRectInvScale;
                float fHeight = (vMouseStart.y - vCurrentMouseLoc.y) * fSelecRectInvScale;

                SelectionRectangle.transform.localScale = new Vector3(fWidth > 0.0f ? -fSelecRectScale : fSelecRectScale, fHeight < 0.0f ? fSelecRectScale : -fSelecRectScale, 1.0f);
                
                SelectionRectangle.rectTransform.sizeDelta = new Vector2(Mathf.Abs(fWidth), Mathf.Abs(fHeight));
            }
        }
        else if (bSelection)
        {
            // Selection ended
            bSelection = false;

            gameGrid.StopSelection();
            SelectionRectangle.enabled = false;
            SelectionRectangle.rectTransform.sizeDelta = new Vector2(0.0f, 0.0f);
            SelectionRectangle.transform.localScale = new Vector3(0.0f, 0.0f, 0.0f);
        }
    }
}
