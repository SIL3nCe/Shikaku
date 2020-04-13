using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class GameGrid : MonoBehaviour
{
    private int width = 0;
    private int height = 0;

    [Tooltip("Cell object to instantiate")]
    public GameObject cellPrefab;

	private float m_fCanvasScaleInvert;

    // Height = i (y), Width = j (x)
    private GameObject[,] aGridView;

    private GridModel aGridModel;

    private int usedCellCounter;
    private bool bGridEnded;

    // Selection
    private bool bSelection;
    private Vector3 vSelectionStart;
    
    private Vector2Int vLastCellEntered;
    private Vector2Int vCellStart;
    private Vector2Int vCellEnd;

    private List<Cell> aSelectedCells;
    // -

    // Selected area visual
    public Sprite AreaSelectionRectangle;
    public GameObject ParentCanvasForImages;

    private List<GameObject> aValidatedAreas;


    private Resolver resolver;

    void Start()
    {
		//
		// Tests
		//GridGenerator.TestValidityNeighbourSplitting();

		m_fCanvasScaleInvert = 1.0f / GameObject.Find("Canvas").transform.localScale.x;

		aValidatedAreas = new List<GameObject>();
    }

    public void Clean()
    {
        aSelectedCells = new List<Cell>();
        resolver = null;
        aGridModel = null;

        int nAreas = aValidatedAreas.Count;
        for (int i = 0; i < nAreas; ++i)
        {
            Destroy(aValidatedAreas[i]);
        }
        aValidatedAreas = new List<GameObject>();

        for (int i = 0; i < height; ++i)
        {
            for (int j = 0; j < width; ++j)
            {
                Destroy(aGridView[i, j]);
            }
        }
        usedCellCounter = 0;
    }

    public void Generate(EDifficulty eDifficulty)
    {
        Clean();

        Debug.Log("Generate grid");
        float fTimeCounter = Time.realtimeSinceStartup;
        GridGenerator generator = new GridGenerator();
        var retVal = generator.Generate(eDifficulty);
        Assert.IsTrue(retVal.Item1);
        Debug.Log("Generated grid in " + (Time.realtimeSinceStartup - fTimeCounter));

        aGridModel = retVal.Item2;
        aGridModel.m_aAreaList.Sort();

        height = aGridModel.m_iHeight;
        width = aGridModel.m_iWidth;

        aGridView = new GameObject[height, width];

        // Set grid center in 0,0
        float fTopLeftX = 0.5f - (width * 0.5f);
        float fTopLeftY = -0.5f + (height * 0.5f);

        int cellSize = 1;
        float x = fTopLeftX, y = fTopLeftY;
        for (int iHeight = 0; iHeight < height; ++iHeight)
        {
            for (int iWidth = 0; iWidth < width; ++iWidth)
            {
                aGridView[iHeight, iWidth] = Instantiate(cellPrefab, GameObject.Find("Canvas").transform);
				aGridView[iHeight, iWidth].GetComponent<Cell>().transform.localScale = new Vector3(m_fCanvasScaleInvert, m_fCanvasScaleInvert, m_fCanvasScaleInvert);
				aGridView[iHeight, iWidth].GetComponent<RectTransform>().anchoredPosition3D = new Vector3(x * m_fCanvasScaleInvert, y * m_fCanvasScaleInvert, 0.0f);
                aGridView[iHeight, iWidth].GetComponent<Cell>().Initialize(iHeight, iWidth, 1.0f, this);
				aGridView[iHeight, iWidth].GetComponent<Cell>().transform.localScale = new Vector3(m_fCanvasScaleInvert, m_fCanvasScaleInvert, m_fCanvasScaleInvert);
				x += cellSize + 0.08f;
				usedCellCounter++;
            }
            x = fTopLeftX;
            y -= cellSize + 0.08f;
        }

        int nArea = aGridModel.m_aAreaList.Count;
        for (int i = 0; i < nArea; ++i)
        {
            Area area = aGridModel.m_aAreaList[i];
            aGridView[area.x, area.y].GetComponent<Cell>().SetAreaSize(area.value);
            
            // Create Image rectangle for each origin
            GameObject NewObj = new GameObject();
            Image NewImage = NewObj.AddComponent<Image>();
            NewImage.sprite = AreaSelectionRectangle;
            NewImage.rectTransform.pivot = new Vector2(0.0f, 1.0f);
            NewImage.enabled = false; // Hide it for now
            //NewImage.transform.localScale = new Vector3(8.0f, -8.0f, 1.0f); // Negative on height because we always drawing from top left
            NewImage.type = Image.Type.Sliced;
            NewImage.fillCenter = false;
			NewImage.pixelsPerUnitMultiplier = m_fCanvasScaleInvert * 0.15f;

			NewObj.GetComponent<RectTransform>().SetParent(ParentCanvasForImages.transform); //Assign the newly created Image GameObject as a Child of the Parent Panel.
            NewObj.SetActive(true);

            aValidatedAreas.Add(NewObj);
        }

        Debug.Log("Resolving grid");
        fTimeCounter = Time.realtimeSinceStartup;
        resolver = new Resolver();
        resolver.Resolve(aGridModel);
        Debug.Log("Resolved grid in " + (Time.realtimeSinceStartup - fTimeCounter));

        bGridEnded = false;
    }

    public bool CheckGridFeasbility()
    {
        if (bGridEnded)
            return true;

        // Check in solutions if current grid is valid based on solver solutions
        return -1 != resolver.CheckGridFeasbility(aGridModel);
    }

    public bool TakeAGridStep()
    {
        if (bGridEnded)
            return false;

        // Search for the first not done yet origin and try to complete it
        int solutionId = resolver.CheckGridFeasbility(aGridModel);
        if (-1 != solutionId)
        {
            int nArea = aGridModel.m_aAreaList.Count;
            for (int iArea = 0; iArea < nArea; ++iArea)
            {
                if (-1 == aGridModel.m_aAreaList[iArea].startX)
                {
                    Area area = resolver.GetCompletedArea(solutionId, iArea);

                    vCellStart.x = area.startX;
                    vCellStart.y = area.startY;
                    vCellEnd.x = vCellStart.x + (area.height - 1);
                    vCellEnd.y = vCellStart.y + (area.width - 1);

                    // Fill selected cells array used to create area when selection end
                    for (int i = 0; i < area.height; ++i)
                    {
                        for (int j = 0; j < area.width; ++j)
                        {
                            Cell cell = aGridView[vCellStart.x + i, vCellStart.y + j].GetComponent<Cell>();
                            //cell.GetComponent<SpriteRenderer>().color = Color.yellow;
                            aSelectedCells.Add(cell);
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
    
    public void OnCellHitByCursor(Vector2Int vCellCoord)
    {
        vLastCellEntered = vCellCoord;

        if (bSelection)
        {
            // Check that current selected area is valid

            Vector2Int vTopLeft = new Vector2Int(Mathf.Min(vCellStart.x, vCellCoord.x), Mathf.Min(vCellStart.y, vCellCoord.y));
            int areaWidth = Mathf.Abs(vCellStart.y - vCellCoord.y) + 1;
            int areaHeight = Mathf.Abs(vCellStart.x - vCellCoord.x) + 1;

            int areaId = -1;
            if (CanAreaBeSelected(vTopLeft, areaWidth, areaHeight))
            {
                // If selection area is valid, reset last selected cells color
                int nCells = aSelectedCells.Count;
                for (int i = 0; i < nCells; ++i)
                {
                    aSelectedCells[i].GetComponent<SpriteRenderer>().color = Color.white;
                }

                // Clear selected cells array (then don't need to compute cells to add only)
                aSelectedCells.Clear();

                // Chose color based on if area could is a valid one or not
                Color cSelectedColor = Color.cyan;
                if (IsAreaValid(vTopLeft, areaWidth, areaHeight, ref areaId))
                    cSelectedColor = Color.green;

                // And fill it with new selected and colored cells
                for (int i = 0; i < areaHeight; ++i)
                {
                    for (int j = 0; j < areaWidth; ++j)
                    {
                        Cell cell = aGridView[vTopLeft.x + i, vTopLeft.y + j].GetComponent<Cell>();
                        cell.GetComponent<SpriteRenderer>().color = cSelectedColor;
                        aSelectedCells.Add(cell);
                    }
                }
            }
        }
    }

    public bool BeginSelection()
    {
        if (bGridEnded)
            return false;

        Cell LastEnteredCell = aGridView[vLastCellEntered.x, vLastCellEntered.y].GetComponent<Cell>();

        // Cursor is no more in the cell
        if (!LastEnteredCell.bHasMouseOnIt)
            return false;

        // Already in area, delete this area
        int lastEnteredAreaId = LastEnteredCell.areaId;
        if (lastEnteredAreaId >= 0)
        {
            for (int iHeight = 0; iHeight < height; ++iHeight)
            {
                for (int iWidth = 0; iWidth < width; ++iWidth)
                {
                    Cell cell = aGridView[iHeight, iWidth].GetComponent<Cell>();
                    if (cell.IsInGivenArea(lastEnteredAreaId))
                    {
                        aGridModel.m_aAreaList[cell.areaId].Reset();
                        cell.areaId = -1;
                        cell.GetComponent<SpriteRenderer>().color = Color.white;
                        usedCellCounter++;
                    }
                }
            }

            aValidatedAreas[lastEnteredAreaId].GetComponent<Image>().enabled = false;

            return false;
        }

        // Otherwise begin selection
        bSelection = true;

        vCellStart = vLastCellEntered;
        vSelectionStart = Input.mousePosition;
        vSelectionStart.y = Camera.main.pixelHeight - vSelectionStart.y;

        return true;
    }

    public void StopSelection()
    {
        if (bSelection)
        {
            bSelection = false;
            vCellEnd = vLastCellEntered;
            OnSelectionEnded();
        }
    }

    private void OnSelectionEnded()
	{
		int nCells = aSelectedCells.Count;
		if(0 == nCells)
		{
			return;
		}

		//
		// Look for top-left and bottom-right cells
		Cell cellTopLeft = aSelectedCells[0], cellBottomRight = aSelectedCells[0];
		Vector2Int vSelectionTopLeft = new Vector2Int(height, width);
		Vector2Int vSelectionBottomRight = new Vector2Int(-1, -1);
		foreach (Cell cell in aSelectedCells)
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
                aSelectedCells[i].GetComponent<SpriteRenderer>().color = Color.white;
                aSelectedCells[i].areaId = areaId;
                usedCellCounter--;
            }

            Image image = aValidatedAreas[areaId].GetComponent<Image>();

			//Vector3 vCellLocation = aGridView[vTopLeft.x, vTopLeft.y].transform.position;
			//vCellLocation.x -= 0.45f;
			//vCellLocation.y += 0.45f;
			//image.transform.position = Camera.main.WorldToScreenPoint(vCellLocation);
			//image.rectTransform.sizeDelta = new Vector2((areaWidthAbs * 8.7f) + (1.6f * (areaWidthAbs - 1)), (areaHeightAbs * 8.7f) + (1.6f * (areaHeightAbs - 1))); // Shitty hardcoded numbers
			image.rectTransform.sizeDelta = new Vector2(iWidth + (iWidth-1)*0.08f, iHeight + (iHeight - 1) * 0.08f);
			image.rectTransform.anchoredPosition = new Vector2(
				m_fCanvasScaleInvert * (-(width * 0.5f) + cellTopLeft.GetCoordinates().y * 1.08f),
				m_fCanvasScaleInvert * ((height * 0.5f) - cellTopLeft.GetCoordinates().x * 1.08f));
			image.color = Color.black;
            image.enabled = true;

            aGridModel.m_aAreaList[areaId].startX = cellTopLeft.GetCoordinates().x;
            aGridModel.m_aAreaList[areaId].startY = cellTopLeft.GetCoordinates().y;
            aGridModel.m_aAreaList[areaId].width = iWidth;
            aGridModel.m_aAreaList[areaId].height = iHeight;

            if (usedCellCounter == 0)
            {
                OnGridEnded();
            }

            goto Finish;
        }

        // Invalid area, clean selected cells
        for (int i = 0; i < nCells; ++i)
        {
            aSelectedCells[i].GetComponent<SpriteRenderer>().color = Color.white;
        }

    Finish:
        aSelectedCells.Clear();
    }

    private void OnGridEnded()
    {
        // Grid is ended
        bGridEnded = true;
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

                Cell cell = aGridView[cellX, cellY].GetComponent<Cell>();

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

                    AreaId = aGridModel.GetAreaId(cellX, cellY);
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

                Cell cell = aGridView[cellX, cellY].GetComponent<Cell>();

                // A cell in given area already belong to an area
                if (cell.IsInArea())
                    return false;
            }
        }

        return true;
    }
}
