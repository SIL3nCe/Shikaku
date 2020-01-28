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

            Debug.Log(vCellEnd);

            // TODO Check if area is valid based on cells on it
            //aGrid[vCellStart.x, vCellStart.y];
            //aGrid[vCellEnd.x, vCellEnd.y];

            // TODO draw a rectangle around cells if area is valid
        }
    }
}
