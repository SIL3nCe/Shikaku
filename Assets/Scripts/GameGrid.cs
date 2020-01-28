using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameGrid : MonoBehaviour
{
    [Tooltip("Grid size")]
    public int width;
    public int height;

    [Tooltip("Cell object to instantiate")]
    public GameObject cellPrefab;

    private GameObject[,] aGrid;

    private bool bSelection;
    private Vector3 vSelectionStart;
    private Vector2Int vCellStart;

    void Start()
    {
        aGrid = new GameObject[height, width];

        // Set grid center in 0,0
        float fTopLeftX = 0.5f - (width * 0.5f);
        float fTopLeftY = -0.5f + height * 0.5f;

        int cellSize = 1;
        float x = fTopLeftX, y = fTopLeftY;
        for (int iHeight = 0; iHeight < height; ++iHeight)
        {
            for (int iWidth = 0; iWidth < width; ++iWidth)
            {
                aGrid[iHeight, iWidth] = Instantiate(cellPrefab, new Vector3(x, y, 0), Quaternion.identity);
                aGrid[iHeight, iWidth].GetComponent<Cell>().Initialize(iHeight, iWidth, 1.0f, this);
                x += cellSize;
            }
            x = fTopLeftX;
            y -= cellSize;
        }
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
    
    public void BeginSelection(Vector2Int cellCoord)
    {
        bSelection = true;
        vCellStart = cellCoord;
        vSelectionStart = Input.mousePosition;
        vSelectionStart.y = Camera.main.pixelHeight - vSelectionStart.y;
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
                // TODO draw a rectangle around cells vs change cell color ?
                for (int i = Mathf.Min(vCellStart.x, vCellEnd.x); i <= Mathf.Max(vCellStart.x, vCellEnd.x); ++i)
                {
                    for (int j = Mathf.Min(vCellStart.y, vCellEnd.y); j <= Mathf.Max(vCellStart.y, vCellEnd.y); ++j)
                    {
                        Cell cell = aGrid[i, j].GetComponent<Cell>();
                        cell.GetComponent<SpriteRenderer>().color = Color.red;
                        cell.SetIsInArea();
                    }
                }
            }
        }
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
                if (cell.IsAreaOrigin())
                {
                    // More than one origin cell in area
                    if (++nOrigin != 1)
                        return false;

                    // Too much/not enough cells in area based on origin cell value
                    if (cell.GetAreaOriginValue() != nCells)
                        return false;

                    // A cell in area is already in another area
                    if (cell.IsInArea())
                        return false;
                }
            }
        }

        return nOrigin != 0;
    }
}
