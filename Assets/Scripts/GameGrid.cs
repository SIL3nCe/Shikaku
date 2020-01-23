using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameGrid : MonoBehaviour
{
    [Tooltip("Grid size")]
    public int width;
    public int height;

    [Tooltip("Object to instantiate")]
    public GameObject cellPrefab;

    private GameObject[,] aGrid;

    void Start()
    {
        aGrid = new GameObject[width,height];

        int cellSize = 10;
        for (int iHeight = 0; iHeight <= height; iHeight++)
        {
            for (int iWidth = 0; iWidth <= width; ++iWidth)
            {
                aGrid[iHeight, iWidth] = Instantiate(cellPrefab, new Vector3(iWidth, iHeight, 0), Quaternion.identity);
                aGrid[iHeight, iWidth].GetComponent<Cell>().Initialize(iHeight, iHeight, cellSize);
            }
        }
    }


    // Update is called once per frame
    //void Update()
    //{
    //    
    //}
}
