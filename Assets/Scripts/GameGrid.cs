using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class GameGrid : MonoBehaviour
{

    [Tooltip("Cell object to instantiate")]
    public GameObject m_cellPrefab;

    private float m_fCellSize = 1.0f;
    public float m_fCellSpacing = 0.08f;


    // Height = i (y), Width = j (x)
    private GameObject[,] m_aGridView;
	private Vector2 m_vTopLeft;
	private int m_iWidth = 0;
	private int m_iHeight = 0;
	private float m_fScaleFactor = 1.0f;

	//
	// Input related
	private bool m_bInputInsideCanvas = false;
	private bool m_bInputInsideGrid = false;
	private Cell m_cellHovered = null;
	private Cell m_cellLastHovered = null;

	//
	// Model
	private GridModel m_aGridModel;

    private int m_iUsedCellCounter;
    private bool m_bGridEnded;

    // Selection
    private bool m_bSelection;
    private Vector3 m_vSelectionStart;
    
    private Vector2Int m_vLastCellEntered;
    private Vector2Int m_vCellStart;
    private Vector2Int m_vCellEnd;

    private List<Cell> m_aSelectedCells;
    // -

    // Selected area visual
    public Sprite m_AreaSelectionRectangle;
    public GameObject m_ParentCanvasForImages;
    public GameObject m_CellsContainer;
    public GameObject m_RectsContainer;

	private List<GameObject> m_aValidatedAreas;

    private Resolver m_resolver;

	public void UpdateScale(float fScale)
	{
		for (int iHeight = 0; iHeight < m_iHeight; ++iHeight)
		{
			for (int iWidth = 0; iWidth < m_iWidth; ++iWidth)
			{
				Vector3 vPos = m_aGridView[iHeight, iWidth].GetComponent<RectTransform>().anchoredPosition3D;
				m_aGridView[iHeight, iWidth].GetComponent<RectTransform>().anchoredPosition3D = new Vector3(fScale * vPos.x / m_fScaleFactor, fScale * vPos.y / m_fScaleFactor, 0.0f);
				m_aGridView[iHeight, iWidth].GetComponent<Cell>().gameObject.transform.localScale = new Vector3(fScale, fScale, 1);
			}
		}

		//
		// Validates areas
		foreach(GameObject area in m_aValidatedAreas)
		{
			Image image = area.GetComponent<Image>();
			if(image)
			{
				//image.rectTransform.sizeDelta = new
				image.rectTransform.anchoredPosition3D = new Vector3(fScale * image.rectTransform.anchoredPosition.x / m_fScaleFactor, fScale * image.rectTransform.anchoredPosition.y / m_fScaleFactor, 0.0f);
				image.gameObject.transform.localScale = new Vector3(fScale, fScale, 1.0f);
			}
		}

		//
		// Update scale
		m_fScaleFactor = fScale;
	}

	public void UpdateInputPosition(Vector2 vScreenPosition)
	{
		//
		// Outside
		if(vScreenPosition.x < m_vTopLeft.x || vScreenPosition.x > m_vTopLeft.x + m_iWidth || vScreenPosition.y > m_vTopLeft.y || vScreenPosition.y < m_vTopLeft.y - m_iHeight)
		{
			//
			// Update state
			OnCurrentCellUnhovered();

			m_bInputInsideCanvas = true;
			m_bInputInsideGrid = false;
		}
		else // Inside
		{

			//
			// If coming from outside, look for hovered cell
			if (!m_bInputInsideGrid || (null == m_cellHovered && null == m_cellLastHovered))
			{

				m_bInputInsideGrid = true;
			}
			else
			{
				//
				// Otherwise check if hovered cell changed using neighbourhood
				int iYM = Mathf.Max(0, m_cellLastHovered.GetCoordinates().y - 1);
				int iYP = Mathf.Min(m_iHeight, m_cellLastHovered.GetCoordinates().y + 2);
				int iXM = Mathf.Max(0, m_cellLastHovered.GetCoordinates().x - 1);
				int iXP = Mathf.Min(m_iWidth, m_cellLastHovered.GetCoordinates().x + 2);
				if(!ResolveInputPositionForCells(iYM, iYP, iXM, iXP, vScreenPosition))
				{
					OnCurrentCellUnhovered();
				}
			}
		}
	}

	public void InputsStopped()
	{
		if(m_bInputInsideCanvas)
		{
			m_bInputInsideCanvas = false;
			m_bInputInsideGrid = false;

			OnCurrentCellUnhovered();
			m_cellLastHovered = null;
		}
	}

	private bool ResolveInputPositionForCells(int iStartY, int iEndY, int iStartX, int iEndX, Vector2 vScreenPosition)
	{
		float fHalfCellSize = m_fCellSize * m_fScaleFactor * 0.5f;
		for (int iHeight = iStartY; iHeight < iEndY; ++iHeight)
		{
			for (int iWidth = iStartX; iWidth < iEndX; ++iWidth)
			{
				GameObject objectCell = m_aGridView[iHeight, iWidth];
				RectTransform t = objectCell.GetComponent<RectTransform>();
				Vector2 vPos = t.anchoredPosition;
				if (	vPos.x - fHalfCellSize < vScreenPosition.x && vPos.y + fHalfCellSize > vScreenPosition.y
					&&	vPos.x + fHalfCellSize > vScreenPosition.x && vPos.y - fHalfCellSize < vScreenPosition.y)
				{
					Cell newCell = objectCell.GetComponent<Cell>();

					//
					// Select new cell
					OnCellHovered(newCell);

					return true;
				}
			}
		}
		return false;
	}

	void Start()
    {
        //
        // Tests
        //GridGenerator.TestValidityNeighbourSplitting();

		m_aValidatedAreas = new List<GameObject>();
    }

    public void Clean()
    {
        m_aSelectedCells = new List<Cell>();
        m_resolver = null;
        m_aGridModel = null;

        int nAreas = m_aValidatedAreas.Count;
        for (int i = 0; i < nAreas; ++i)
        {
            Destroy(m_aValidatedAreas[i]);
        }
        m_aValidatedAreas = new List<GameObject>();

        for (int i = 0; i < m_iHeight; ++i)
        {
            for (int j = 0; j < m_iWidth; ++j)
            {
                Destroy(m_aGridView[i, j]);
            }
        }
        m_iUsedCellCounter = 0;
    }

    public void Generate(EDifficulty eDifficulty)
    {
        Clean();

        //
        // Generator
        Debug.Log("Generate grid");
        float fTimeCounter = Time.realtimeSinceStartup;
        GridGenerator generator = new GridGenerator();
        var retVal = generator.Generate(eDifficulty);
        Assert.IsTrue(retVal.Item1);
        Debug.Log("Generated grid in " + (Time.realtimeSinceStartup - fTimeCounter));

        m_aGridModel = retVal.Item2;
        m_aGridModel.m_aAreaList.Sort();

        m_iHeight = m_aGridModel.m_iHeight;
        m_iWidth = m_aGridModel.m_iWidth;
        
        //
        // Solver
        Debug.Log("Resolving grid");
        fTimeCounter = Time.realtimeSinceStartup;
        m_resolver = new Resolver();
        m_resolver.Resolve(m_aGridModel);
        Debug.Log("Resolved grid in " + (Time.realtimeSinceStartup - fTimeCounter));

        //
        // Game grid
        m_aGridView = new GameObject[m_iHeight, m_iWidth];

		// Set grid center in 0,0
		float fCellSize = m_fCellSize * m_fScaleFactor;
		float fHalfCellSize = fCellSize * 0.5f;
		float fCellSpacing = m_fCellSpacing * m_fScaleFactor;
		float fHalfCellSpacing = fCellSpacing * 0.5f;
		if (m_iWidth%2 != 0)
		{
			m_vTopLeft.x = -fHalfCellSize - (m_iWidth/2) * (fCellSpacing + fCellSize);
		}
		else
		{
			m_vTopLeft.x = -fHalfCellSize - fHalfCellSpacing - (m_iWidth/2) * fCellSize - (m_iWidth/2 - 1) * fCellSpacing;
		}

		if (m_iHeight%2 != 0)
		{
			m_vTopLeft.y = fHalfCellSize + (m_iHeight/2) * (fCellSpacing + fCellSize);
		}
		else
		{
			m_vTopLeft.y = fHalfCellSize + fHalfCellSpacing + (m_iHeight/2) * fCellSize + (m_iWidth/2 - 1) * fCellSpacing;
		}

        float x = m_vTopLeft.x, y = m_vTopLeft.y;
        for (int iHeight = 0; iHeight < m_iHeight; ++iHeight)
        {
            for (int iWidth = 0; iWidth < m_iWidth; ++iWidth)
            {
                m_aGridView[iHeight, iWidth] = Instantiate(m_cellPrefab, m_CellsContainer.transform);
				m_aGridView[iHeight, iWidth].GetComponent<RectTransform>().anchoredPosition3D = new Vector3(x + fHalfCellSize, y - fHalfCellSize, 0.0f);
                m_aGridView[iHeight, iWidth].GetComponent<Cell>().Initialize(iWidth, iHeight, fCellSize, this);
				x += fCellSize + fCellSpacing;
				m_iUsedCellCounter++;
            }
            x = m_vTopLeft.x;
            y -= (fCellSize + fCellSpacing);
        }

        int nArea = m_aGridModel.m_aAreaList.Count;
        for (int i = 0; i < nArea; ++i)
        {
            Area area = m_aGridModel.m_aAreaList[i];
            area.Reset();
            m_aGridView[area.x, area.y].GetComponent<Cell>().SetAreaSize(area.value);
            
            // Create Image rectangle for each origin
            GameObject NewObj = new GameObject();
            Image NewImage = NewObj.AddComponent<Image>();
            NewImage.sprite = m_AreaSelectionRectangle;
            NewImage.rectTransform.pivot = new Vector2(0.0f, 1.0f);
            NewImage.enabled = false; // Hide it for now
            //NewImage.transform.localScale = new Vector3(8.0f, -8.0f, 1.0f); // Negative on height because we always drawing from top left
            NewImage.type = Image.Type.Sliced;
            NewImage.fillCenter = false;
			NewImage.pixelsPerUnitMultiplier = 0.15f * m_fScaleFactor;

			NewObj.GetComponent<RectTransform>().SetParent(m_RectsContainer.transform); //Assign the newly created Image GameObject as a Child of the Parent Panel.
            NewObj.SetActive(true);

            m_aValidatedAreas.Add(NewObj);
        }

        m_bGridEnded = false;
    }

    public bool CheckGridFeasbility()
    {
        if (m_bGridEnded)
            return true;

        // Check in solutions if current grid is valid based on solver solutions
        return -1 != m_resolver.CheckGridFeasbility(m_aGridModel);
    }

    public bool TakeAGridStep()
    {
        if (m_bGridEnded)
            return false;

        // Search for the first not done yet origin and try to complete it
        int solutionId = m_resolver.CheckGridFeasbility(m_aGridModel);
        if (-1 != solutionId)
        {
            int nArea = m_aGridModel.m_aAreaList.Count;
            for (int iArea = 0; iArea < nArea; ++iArea)
            {
                if (!m_aGridModel.m_aAreaList[iArea].IsCompleted())
                {
                    Area area = m_resolver.GetCompletedArea(solutionId, iArea);

                    m_vCellStart.x = area.startX;
                    m_vCellStart.y = area.startY;
                    m_vCellEnd.x = m_vCellStart.x + (area.height - 1);
                    m_vCellEnd.y = m_vCellStart.y + (area.width - 1);

                    // Fill selected cells array used to create area when selection end
                    for (int i = 0; i < area.height; ++i)
                    {
                        for (int j = 0; j < area.width; ++j)
                        {
                            Cell cell = m_aGridView[m_vCellStart.x + i, m_vCellStart.y + j].GetComponent<Cell>();
                            //cell.GetComponent<SpriteRenderer>().color = Color.yellow;
                            m_aSelectedCells.Add(cell);
                        }
                    }

                    //TODO Just set color as hint or create area ?
                    OnSelectionEnded();

                    return true;
                }
            }
        }
        
        return false;
    }
    
    public void OnCellHovered(Cell cell)
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
		
		m_cellHovered.bHasMouseOnIt = false;

		m_vLastCellEntered = cell.GetCoordinates();

		cell.GetComponent<SpriteRenderer>().color = new Color(1.0f, 0.0f, 0.0f);

		if (m_bSelection)
        {
            // Check that current selected area is valid

            Vector2Int vTopLeft = new Vector2Int(Mathf.Min(m_vCellStart.x, m_vLastCellEntered.x), Mathf.Min(m_vCellStart.y, m_vLastCellEntered.y));
            int areaWidth = Mathf.Abs(m_vCellStart.y - m_vLastCellEntered.y) + 1;
            int areaHeight = Mathf.Abs(m_vCellStart.x - m_vLastCellEntered.x) + 1;

            int areaId = -1;
            if (CanAreaBeSelected(vTopLeft, areaWidth, areaHeight))
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
                if (IsAreaValid(vTopLeft, areaWidth, areaHeight, ref areaId))
                    cSelectedColor = Color.green;

                // And fill it with new selected and colored cells
                for (int i = 0; i < areaHeight; ++i)
                {
                    for (int j = 0; j < areaWidth; ++j)
                    {
                        Cell cellTemp = m_aGridView[vTopLeft.x + i, vTopLeft.y + j].GetComponent<Cell>();
						cellTemp.GetComponent<SpriteRenderer>().color = cSelectedColor;
                        m_aSelectedCells.Add(cellTemp);
                    }
                }
            }
        }
	}

	public void OnCurrentCellUnhovered()
	{
		if (null != m_cellHovered)
		{
			m_cellHovered.GetComponent<SpriteRenderer>().color = new Color(1.0f, 1.0f, 1.0f);
			m_cellHovered.bHasMouseOnIt = false;
			m_cellHovered = null;
		}
	}

	public bool BeginSelection(Vector2 vInputPosition)
    {
        if (m_bGridEnded)
            return false;

        Cell LastEnteredCell = m_aGridView[m_vLastCellEntered.x, m_vLastCellEntered.y].GetComponent<Cell>();

        // Cursor is no more in the cell
        if (!LastEnteredCell.bHasMouseOnIt)
            return false;

        // Already in area, delete this area
        int lastEnteredAreaId = LastEnteredCell.areaId;
        if (lastEnteredAreaId >= 0)
        {
            for (int iHeight = 0; iHeight < m_iHeight; ++iHeight)
            {
                for (int iWidth = 0; iWidth < m_iWidth; ++iWidth)
                {
                    Cell cell = m_aGridView[iHeight, iWidth].GetComponent<Cell>();
                    if (cell.IsInGivenArea(lastEnteredAreaId))
                    {
                        m_aGridModel.m_aAreaList[cell.areaId].Reset();
                        cell.areaId = -1;
                        cell.GetComponent<SpriteRenderer>().color = Color.white;
                        m_iUsedCellCounter++;
                    }
                }
            }

            m_aValidatedAreas[lastEnteredAreaId].GetComponent<Image>().enabled = false;

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
        }
    }

    private void OnSelectionEnded()
	{
		int nCells = m_aSelectedCells.Count;
		if(0 == nCells)
		{
			return;
		}

		//
		// Look for top-left and bottom-right cells
		Cell cellTopLeft = m_aSelectedCells[0], cellBottomRight = m_aSelectedCells[0];
		Vector2Int vSelectionTopLeft = new Vector2Int(m_iHeight, m_iWidth);
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
		int iWidth	= 1 + cellBottomRight.GetCoordinates().y - cellTopLeft.GetCoordinates().y;
		int iHeight	= 1 + cellBottomRight.GetCoordinates().x - cellTopLeft.GetCoordinates().x;

        int areaId = -1;
        if (IsAreaValid(cellTopLeft.GetCoordinates(), iWidth, iHeight, ref areaId))
        {
            for (int i = 0; i < nCells; ++i)
            {
                m_aSelectedCells[i].GetComponent<SpriteRenderer>().color = Color.white;
                m_aSelectedCells[i].areaId = areaId;
                m_iUsedCellCounter--;
            }

            Image image = m_aValidatedAreas[areaId].GetComponent<Image>();

            //Vector3 vCellLocation = aGridView[vTopLeft.x, vTopLeft.y].transform.position;
            //vCellLocation.x -= 0.45f;
            //vCellLocation.y += 0.45f;
            //image.transform.position = Camera.main.WorldToScreenPoint(vCellLocation);
            //image.rectTransform.sizeDelta = new Vector2((areaWidthAbs * 8.7f) + (1.6f * (areaWidthAbs - 1)), (areaHeightAbs * 8.7f) + (1.6f * (areaHeightAbs - 1))); // Shitty hardcoded numbers
            image.rectTransform.sizeDelta = new Vector2(iWidth + (iWidth - 1) * m_fCellSpacing, iHeight + (iHeight - 1) * m_fCellSpacing);
			image.rectTransform.anchoredPosition = new Vector2(
				m_fScaleFactor * (-(m_iWidth * 0.5f) + cellTopLeft.GetCoordinates().y * (m_fCellSize + m_fCellSpacing)),
				m_fScaleFactor * ((m_iHeight * 0.5f) - cellTopLeft.GetCoordinates().x * (m_fCellSize + m_fCellSpacing)));
			image.color = Color.black;
            image.enabled = true;

            m_aGridModel.m_aAreaList[areaId].startX = cellTopLeft.GetCoordinates().x;
            m_aGridModel.m_aAreaList[areaId].startY = cellTopLeft.GetCoordinates().y;
            m_aGridModel.m_aAreaList[areaId].width = iWidth;
            m_aGridModel.m_aAreaList[areaId].height = iHeight;

            if (m_iUsedCellCounter == 0)
            {
                OnGridEnded();
            }

            goto Finish;
        }

        // Invalid area, clean selected cells
        for (int i = 0; i < nCells; ++i)
        {
            m_aSelectedCells[i].GetComponent<SpriteRenderer>().color = Color.white;
        }

    Finish:
        m_aSelectedCells.Clear();
    }

    private void OnGridEnded()
    {
        // Grid is ended
        m_bGridEnded = true;
        Debug.Log("GRID ENDED, GGWP");
    }

    private bool IsAreaValid(Vector2Int vTopLeft, int areaWidth, int areaHeight, ref int AreaId)
    {
        int nOrigin = 0;

        int nCells = areaWidth * areaHeight;

        for (int i = 0; i < areaHeight; ++i)
        {
            int cellX = vTopLeft.x + i;
            for (int j = 0; j < areaWidth; ++j)
            {
                int cellY = vTopLeft.y + j;

                Cell cell = m_aGridView[cellX, cellY].GetComponent<Cell>();

                // A cell in given area already belong to an area
                if (cell.IsInArea())
                    return false;

                if (cell.IsAreaOrigin())
                {
                    // More than one origin cell in area
                    if (++nOrigin != 1)
                        return false;

                    // Too much/not enough cells in area based on origin cell value
                    if (cell.GetAreaOriginValue() != nCells)
                        return false;

                    AreaId = m_aGridModel.GetAreaId(cellX, cellY);
                }
            }
        }

        return nOrigin != 0;
    }


    private bool CanAreaBeSelected(Vector2Int vTopLeft, int areaWidth, int areaHeight)
    {
        int nCells = areaWidth * areaHeight;

        for (int i = 0; i < areaHeight; ++i)
        {
            int cellX = vTopLeft.x + i;
            for (int j = 0; j < areaWidth; ++j)
            {
                int cellY = vTopLeft.y + j;

                Cell cell = m_aGridView[cellX, cellY].GetComponent<Cell>();

                // A cell in given area already belong to an area
                if (cell.IsInArea())
                    return false;
            }
        }

        return true;
    }
}
