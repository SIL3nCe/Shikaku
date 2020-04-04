using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class GameGrid : MonoBehaviour
{
    private int width = 0;
    private int height = 0;

    [Tooltip("Cell object to instantiate")]
    public GameObject cellPrefab;

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

    private Resolver resolver;

    void Start()
    {
		//
		// Tests
		//GridGenerator.TestValidityNeighbourSplitting();
    }

    public void Clean()
    {
        aSelectedCells = new List<Cell>();
        resolver = null;
        aGridModel = null;

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
                aGridView[iHeight, iWidth] = Instantiate(cellPrefab, new Vector3(x, y, 0), Quaternion.identity);
                aGridView[iHeight, iWidth].GetComponent<Cell>().Initialize(iHeight, iWidth, 1.0f, this);
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
        Vector2Int vTopLeft = new Vector2Int(Mathf.Min(vCellStart.x, vCellEnd.x), Mathf.Min(vCellStart.y, vCellEnd.y));
        int areaWidth = Mathf.Abs(vCellStart.y - vCellEnd.y) + 1;
        int areaHeight = Mathf.Abs(vCellStart.x - vCellEnd.x) + 1;

        int nCells = aSelectedCells.Count;

        int areaId = -1;
        if (IsAreaValid(vTopLeft, areaWidth, areaHeight, ref areaId))
        {
            Color areaColor = UnityEngine.Random.ColorHSV();
            for (int i = 0; i < nCells; ++i)
            {
                aSelectedCells[i].GetComponent<SpriteRenderer>().color = areaColor;
                aSelectedCells[i].areaId = areaId;
                usedCellCounter--;
            }

            aGridModel.m_aAreaList[areaId].startX = vTopLeft.x;
            aGridModel.m_aAreaList[areaId].startY = vTopLeft.y;
            aGridModel.m_aAreaList[areaId].width = areaWidth;
            aGridModel.m_aAreaList[areaId].height = areaHeight;

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
