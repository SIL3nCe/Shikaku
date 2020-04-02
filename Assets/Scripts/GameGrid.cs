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

    private bool bSelection;
    private Vector3 vSelectionStart;
    private Vector2Int vCellStart;
    private Vector2Int vCellEnd;

    private Resolver resolver;

    void Start()
    {
		//
		// Tests
		//GridGenerator.TestValidityNeighbourSplitting();
    }

    public void Clean()
    {
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

        // 10*10
        //aGridView[0, 8].GetComponent<Cell>().SetAreaSize(9);
        //aGridView[1, 6].GetComponent<Cell>().SetAreaSize(9);
        //aGridView[3, 1].GetComponent<Cell>().SetAreaSize(20);
        //aGridView[3, 4].GetComponent<Cell>().SetAreaSize(8);
        //aGridView[3, 8].GetComponent<Cell>().SetAreaSize(6);
        //aGridView[5, 3].GetComponent<Cell>().SetAreaSize(6);
        //aGridView[5, 6].GetComponent<Cell>().SetAreaSize(6);
        //aGridView[6, 0].GetComponent<Cell>().SetAreaSize(10);
        //aGridView[8, 2].GetComponent<Cell>().SetAreaSize(6);
        //aGridView[8, 4].GetComponent<Cell>().SetAreaSize(6);
        //aGridView[8, 8].GetComponent<Cell>().SetAreaSize(8);
        //aGridView[9, 6].GetComponent<Cell>().SetAreaSize(6);

        //Hardcoded 5*5 grid for tests
        //aGridView[0, 0].GetComponent<Cell>().SetAreaSize(2);
        //aGridView[0, 2].GetComponent<Cell>().SetAreaSize(4);
        //aGridView[0, 3].GetComponent<Cell>().SetAreaSize(2);
        //aGridView[1, 1].GetComponent<Cell>().SetAreaSize(4);
        //aGridView[1, 3].GetComponent<Cell>().SetAreaSize(2);
        //aGridView[2, 0].GetComponent<Cell>().SetAreaSize(2);
        //aGridView[2, 4].GetComponent<Cell>().SetAreaSize(3);
        //aGridView[4, 2].GetComponent<Cell>().SetAreaSize(3);
        //aGridView[4, 3].GetComponent<Cell>().SetAreaSize(3);

        // Multiple solutions 5*5
        //aGridView[0, 0].GetComponent<Cell>().SetAreaSize(3);
        //aGridView[0, 3].GetComponent<Cell>().SetAreaSize(4);
        //aGridView[1, 2].GetComponent<Cell>().SetAreaSize(2);
        //aGridView[1, 3].GetComponent<Cell>().SetAreaSize(2);
        //aGridView[2, 1].GetComponent<Cell>().SetAreaSize(3);
        //aGridView[2, 3].GetComponent<Cell>().SetAreaSize(3);
        //aGridView[3, 0].GetComponent<Cell>().SetAreaSize(2);
        //aGridView[3, 2].GetComponent<Cell>().SetAreaSize(2);
        //aGridView[4, 4].GetComponent<Cell>().SetAreaSize(2);
        //aGridView[4, 3].GetComponent<Cell>().SetAreaSize(2);

        //for (int iHeight = 0; iHeight < height; ++iHeight)
        //{
        //    for (int iWidth = 0; iWidth < width; ++iWidth)
        //    {
        //        int val = aGridView[iHeight, iWidth].GetComponent<Cell>().GetAreaOriginValue();
        //
        //        aGridModel.m_aCells[iHeight, iWidth] = val;
        //
        //        if (val != 0)
        //        {
        //            Area area = new Area(iHeight, iWidth, val);
        //            aGridModel.m_aAreaList.Add(area);
        //        }
        //    }
        //}

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

                    OnSelectionEnded();

                    return true;
                }
            }
        }
        
        return false;
    }

    void OnGUI()
    {
        if (bSelection)
        {
            Vector3 vCurrMousePos = Input.mousePosition;
            vCurrMousePos.y = Camera.main.pixelHeight - vCurrMousePos.y;

            // TODO Keep last known size if new covered cells give an invalid area

            GUI.Box(new Rect(vSelectionStart.x, vSelectionStart.y, vCurrMousePos.x - vSelectionStart.x, vCurrMousePos.y - vSelectionStart.y), ":aaaaaaaaaaaaaaaaaaaaaaaah:");
        }
    }
    
    public void BeginSelection(Vector2Int cellCoord, int areaId)
    {
        if (bGridEnded)
            return;

        if (areaId >= 0)
        {
            // Already in area, delete this area
            for (int iHeight = 0; iHeight < height; ++iHeight)
            {
                for (int iWidth = 0; iWidth < width; ++iWidth)
                {
                    Cell cell = aGridView[iHeight, iWidth].GetComponent<Cell>();
                    if (cell.IsInGivenArea(areaId))
                    {
                        aGridModel.m_aAreaList[cell.areaId].Reset();
                        cell.areaId = -1;
                        cell.GetComponent<SpriteRenderer>().color = Color.white;
                        usedCellCounter++;
                    }
                }
            }
        }
        else
        {
            // Begin selection
            bSelection = true;
            vCellStart = cellCoord;
            vSelectionStart = Input.mousePosition;
            vSelectionStart.y = Camera.main.pixelHeight - vSelectionStart.y;
        }
    }

    public void StopSelection()
    {
        if (bSelection)
        {
            bSelection = false;

            Vector3 vCurrMousePos = Input.mousePosition;
            vCurrMousePos.z = Camera.main.farClipPlane; //distance of the plane from the camera
            vCurrMousePos = Camera.main.ScreenToWorldPoint(vCurrMousePos);

            //TODO could be computed without loops, with cell size and coordinates
            for (int iHeight = 0; iHeight < height; ++iHeight)
            {
                for (int iWidth = 0; iWidth < width; ++iWidth)
                {
                    if (RectTransformUtility.RectangleContainsScreenPoint(aGridView[iHeight, iWidth].GetComponent<RectTransform>(), vCurrMousePos))
                    {
                        vCellEnd.x = iHeight;
                        vCellEnd.y = iWidth;
                        break;
                    }
                }
            }

            OnSelectionEnded();
        }
    }

    private void OnSelectionEnded()
    {
        Vector2Int vTopLeft = new Vector2Int(Mathf.Min(vCellStart.x, vCellEnd.x), Mathf.Min(vCellStart.y, vCellEnd.y));
        int areaWidth = Mathf.Abs(vCellStart.y - vCellEnd.y) + 1;
        int areaHeight = Mathf.Abs(vCellStart.x - vCellEnd.x) + 1;

        int areaId = -1;
        if (IsAreaValid(vTopLeft, areaWidth, areaHeight, ref areaId))
        {
            Color areaColor = UnityEngine.Random.ColorHSV();
            // TODO draw a rectangle around cells vs change cell color ?
            for (int i = 0; i < areaHeight; ++i)
            {
                for (int j = 0; j < areaWidth; ++j)
                {
                    Cell cell = aGridView[vTopLeft.x + i, vTopLeft.y + j].GetComponent<Cell>();
                    cell.GetComponent<SpriteRenderer>().color = areaColor;
                    cell.areaId = areaId;
                    usedCellCounter--;
                }
            }

            aGridModel.m_aAreaList[areaId].startX = vTopLeft.x;
            aGridModel.m_aAreaList[areaId].startY = vTopLeft.y;
            aGridModel.m_aAreaList[areaId].width = areaWidth;
            aGridModel.m_aAreaList[areaId].height = areaHeight;

            if (usedCellCounter == 0)
            {
                OnGridEnded();
            }
        }
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

                // A cell in area is already in another area
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
}
