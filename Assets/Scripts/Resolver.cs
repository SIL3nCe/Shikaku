using System;
using System.Collections.Generic;
using UnityEngine;

class Resolver
{
    // Height (line) = i (x), Width (column) = j (y)

    class SPoint : IComparable
    {
        public int x; // Height (i)
        public int y; // Width (j)
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

    private List<SPoint> m_aSPointList;

    private List<int[,]> m_aGridList;
    private int m_iWidth;
    private int m_iHeight;

    void Display(int[,] aBaseGrid)
    {
        string strGrid = "Display Grid\n";

        for (int iHeight = 0; iHeight < m_iHeight; ++iHeight)
        {
            for (int iWidth = 0; iWidth < m_iWidth; ++iWidth)
            {
                strGrid += aBaseGrid[iHeight, iWidth];
                strGrid += " ";
            }
            strGrid += System.Environment.NewLine;
        }
        Debug.Log(strGrid);
    }

    bool IsValidArea(int [,] aGrid, SPoint sPoint, int x, int y, int width, int height)
    {
        // Height (line) = i (x), Width (column) = j (y)

        if (x < 0 
            || x + height > m_iHeight 
            || y < 0 
            || y + width > m_iWidth)
            return false;

        // loop on area, check if empty
        for (int i = x; i < x + height; ++i)
        {
            for (int j = y; j < y + width; ++j)
            {
                if (i == sPoint.x && j == sPoint.y)
                    continue;

                if (aGrid[i, j] != 0)
                    return false;
            }
        }

        return true;
    }

    void AddValidArea(int[,] aGrid, SPoint sPoint, int width, int height)
    {
        // Height (line) = i (x), Width (column) = j (y)

        //Display(aGrid);
        Debug.Log("test with area (x,y) " + height + " " + width);

        int startX = sPoint.x - height;
        int startY = sPoint.y - width;

        Debug.Log("spoint x : y " + sPoint.x + " " + sPoint.y);
        Debug.Log("start x : y " + startX + " " + startY);

        for (int currX = startX; currX <= startX + height; ++currX)
        {
            for (int currY = startY; currY <= startY + width; ++currY)
            {
                Debug.Log("Try top left: " + currX + " " + currY);
                if (IsValidArea(aGrid, sPoint, currX, currY, width, height))
                {
                    Debug.Log("Found valid area");
                    // Copy grid in a new grid
                    int[,] aValidGrid = aGrid;

                    // Update new grid cells for valid area
                    for (int k = currX; k < currX + height; ++k)
                    {
                        for (int l = currY; l < currY + width; ++l)
                        {
                            //Debug.Log(k + " " + l + " " + (height - 1) + " " + (width - 1));
                            aValidGrid[k, l] = sPoint.areaValue;
                        }
                    }
                    Display(aValidGrid);
                    
                    // Push in global grid list
                    m_aGridList.Add(aValidGrid);
                }
            }
        }
    }

    void AddAllValidAreas(int[,] aGrid, SPoint sPoint, int sideLength1, int sideLength2)
    {
         AddValidArea(aGrid, sPoint, sideLength1, sideLength2);

        if (sideLength1 != sideLength2)
        {
            AddValidArea(aGrid, sPoint, sideLength2, sideLength1);
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
        m_aGridList = new List<int[,]>();
        m_aSPointList = new List<SPoint>();

        m_iWidth = _width;
        m_iHeight = _height;

        m_aGridList.Add(aBaseGrid);

        // Init SPoint list
        {
            for (int i = 0; i < m_iHeight; ++i)
            {
                for (int j = 0; j < m_iWidth; ++j)
                {
                    if (0 != aBaseGrid[i, j])
                    {
                        SPoint sPoint = new SPoint
                        {
                            x = i,
                            y = j,
                            areaValue = aBaseGrid[i, j]
                        };

                        m_aSPointList.Add(sPoint);
                    }
                }
            }

            m_aSPointList.Sort();

            // Debug Display sorted list of SPoints
            //for (int i = 0; i < aSPointList.Count; ++i)
            //{
            //    Debug.Log(aSPointList[i]);
            //}
        }

        // Run
        for (int iSPoint = 0; iSPoint < m_aSPointList.Count; ++iSPoint)
        {
            Debug.Log("Start with " + m_aSPointList[iSPoint]);

            int nSolutions = m_aGridList.Count;
            for (int iGrid = 0; iGrid < nSolutions; ++iGrid)
            {
                // Generate grid cases with current SPoint
                GenerateGridsFromSPoint(m_aGridList[iGrid], m_aSPointList[iSPoint]);
            }

            // Remove old grids using nSolutions and new list sizes
        }

        for (int i = 0; i < m_aGridList.Count; ++i)
        {
            Display(m_aGridList[0]);
        }
    }
}
