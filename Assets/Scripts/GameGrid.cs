using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class GameGrid : MonoBehaviour
{
    [Tooltip("Grid size")]
    public int width;
    public int height;

    [Tooltip("Cell object to instantiate")]
    public GameObject cellPrefab;

    // Height = i (y), Width = j (x)
    private GameObject[,] aGrid;

    private int[,] aGridModel;

    private bool bSelection;
    private Vector3 vSelectionStart;
    private Vector2Int vCellStart;

    private int areaCounter;

    private Resolver resolver;

    void Start()
    {
        resolver = new Resolver();
        Generate();
    }

    public void Generate()
    {
        areaCounter = 0;

        height = 10; width = 10; //Hardcoded 5*5 grid for tests

        aGrid = new GameObject[height, width];

        // Set grid center in 0,0
        float fTopLeftX = 0.5f - (width * 0.5f);
        float fTopLeftY = -0.5f + (height * 0.5f);

        int cellSize = 1;
        float x = fTopLeftX, y = fTopLeftY;
        for (int iHeight = 0; iHeight < height; ++iHeight)
        {
            for (int iWidth = 0; iWidth < width; ++iWidth)
            {
                aGrid[iHeight, iWidth] = Instantiate(cellPrefab, new Vector3(x, y, 0), Quaternion.identity);
                aGrid[iHeight, iWidth].GetComponent<Cell>().Initialize(iHeight, iWidth, 1.0f, this);
                x += cellSize + 0.08f;
            }
            x = fTopLeftX;
            y -= cellSize + 0.08f;
        }

        /*Codingame test
         1 solution
            0 0 0 0 0 0 0 0 9 0
            0 0 0 0 0 0 9 0 0 0
            0 0 0 0 0 0 0 0 0 0
            0 20 0 0 8 0 0 0 6 0
            0 0 0 0 0 0 0 0 0 0
            0 0 0 6 0 0 6 0 0 0
            10 0 0 0 0 0 0 0 0 0
            0 0 0 0 0 0 0 0 0 0
            0 0 6 0 6 0 0 0 8 0
            0 0 0 0 0 0 6 0 0 0
         */

        aGrid[0, 8].GetComponent<Cell>().SetAreaSize(9);
        aGrid[1, 6].GetComponent<Cell>().SetAreaSize(9);
        aGrid[3, 1].GetComponent<Cell>().SetAreaSize(20);
        aGrid[3, 4].GetComponent<Cell>().SetAreaSize(8);
        aGrid[3, 8].GetComponent<Cell>().SetAreaSize(6);
        aGrid[5, 3].GetComponent<Cell>().SetAreaSize(6);
        aGrid[5, 6].GetComponent<Cell>().SetAreaSize(6);
        aGrid[6, 0].GetComponent<Cell>().SetAreaSize(10);
        aGrid[8, 2].GetComponent<Cell>().SetAreaSize(6);
        aGrid[8, 4].GetComponent<Cell>().SetAreaSize(6);
        aGrid[8, 8].GetComponent<Cell>().SetAreaSize(8);
        aGrid[9, 6].GetComponent<Cell>().SetAreaSize(6);

        //TODO grid generator
        //Hardcoded 5*5 grid for tests
        //aGrid[0, 0].GetComponent<Cell>().SetAreaSize(2);
        //aGrid[0, 2].GetComponent<Cell>().SetAreaSize(4);
        //aGrid[0, 3].GetComponent<Cell>().SetAreaSize(2);
        //aGrid[1, 1].GetComponent<Cell>().SetAreaSize(4);
        //aGrid[1, 3].GetComponent<Cell>().SetAreaSize(2);
        //aGrid[2, 0].GetComponent<Cell>().SetAreaSize(2);
        //aGrid[2, 4].GetComponent<Cell>().SetAreaSize(3);
        //aGrid[4, 2].GetComponent<Cell>().SetAreaSize(3);
        //aGrid[4, 3].GetComponent<Cell>().SetAreaSize(3);

		GridGenerator generator = new GridGenerator();
		int[,] aTestGrid = new int[0, 0];
		bool bGenerated = generator.Generate(ref aTestGrid, GridGenerator.EDifficulty.easy);
		Assert.IsTrue(bGenerated);

        resolver.Resolve(aTestGrid.GetLength(1), aTestGrid.GetLength(0), aTestGrid);
    }

    void OnGUI()
    {
        if (bSelection)
        {
            Vector3 vCurrMousePos = Input.mousePosition;
            vCurrMousePos.y = Camera.main.pixelHeight - vCurrMousePos.y;

            // TODO Keep last known size if new covered cells give an invalid area

            GUI.Box(new Rect(vSelectionStart.x, vSelectionStart.y, vCurrMousePos.x - vSelectionStart.x, vCurrMousePos.y - vSelectionStart.y), "vntmlrdpapds");
        }
    }
    
    public void BeginSelection(Vector2Int cellCoord, int areaId)
    {
        if (areaId >= 0)
        {
            // Already in area, delete this area
            for (int iHeight = 0; iHeight < height; ++iHeight)
            {
                for (int iWidth = 0; iWidth < width; ++iWidth)
                {
                    Cell cell = aGrid[iHeight, iWidth].GetComponent<Cell>();
                    if (cell.IsInGivenArea(areaId))
                    {
                        cell.SetAreaId(-1);
                        cell.GetComponent<SpriteRenderer>().color = Color.white;
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
            Vector2Int vCellEnd = new Vector2Int();
            for (int iHeight = 0; iHeight < height; ++iHeight)
            {
                for (int iWidth = 0; iWidth < width; ++iWidth)
                {
                    if (RectTransformUtility.RectangleContainsScreenPoint(aGrid[iHeight, iWidth].GetComponent<RectTransform>(), vCurrMousePos))
                    {
                        vCellEnd.x = iHeight;
                        vCellEnd.y = iWidth;
                        break;
                    }
                }
            }

            if (IsAreaValid(vCellStart, vCellEnd))
            {
                Color areaColor = UnityEngine.Random.ColorHSV();
                // TODO draw a rectangle around cells vs change cell color ?
                for (int i = Mathf.Min(vCellStart.x, vCellEnd.x); i <= Mathf.Max(vCellStart.x, vCellEnd.x); ++i)
                {
                    for (int j = Mathf.Min(vCellStart.y, vCellEnd.y); j <= Mathf.Max(vCellStart.y, vCellEnd.y); ++j)
                    {
                        Cell cell = aGrid[i, j].GetComponent<Cell>();
                        cell.GetComponent<SpriteRenderer>().color = areaColor;
                        cell.SetAreaId(areaCounter);
                    }
                }

                areaCounter++;

                CheckEndCondition();
            }
        }
    }

    private void CheckEndCondition()
    {
        //TODO add not-int-area counter instead
        for (int iHeight = 0; iHeight < height; ++iHeight)
        {
            for (int iWidth = 0; iWidth < width; ++iWidth)
            {
                Cell cell = aGrid[iHeight, iWidth].GetComponent<Cell>();
                if (!cell.IsInArea())
                    return;
            }
        }

        // Grid is ended
        // TODO - Button to generate next grid
        Debug.Log("GRID ENDED, GGWP");
    }

    private bool IsAreaValid(Vector2Int vStart, Vector2Int vEnd)
    {
        int nOrigin = 0;

        //TODO Compute without loop ?
        int x = 0;
        for (int i = Mathf.Min(vStart.x, vEnd.x); i <= Mathf.Max(vStart.x, vEnd.x); ++i, ++x) ;
        int y = 0;
        for (int j = Mathf.Min(vStart.y, vEnd.y); j <= Mathf.Max(vStart.y, vEnd.y); ++j, ++y) ;
        int nCells = x * y;
        //Debug.Log(nCells);

        for (int i = Mathf.Min(vStart.x, vEnd.x); i <= Mathf.Max(vStart.x, vEnd.x); ++i)
        {
            for (int j = Mathf.Min(vStart.y, vEnd.y); j <= Mathf.Max(vStart.y, vEnd.y); ++j)
            {
                Cell cell = aGrid[i, j].GetComponent<Cell>();

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
                }
            }
        }

        return nOrigin != 0;
    }
}
