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
	public GameObject CanvasGame;
	public CanvasGameInputsHandler m_inputHandler;
    private float m_fCanvasScaleInvert = 1.0f;

    private bool bSelection;
    private Vector2 vMouseStart;

    void Start()
    {
		if (EDifficulty.max != StaticDatas.eCurrentDifficulty)
		{
			Difficulty = StaticDatas.eCurrentDifficulty;
		}

        SelectionRectangle.enabled = false;
        SelectionRectangle.transform.SetAsLastSibling(); // Draw it last, on top of all gui
        gameGrid = gameObject.GetComponent<GameGrid>();

		UpdateScale(1.0f);

        gameGrid.Generate(Difficulty);

		bSelection = false;
    }

	private void UpdateScale(float fScale)
	{
		//
		// Selection rectangles
		Vector3 vPos = SelectionRectangle.rectTransform.anchoredPosition3D;
		SelectionRectangle.rectTransform.anchoredPosition3D = new Vector3(vPos.x * m_fCanvasScaleInvert, vPos.y * m_fCanvasScaleInvert, vPos.z * m_fCanvasScaleInvert);
		SelectionRectangle.transform.localScale = new Vector3(m_fCanvasScaleInvert, m_fCanvasScaleInvert, m_fCanvasScaleInvert);
		SelectionRectangle.pixelsPerUnitMultiplier = m_fCanvasScaleInvert / 2;
		SelectionRectangle.pixelsPerUnitMultiplier = m_fCanvasScaleInvert / 2;

		//
		// Gamegrid
		gameGrid.UpdateScale(m_fCanvasScaleInvert);

		//
		// Update value
		m_fCanvasScaleInvert = fScale;
	}

    void Update()
    {
		//
		// Update scale of canvas according to window in case of a resize
		float fScale = 1.0f;
		if(!Mathf.Approximately(fScale, m_fCanvasScaleInvert))
		{
			UpdateScale(fScale);
		}

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
			Vector2 vInputPosition = m_inputHandler.GetInputPosition();
			SelectionRectangle.transform.SetAsLastSibling(); // Draw it last, on top of all gui
            if (!bSelection)
			{
                // Check game grid can begin selection before
                if (gameGrid.BeginSelection(vInputPosition))
                {
                    vMouseStart = vInputPosition;
                    SelectionRectangle.rectTransform.anchoredPosition3D = vMouseStart * SelectionRectangle.rectTransform.localScale;
                    SelectionRectangle.enabled = true;
                    bSelection = true;
                }
            }
            else
            {
				float fWidth = (vMouseStart.x - vInputPosition.x);
				float fHeight = (vMouseStart.y - vInputPosition.y);
				Vector3 vScale = new Vector3(fWidth < 0.0f ? m_fCanvasScaleInvert : -m_fCanvasScaleInvert, fHeight < 0.0f ? m_fCanvasScaleInvert : -m_fCanvasScaleInvert, m_fCanvasScaleInvert);
				SelectionRectangle.transform.localScale = vScale;
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
			SelectionRectangle.transform.localScale = new Vector3(m_fCanvasScaleInvert, m_fCanvasScaleInvert, m_fCanvasScaleInvert);
        }
    }

	public GameGrid GetGameGrid()
	{
		return gameGrid;
	}
}
