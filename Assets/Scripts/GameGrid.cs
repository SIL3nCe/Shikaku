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

    void Start()
    {
        aGrid = new GameObject[height, width];

        int cellSize = 10;
        int x = 0, y = 0;
        for (int iHeight = 0; iHeight < height; ++iHeight)
        {
            for (int iWidth = 0; iWidth < width; ++iWidth)
            {
                aGrid[iHeight, iWidth] = Instantiate(cellPrefab, new Vector3(x, y, 0), Quaternion.identity);
                aGrid[iHeight, iWidth].GetComponent<Cell>().Initialize(iHeight, iWidth, cellSize);
                x += cellSize;
            }
            x = 0;
            y -= cellSize;
        }
    }

    // Update is called once per frame
    //void Update()
    //{
    //    
    //}
}
