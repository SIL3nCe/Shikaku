using System;
using System.Collections.Generic;
using UnityEngine;

class Resolver
{
    // Height = i (y), Width = j (x)

    private List<int[,]> aGridList;
    private int width;
    private int height;

    void Display(int[,] aBaseGrid)
    {
        string strGrid = "\n";

        for (int iHeight = 0; iHeight < height; ++iHeight)
        {
            for (int iWidth = 0; iWidth < width; ++iWidth)
            {
                strGrid += aBaseGrid[iHeight, iWidth];
                strGrid += " ";
            }
            strGrid += System.Environment.NewLine;
        }
        Debug.Log(strGrid);
    }

    public void Resolve(int _width, int _height, int[,] aBaseGrid)
    {
        aGridList = new List<int[,]>();

        width = _width;
        height = _height;

        aGridList.Add(aBaseGrid);

        Display(aBaseGrid);
    }
}
