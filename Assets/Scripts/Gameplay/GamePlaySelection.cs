using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class GamePlaySelection : GameGridListener
{
	public GameGrid m_GameGrid;

	//
	// Selection
	private List<Cell> m_aSelectedCells;
	private bool m_bSelection;
	private Vector2 m_vSelectionStart;
	private Vector2 m_vSelectionCurrent;

	public Image SelectionRectangle;

	[Range(1, 50)]
	public float fSelecRectScale;

	public Sprite m_AreaSelectionRectangle;
	public Canvas m_CanvasGame;

	//
	// Input related
	private Cell m_cellHovered = null;
	private Cell m_cellLastHovered = null;
	private Cell m_cellFirstHovered = null;
	private Vector2Int m_vLastCellEntered;
	private Vector2Int m_vCellStart;
	private Vector2Int m_vCellEnd;

	public GameObject m_ValidatedAreasContainer;

	private List<GameObject> m_aValidatedAreas;

	//
	// Debug
	public bool m_bDebugSelection = false;
	public bool m_bDebugCellHovered = false;

	// Start is called before the first frame update
	void Start()
	{
		m_aValidatedAreas = new List<GameObject>();
		m_aSelectedCells = new List<Cell>();

		SelectionRectangle.enabled = false;
		SelectionRectangle.transform.SetAsLastSibling(); // Draw it last, on top of all gui

		m_bSelection = false;

		m_GameGrid.AddGameGridListener(this);
	}

	void Update()
	{
		if (m_bDebugSelection)
		{
			//
			// GameGrid
			Vector2 vTopLeft = m_GameGrid.GetTopLeft();
			Debug.DrawLine(new Vector3(m_CanvasGame.gameObject.transform.position.x + vTopLeft.x, m_CanvasGame.gameObject.transform.position.y + vTopLeft.y, 20.0f), new Vector3(m_CanvasGame.gameObject.transform.position.x + -vTopLeft.x, m_CanvasGame.gameObject.transform.position.y + vTopLeft.y, 20.0f), new Color(1.0f, 0.0f, 0.0f));
			Debug.DrawLine(new Vector3(m_CanvasGame.gameObject.transform.position.x + vTopLeft.x, m_CanvasGame.gameObject.transform.position.y + vTopLeft.y, 20.0f), new Vector3(m_CanvasGame.gameObject.transform.position.x + vTopLeft.x, m_CanvasGame.gameObject.transform.position.y + -vTopLeft.y, 20.0f), new Color(1.0f, 0.0f, 0.0f));
			Debug.DrawLine(new Vector3(m_CanvasGame.gameObject.transform.position.x + -vTopLeft.x, m_CanvasGame.gameObject.transform.position.y + -vTopLeft.y, 20.0f), new Vector3(m_CanvasGame.gameObject.transform.position.x + -vTopLeft.x, m_CanvasGame.gameObject.transform.position.y + vTopLeft.y, 20.0f), new Color(1.0f, 0.0f, 0.0f));
			Debug.DrawLine(new Vector3(m_CanvasGame.gameObject.transform.position.x + -vTopLeft.x, m_CanvasGame.gameObject.transform.position.y + -vTopLeft.y, 20.0f), new Vector3(m_CanvasGame.gameObject.transform.position.x + vTopLeft.x, m_CanvasGame.gameObject.transform.position.y + -vTopLeft.y, 20.0f), new Color(1.0f, 0.0f, 0.0f));

			//
			// InputPosition
			Vector2 vInputPosition = m_vSelectionCurrent;
			Debug.DrawLine(new Vector3(m_CanvasGame.gameObject.transform.position.x + vInputPosition.x - 0.3f, m_CanvasGame.gameObject.transform.position.y + vInputPosition.y, 20.0f), new Vector3(m_CanvasGame.gameObject.transform.position.x + vInputPosition.x + 0.3f, m_CanvasGame.gameObject.transform.position.y + vInputPosition.y, 20.0f), new Color(1.0f, 0.0f, 0.0f));
			Debug.DrawLine(new Vector3(m_CanvasGame.gameObject.transform.position.x + vInputPosition.x, m_CanvasGame.gameObject.transform.position.y + vInputPosition.y + 0.3f, 20.0f), new Vector3(m_CanvasGame.gameObject.transform.position.x + vInputPosition.x, m_CanvasGame.gameObject.transform.position.y + vInputPosition.y - 0.3f, 20.0f), new Color(1.0f, 0.0f, 0.0f));
		}
	}

	// Update is called once per frame
	//void OnSelectionUpdate(Vector2 vScreenPosition)
	//{
	//	m_vSelectionCurrent = vScreenPosition;
	//	SelectionRectangle.transform.SetAsLastSibling(); // Draw it last, on top of all gui
	//	if (!m_bSelection)
	//	{
	//		// Check game grid can begin selection before
	//		if (BeginSelection(vScreenPosition))
	//		{
	//			m_vSelectionStart = vScreenPosition;
	//			SelectionRectangle.rectTransform.anchoredPosition = m_vSelectionStart;
	//			SelectionRectangle.enabled = true;
	//			m_bSelection = true;
	//		}
	//	}
	//	else
	//	{
	//		float fWidth = (m_vSelectionStart.x - vScreenPosition.x);
	//		float fHeight = (m_vSelectionStart.y - vScreenPosition.y);
	//		SelectionRectangle.rectTransform.sizeDelta = new Vector2(Mathf.Abs(fWidth), Mathf.Abs(fHeight));
	//	}
	//}

	public void UpdateInputPosition(Vector2 vScreenPosition)
	{
		//
		// No need for inputs if grid is completed
		if (m_GameGrid.IsGridCompleted())
		{
			return;
		}

		//
		// First input
		if(!m_bSelection)
		{
			m_bSelection = true;
			m_vSelectionStart = vScreenPosition;
		}

		//
		// Update current input
		m_vSelectionCurrent = vScreenPosition;

		//
		// Inside grid bounds
		Vector2 vTopLeft = m_GameGrid.GetTopLeft();
		Vector2 vGridSize = m_GameGrid.GetSize();
		float fGridWidth = vGridSize.x;
		float fGridHeight = vGridSize.y;
		if ((		vScreenPosition.x >= vTopLeft.x
				&&	vScreenPosition.x <= vTopLeft.x + fGridWidth
				&&	vScreenPosition.y <= vTopLeft.y
				&&	vScreenPosition.y >= vTopLeft.y - fGridHeight))
		{
			int iWidth = m_GameGrid.GetWidth();
			int iHeight = m_GameGrid.GetHeight();

			//
			// If coming from outside, look for hovered cell
			if (true)//null == m_cellHovered && null == m_cellLastHovered)
			{
				ResolveInputPositionForCells(0, iHeight, 0, iWidth, vScreenPosition);
			}
			else
			{
				//
				// Otherwise check if hovered cell changed using neighbourhood
				int iYM = Mathf.Max(0, m_cellLastHovered.GetCoordinates().y - 1);
				int iYP = Mathf.Min(iHeight, m_cellLastHovered.GetCoordinates().y + 2);
				int iXM = Mathf.Max(0, m_cellLastHovered.GetCoordinates().x - 1);
				int iXP = Mathf.Min(iWidth, m_cellLastHovered.GetCoordinates().x + 2);
				if (!ResolveInputPositionForCells(iYM, iYP, iXM, iXP, vScreenPosition))
				{
					//
					// Fallback
					if (!ResolveInputPositionForCells(0, iHeight, 0, iWidth, vScreenPosition))
					{
						OnCurrentCellUnhovered();
					}
				}
			}
		}
	}

	private bool ResolveInputPositionForCells(int iStartY, int iEndY, int iStartX, int iEndX, Vector2 vScreenPosition)
	{
		GameObject[,] aGridView = m_GameGrid.GetGridView(); ;
		float fCellSize = m_GameGrid.GetCellSize();
		float fHalfCellSize = fCellSize * 0.5f ;
		for (int iColumn = iStartY; iColumn < iEndY; ++iColumn)
		{
			for (int iRow = iStartX; iRow < iEndX; ++iRow)
			{
				GameObject objectCell = aGridView[iColumn, iRow];
				RectTransform t = objectCell.GetComponent<RectTransform>();
				Vector2 vPos = t.anchoredPosition;

				if (		vPos.x - fHalfCellSize <= vScreenPosition.x
						&&	vPos.y + fHalfCellSize >= vScreenPosition.y
						&&	vPos.x + fHalfCellSize >= vScreenPosition.x
						&&	vPos.y - fHalfCellSize <= vScreenPosition.y)
				{
					Cell newCell = objectCell.GetComponent<Cell>();

					// Select new cell
					OnCellHovered(aGridView, newCell);

					return true;
				}
			}
		}
		return false;
	}

	public void InputsStopped()
	{
		//
		// Check for area deletion
		if (	Mathf.Approximately(m_vSelectionCurrent.x, m_vSelectionStart.x)
			&&	Mathf.Approximately(m_vSelectionCurrent.y, m_vSelectionStart.y)
			&&	null != m_cellLastHovered && m_cellLastHovered.IsInArea())
		{
			m_GameGrid.DeleteArea(m_cellLastHovered.areaId);
			m_aValidatedAreas[m_cellLastHovered.areaId].SetActive(false);
		}

		//
		// Selection ended -> area validation
		OnSelectionEnded();

		//
		// Reset state
		m_bSelection = false;
		OnCurrentCellUnhovered();
		m_cellFirstHovered = null;
		m_cellLastHovered = null;
		SelectionRectangle.enabled = false;
		SelectionRectangle.rectTransform.sizeDelta = new Vector2(0.0f, 0.0f);
	}

	public void OnCellHovered(GameObject[,] aGridView, Cell cell)
	{
		if (null != cell && cell == m_cellLastHovered)
		{
			return;
		}

		//
		// Draw it last, on top of all gui
		SelectionRectangle.transform.SetAsLastSibling();

		//
		// If new cell, unselect it and select the new one
		if (m_cellLastHovered != cell)
		{
			OnCurrentCellUnhovered();

			//
			// Update hovered cell
			m_cellHovered = cell;
			m_cellHovered.bHasMouseOnIt = true;

			if(null == m_cellLastHovered)
			{
				m_cellFirstHovered = cell;
				m_vCellStart = m_cellFirstHovered.GetCoordinates();

				SelectionRectangle.rectTransform.anchoredPosition = m_vSelectionStart;
				SelectionRectangle.enabled = true;
			}
			m_cellLastHovered = cell;
			m_vLastCellEntered = m_cellLastHovered.GetCoordinates();

			if(m_bDebugCellHovered)
			{
				cell.GetComponent<SpriteRenderer>().color = new Color(1.0f, 0.0f, 0.0f);
			}

			//
			// Cell already in area, do nothing else
			if(m_cellFirstHovered.IsInArea())
			{
				SelectionRectangle.enabled = false;
				return;
			}

			//
			// Update selection rectangle
			float fWidth = (m_vSelectionStart.x - m_vSelectionCurrent.x);
			float fHeight = (m_vSelectionStart.y - m_vSelectionCurrent.y);
			SelectionRectangle.rectTransform.sizeDelta = new Vector2(Mathf.Abs(fWidth), Mathf.Abs(fHeight));

			// Check that current selected area is valid
			Vector2Int vTopLeft = new Vector2Int(Mathf.Min(m_vCellStart.x, m_vLastCellEntered.x), Mathf.Min(m_vCellStart.y, m_vLastCellEntered.y));
			int areaWidth = Mathf.Abs(m_vCellStart.y - m_vLastCellEntered.y) + 1;
			int areaHeight = Mathf.Abs(m_vCellStart.x - m_vLastCellEntered.x) + 1;

			int areaId = -1;
			if (CanAreaBeSelected(aGridView, vTopLeft, areaWidth, areaHeight))
			{
				// If selection area is valid, reset last selected cells color
				int nCells = m_aSelectedCells.Count;
				for (int i = 0; i < nCells; ++i)
				{
					m_aSelectedCells[i].GetComponent<SpriteRenderer>().color = Color.white;
				}

				// Clear selected cells array (then don't need to compute cells to add only)
				m_aSelectedCells.Clear();

				// Choose color based on if area could is a valid one or not
				Color cSelectedColor = Color.cyan;
				if (m_GameGrid.IsAreaValid(vTopLeft, areaWidth, areaHeight, ref areaId))
				{
					cSelectedColor = Color.green;
				}

				// And fill it with new selected and colored cells
				for (int i = 0; i < areaHeight; ++i)
				{
					for (int j = 0; j < areaWidth; ++j)
					{
						Cell cellTemp = aGridView[vTopLeft.x + i, vTopLeft.y + j].GetComponent<Cell>();
						cellTemp.GetComponent<SpriteRenderer>().color = cSelectedColor;
						m_aSelectedCells.Add(cellTemp);
					}
				}
			}
		}
	}

	private bool CanAreaBeSelected(GameObject[,] aGridview, Vector2Int vTopLeft, int areaWidth, int areaHeight)
	{
		int nCells = areaWidth * areaHeight;

		for (int i = 0; i < areaHeight; ++i)
		{
			int cellX = vTopLeft.x + i;
			for (int j = 0; j < areaWidth; ++j)
			{
				int cellY = vTopLeft.y + j;

				Cell cell = aGridview[cellX, cellY].GetComponent<Cell>();

				// A cell in given area already belong to an area
				if (cell.IsInArea())
				{
					return false;
				}
			}
		}

		return true;
	}

	public void OnCurrentCellUnhovered()
	{
		if (null != m_cellHovered)
		{
			if(m_bDebugCellHovered)
			{
				if(m_cellLastHovered.IsInArea())
				{
					m_cellHovered.GetComponent<SpriteRenderer>().color = new Color(1.0f, 0.0f, 0.0f); // TODO
				}
				else
				{
					m_cellHovered.GetComponent<SpriteRenderer>().color = new Color(1.0f, 1.0f, 1.0f); // TODO
				}
			}
			m_cellHovered.bHasMouseOnIt = false;
			m_cellHovered = null;
		}
	}

	//public bool BeginSelection(Vector2 vInputPosition)
	//{
	//	GameObject[,] aGridview = m_GameGrid.GetGridView();
	//	int iWidth = m_GameGrid.GetWidth();
	//	int iHeight = m_GameGrid.GetHeight();
	//	Cell LastEnteredCell = aGridview[m_vLastCellEntered.x, m_vLastCellEntered.y].GetComponent<Cell>();
	//
	//	// Otherwise begin selection
	//	m_bSelection = true;
	//
	//	m_vCellStart = m_vLastCellEntered;
	//	m_vSelectionStart = vInputPosition;
	//	m_vSelectionStart.y = Camera.main.pixelHeight - m_vSelectionStart.y; // TODO 
	//
	//	return true;
	//}

	//public void StopSelection()
	//{
	//	if (m_bSelection)
	//	{
	//		m_bSelection = false;
	//		m_vCellEnd = m_vLastCellEntered;
	//		OnSelectionEnded();
	//
	//		SelectionRectangle.enabled = false;
	//		SelectionRectangle.rectTransform.sizeDelta = new Vector2(0.0f, 0.0f);
	//	}
	//}

	private void OnSelectionEnded()
	{
		int nCells = m_aSelectedCells.Count;
		if (0 == nCells)
		{
			return;
		}

		//
		// Look for top-left and bottom-right cells
		Cell cellTopLeft = m_aSelectedCells[0], cellBottomRight = m_aSelectedCells[0];
		int iGridWidth = m_GameGrid.GetWidth();
		int iGridHeight = m_GameGrid.GetHeight();
		Vector2Int vSelectionTopLeft = new Vector2Int(iGridHeight, iGridWidth);
		Vector2Int vSelectionBottomRight = new Vector2Int(-1, -1);
		foreach (Cell cell in m_aSelectedCells)
		{
			Vector2Int vCellCoordinates = cell.GetCoordinates();
			if (	vCellCoordinates.x <= vSelectionTopLeft.x
				&&	vCellCoordinates.y <= vSelectionTopLeft.y)
			{
				cellTopLeft = cell;
				vSelectionTopLeft = vCellCoordinates;
			}
			else if (	vCellCoordinates.x >= vSelectionBottomRight.x
					&&	vCellCoordinates.y >= vSelectionBottomRight.y)
			{
				cellBottomRight = cell;
				vSelectionBottomRight = vCellCoordinates;
			}
		}
		int iAreaWidth = 1 + cellBottomRight.GetCoordinates().y - cellTopLeft.GetCoordinates().y;
		int iAreaHeight = 1 + cellBottomRight.GetCoordinates().x - cellTopLeft.GetCoordinates().x;

		//
		// Check area validity
		int iAreaId = -1;
		if (m_GameGrid.IsAreaValid(cellTopLeft.GetCoordinates(), iAreaWidth, iAreaHeight, ref iAreaId))
		{
			for (int i = 0; i < nCells; ++i)
			{
				m_aSelectedCells[i].GetComponent<SpriteRenderer>().color = Color.cyan;
				m_aSelectedCells[i].areaId = iAreaId;
			}

			m_aValidatedAreas[iAreaId].SetActive(true);

			//
			// Update image of validated area to fit the area
			Image image = m_aValidatedAreas[iAreaId].GetComponent<Image>();
			float fCellSpacing = m_GameGrid.GetCellSpacing();
			image.rectTransform.sizeDelta = new Vector2(iAreaWidth + (iAreaWidth - 1) * fCellSpacing, iAreaHeight + (iAreaHeight - 1) * fCellSpacing);
			float fHalfCellSize = m_GameGrid.GetCellSize() * 0.5f;
			Vector2 vTopLeftTopLeft = cellTopLeft.GetComponent<RectTransform>().anchoredPosition3D;
			vTopLeftTopLeft.x -= fHalfCellSize;
			vTopLeftTopLeft.y += fHalfCellSize;
			image.rectTransform.anchoredPosition = vTopLeftTopLeft;
			image.color = Color.black;

			//
			// Select area in GameGrid
			m_GameGrid.SelectArea(cellTopLeft, iAreaHeight, iAreaHeight, iAreaId);

			m_aSelectedCells.Clear();
		}
		else
		{
			// Invalid area, clean selected cells
			for (int i = 0; i < nCells; ++i)
			{
				m_aSelectedCells[i].GetComponent<SpriteRenderer>().color = Color.white;
			}
		}
	}

	public override void OnGameGridCreated(int iAreaCount)
	{
		for (int i = 0; i < iAreaCount; ++i)
		{
			// Create Image rectangle for each origin
			GameObject SelecRect = new GameObject();
			Image NewImage = SelecRect.AddComponent<Image>();
			NewImage.sprite = m_AreaSelectionRectangle;
			NewImage.rectTransform.pivot = new Vector2(0.0f, 1.0f);
			NewImage.type = Image.Type.Sliced;
			NewImage.fillCenter = false;
			NewImage.pixelsPerUnitMultiplier = 10.0f;

			SelecRect.GetComponent<RectTransform>().SetParent(m_ValidatedAreasContainer.transform); //Assign the newly created Image GameObject as a Child of the Parent Panel.
			SelecRect.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
			SelecRect.SetActive(false);

			m_aValidatedAreas.Add(SelecRect);
		}
	}

	public override void OnGameGridStepValidated(Area area)
	{
	}

	public override void OnGameGridStepHinted(Area area)
	{
		GameObject[,] aGridView = m_GameGrid.GetGridView();
		m_vCellStart.x = area.startX;
		m_vCellStart.y = area.startY;
		m_vCellEnd.x = m_vCellStart.x + (area.height - 1);
		m_vCellEnd.y = m_vCellStart.y + (area.width - 1);

		// Fill selected cells array used to create area when selection end
		for (int i = 0; i < area.height; ++i)
		{
			for (int j = 0; j < area.width; ++j)
			{
				Cell cell = aGridView[m_vCellStart.x + i, m_vCellStart.y + j].GetComponent<Cell>();
				//cell.GetComponent<SpriteRenderer>().color = Color.yellow;
				m_aSelectedCells.Add(cell);
			}
		}

		//TODO Just set color as hint or create area ?
		OnSelectionEnded();
	}

	public override void OnGameGridFinished()
	{
		m_aSelectedCells.Clear();
	}

	public override void OnGameGridDestroyed()
	{
		m_aSelectedCells.Clear();

		//
		// Destroy all validates areas
		int nAreas = m_aValidatedAreas.Count;
		for (int i = 0; i < nAreas; ++i)
		{
			Destroy(m_aValidatedAreas[i]);
		}
		m_aValidatedAreas.Clear();
	}
}
