using System;
using System.Collections.Generic;
using UnityEngine;

class Resolver
{
    // Height = i (y), Width = j (x)

    class SPoint : IComparable
    {
        public int y; // Height (i)
        public int x; // Width (j)
        public int areaValue;

        public int CompareTo(object Other)
        {
            SPoint otherPoint = Other as SPoint;
            if (this.areaValue > otherPoint.areaValue)
                return -1;

            return 1;
        }

        public override string ToString()
        {
            return "SPoint: (" + y + "," + x + ") : " + areaValue;
        }
    };

    private List<SPoint> aSPointList;

    private List<int[,]> aGridList;
    private int width;
    private int height;

    void Display(int[,] aBaseGrid)
    {
        string strGrid = "Display Grid\n";

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

    //TODO AddValidArea

    void AddAllValidAreas(int[,] aGrid, SPoint sPoint, int width, int height)
    {
        Debug.Log("AddAllValidAreas: " + width + " " + height);

        //AddValidArea(currentGrid, sPoint, width, height);

        if (width != height)
        {
            //AddValidArea(currentGrid, sPoint, height, width);
        }
    }

    void GenerateGridsFromSPoint(int[,] aGrid, SPoint sPoint)
    {
        List<int> aDivisors = new List<int>{ 2, 3, 5, 7 };
        int currentMultiple = 1;
        int iLastMultiple = sPoint.areaValue;
        while (true)
        {
            int iDivisorIndex = 0;
            for (; iDivisorIndex < 4; ++iDivisorIndex)
            {
                if (0 == iLastMultiple % aDivisors[iDivisorIndex])
                {
                    iLastMultiple = iLastMultiple / aDivisors[iDivisorIndex];
                    currentMultiple *= aDivisors[iDivisorIndex];
                    AddAllValidAreas(aGrid, sPoint, iLastMultiple, currentMultiple);
                    break;
                }
            }
            if (iDivisorIndex == 4)
                break;
        }
    }

    public void Resolve(int _width, int _height, int[,] aBaseGrid)
    {
        aGridList = new List<int[,]>();
        aSPointList = new List<SPoint>();

        width = _width;
        height = _height;

        aGridList.Add(aBaseGrid);

        // Init SPoint list
        {
            for (int iHeight = 0; iHeight < height; ++iHeight)
            {
                for (int iWidth = 0; iWidth < width; ++iWidth)
                {
                    if (0 != aBaseGrid[iHeight, iWidth])
                    {
                        SPoint sPoint = new SPoint
                        {
                            y = iWidth,
                            x = iHeight,
                            areaValue = aBaseGrid[iHeight, iWidth]
                        };

                        aSPointList.Add(sPoint);
                    }
                }
            }

            aSPointList.Sort();

            // Debug Display sorted list of SPoints
            //for (int i = 0; i < aSPointList.Count; ++i)
            //{
            //    Debug.Log(aSPointList[i]);
            //}
        }

        // Run
        for (int iSPoint = 0; iSPoint < aSPointList.Count; ++iSPoint)
        {
            Debug.Log("Start with " + aSPointList[iSPoint]);

            int nSolutions = aGridList.Count;
            for (int iGrid = 0; iGrid < nSolutions; ++iGrid)
            {
                // Generate grid cases with current SPoint
                GenerateGridsFromSPoint(aGridList[iGrid], aSPointList[iSPoint]);
            }

            // Remove old grids using nSolutions and new list sizes
        }

        Display(aBaseGrid);
    }
}
