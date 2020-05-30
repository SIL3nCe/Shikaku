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
	private Vector2 m_vLastInputPosition;

	public Image SelectionRectangle;

	[Range(1, 50)]
	public float fSelecRectScale;

	private Vector2Int m_vLastCellEntered;
	private Vector2Int m_vCellStart;
	private Vector2Int m_vCellEnd;

	public Sprite m_AreaSelectionRectangle;

	//
	// Input related
	private bool m_bInputInsideGrid = false;
	private Cell m_cellHovered = null;
	private Cell m_cellLastHovered = null;
	
	public GameObject m_ValidatedAreasContainer;

	private List<GameObject> m_aValidatedAreas;

	//
	// Debug
	public bool m_bDebugSelection;

	// Start is called before the first frame update
	void Start()
	{
		m_aValidatedAreas = new List<GameObject>();

		SelectionRectangle.enabled = false;
		SelectionRectangle.transform.SetAsLastSibling(); // Draw it last, on top of all gui

		m_bSelection = false;
	}

	void Update()
	{
		if (m_bDebugSelection)
		{
			//
			// GameGrid
			Vector2 vTopLeft = m_GameGrid.GetTopLeft();
			Debug.DrawLine(new Vector3(gameObject.transform.position.x + vTopLeft.x, gameObject.transform.position.y + vTopLeft.y, 20.0f), new Vector3(gameObject.transform.position.x + -vTopLeft.x, gameObject.transform.position.y + vTopLeft.y, 20.0f), new Color(1.0f, 0.0f, 0.0f));
			Debug.DrawLine(new Vector3(gameObject.transform.position.x + vTopLeft.x, gameObject.transform.position.y + vTopLeft.y, 20.0f), new Vector3(gameObject.transform.position.x + vTopLeft.x, gameObject.transform.position.y + -vTopLeft.y, 20.0f), new Color(1.0f, 0.0f, 0.0f));
			Debug.DrawLine(new Vector3(gameObject.transform.position.x + -vTopLeft.x, gameObject.transform.position.y + -vTopLeft.y, 20.0f), new Vector3(gameObject.transform.position.x + -vTopLeft.x, gameObject.transform.position.y + vTopLeft.y, 20.0f), new Color(1.0f, 0.0f, 0.0f));
			Debug.DrawLine(new Vector3(gameObject.transform.position.x + -vTopLeft.x, gameObject.transform.position.y + -vTopLeft.y, 20.0f), new Vector3(gameObject.transform.position.x + vTopLeft.x, gameObject.transform.position.y + -vTopLeft.y, 20.0f), new Color(1.0f, 0.0f, 0.0f));

			//
			// InputPosition
			Vector2 vInputPosition = m_vLastInputPosition;
			Debug.DrawLine(new Vector3(gameObject.transform.position.x + vInputPosition.x - 0.3f, gameObject.transform.position.y + vInputPosition.y, 20.0f), new Vector3(gameObject.transform.position.x + vInputPosition.x + 0.3f, gameObject.transform.position.y + vInputPosition.y, 20.0f), new Color(1.0f, 0.0f, 0.0f));
			Debug.DrawLine(new Vector3(gameObject.transform.position.x + vInputPosition.x, gameObject.transform.position.y + vInputPosition.y + 0.3f, 20.0f), new Vector3(gameObject.transform.position.x + vInputPosition.x, gameObject.transform.position.y + vInputPosition.y - 0.3f, 20.0f), new Color(1.0f, 0.0f, 0.0f));
		}
	}

	// Update is called once per frame
	void OnSelectionUpdate(Vector2 vScreenPosition)
	{
		m_vLastInputPosition = vScreenPosition;
		SelectionRectangle.transform.SetAsLastSibling(); // Draw it last, on top of all gui
		if (!m_bSelection)
		{
			// Check game grid can begin selection before
			if (BeginSelection(vScreenPosition))
			{
				m_vSelectionStart = vScreenPosition;
				SelectionRectangle.rectTransform.anchoredPosition = m_vSelectionStart;
				SelectionRectangle.enabled = true;
				m_bSelection = true;
			}
		}
		else
		{
			float fWidth = (m_vSelectionStart.x - vScreenPosition.x);
			float fHeight = (m_vSelectionStart.y - vScreenPosition.y);
			SelectionRectangle.rectTransform.sizeDelta = new Vector2(Mathf.Abs(fWidth), Mathf.Abs(fHeight));
		}
	}

	public void UpdateInputPosition(Vector2 vScreenPosition)
	{
		Vector2 vTopLeft = m_GameGrid.GetTopLeft();
		int iWidth = m_GameGrid.GetWidth();
		int iHeight = m_GameGrid.GetHeight();

		//
		// Inside grid bounds
		if (!(		vScreenPosition.x < vTopLeft.x 
				||	vScreenPosition.x > vTopLeft.x + iWidth
				||	vScreenPosition.y > vTopLeft.y
				||	vScreenPosition.y < vTopLeft.y - iHeight))
		{
			m_bInputInsideGrid = true;

			//
			// If coming from outside, look for hovered cell
			if (null == m_cellHovered && null == m_cellLastHovered)
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
		float fHalfCellSize = fCellSize * 0.5f;
		for (int iHeight = iStartY; iHeight < iEndY; ++iHeight)
		{
			for (int iWidth = iStartX; iWidth < iEndX; ++iWidth)
			{
				GameObject objectCell = aGridView[iWidth, iHeight];
				RectTransform t = objectCell.GetComponent<RectTransform>();
				Vector2 vPos = t.anchoredPosition;

				if (		vPos.x - fHalfCellSize < vScreenPosition.x
						&&	vPos.y + fHalfCellSize > vScreenPosition.y
						&&	vPos.x + fHalfCellSize > vScreenPosition.x
						&&	vPos.y - fHalfCellSize < vScreenPosition.y)
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
		m_bInputInsideGrid = false;

		OnCurrentCellUnhovered();
		m_cellLastHovered = null;
	}

	public void OnCellHovered(GameObject[,] aGridView, Cell cell)
	{
		if (cell == m_cellHovered)
		{
			return;
		}

		//
		// Unselect cell and select new one
		OnCurrentCellUnhovered();

		//
		// Update hovered cells
		m_cellHovered = cell;
		m_cellLastHovered = cell;

		m_cellHovered.bHasMouseOnIt = true;

		m_vLastCellEntered = cell.GetCoordinates();

		//cell.GetComponent<SpriteRenderer>().color = new Color(1.0f, 0.0f, 0.0f);

		if (m_bSelection)
		{
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

				// Chose color based on if area could is a valid one or not
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
			//m_cellHovered.GetComponent<SpriteRenderer>().color = new Color(1.0f, 1.0f, 1.0f);
			m_cellHovered.bHasMouseOnIt = false;
			m_cellHovered = null;
		}
	}

	public bool BeginSelection(Vector2 vInputPosition)
	{
		if (m_GameGrid.IsGridCompleted())
			return false;

		GameObject[,] aGridview = m_GameGrid.GetGridView();
		int iWidth = m_GameGrid.GetWidth();
		int iHeight = m_GameGrid.GetHeight();
		Cell LastEnteredCell = aGridview[m_vLastCellEntered.x, m_vLastCellEntered.y].GetComponent<Cell>();

		// Cursor is no more in the cell
		if (!LastEnteredCell.bHasMouseOnIt)
			return false;

		// Already in area, delete this area
		int lastEnteredAreaId = LastEnteredCell.areaId;
		if (lastEnteredAreaId >= 0)
		{
			m_GameGrid.DeleteArea(lastEnteredAreaId);
			m_aValidatedAreas[lastEnteredAreaId].SetActive(false);

			return false;
		}

		// Otherwise begin selection
		m_bSelection = true;

		m_vCellStart = m_vLastCellEntered;
		m_vSelectionStart = vInputPosition;
		m_vSelectionStart.y = Camera.main.pixelHeight - m_vSelectionStart.y;

		return true;
	}

	public void StopSelection()
	{
		if (m_bSelection)
		{
			m_bSelection = false;
			m_vCellEnd = m_vLastCellEntered;
			OnSelectionEnded();

			SelectionRectangle.enabled = false;
			SelectionRectangle.rectTransform.sizeDelta = new Vector2(0.0f, 0.0f);
		}
	}

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
			if (vCellCoordinates.x <= vSelectionTopLeft.x
				&& vCellCoordinates.y <= vSelectionTopLeft.y)
			{
				cellTopLeft = cell;
				vSelectionTopLeft = vCellCoordinates;
			}
			else if (vCellCoordinates.x >= vSelectionBottomRight.x
					&& vCellCoordinates.y >= vSelectionBottomRight.y)
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
				m_aSelectedCells[i].GetComponent<SpriteRenderer>().color = Color.white;
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
		m_aSelectedCells = new List<Cell>();

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
