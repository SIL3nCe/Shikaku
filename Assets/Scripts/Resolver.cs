using System;
using System.Collections.Generic;
using UnityEngine;

// Height (line) = i (x), Width (column) = j (y)

class Resolver
{
    private List<Area> m_aSPointList;

    private List<GridModel> m_aGridList;
    private int m_iWidth;
    private int m_iHeight;

    bool IsValidArea(int[,] aGrid, Area sPoint, int x, int y, int width, int height)
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

    void AddValidArea(GridModel aGrid, Area sPoint, int width, int height)
    {
        // Height (line) = i (x), Width (column) = j (y)

        //Display(aGrid);
        //Debug.Log("test with area (x,y) " + height + " " + width);

        int startX = sPoint.x - (height - 1);
        int startY = sPoint.y - (width - 1);

        //Debug.Log("spoint x : y " + sPoint.x + " " + sPoint.y);
        //Debug.Log("start x : y " + startX + " " + startY);

        for (int currX = startX; currX < startX + height; ++currX)
        {
            for (int currY = startY; currY < startY + width; ++currY)
            {
                //Debug.Log("Try top left: " + currX + " " + currY);
                //Debug.Log(aGrid.ToString());
                if (IsValidArea(aGrid.m_aCells, sPoint, currX, currY, width, height))
                {
                    //Debug.Log("Found valid area");

                    // Copy grid
                    GridModel newGrid = aGrid.DeepCopy();

                    // Update new grid cells for valid area
                    for (int k = currX; k < currX + height; ++k)
                    {
                        for (int l = currY; l < currY + width; ++l)
                        {
                            //Debug.Log(k + " " + l + " " + (height - 1) + " " + (width - 1));
                            newGrid.m_aCells[k, l] = sPoint.areaValue;
                        }
                    }

                    // Push in global grid list
                    //Debug.Log("Add new solution: ");
                    //Debug.Log(newGrid.ToString());
                    m_aGridList.Add(newGrid);
                }
            }
        }
    }

    void AddAllValidAreas(GridModel aGrid, Area sPoint, int sideLength1, int sideLength2)
    {
        AddValidArea(aGrid, sPoint, sideLength1, sideLength2);

        if (sideLength1 != sideLength2)
        {
            AddValidArea(aGrid, sPoint, sideLength2, sideLength1);
        }
    }

    void GenerateGridsFromSPoint(GridModel aGrid, Area sPoint)
    {
        List<Tuple<int, int>> aTestedSizes = new List<Tuple<int, int>>();
        
        Func<Tuple<int, int>, bool> AlreadyTested = tuple =>
        {
            int nTuple = aTestedSizes.Count;
            for (int i = 0; i < nTuple; ++i)
            {
                if ((aTestedSizes[i].Item1 == tuple.Item1 && aTestedSizes[i].Item2 == tuple.Item2)
                || (aTestedSizes[i].Item1 == tuple.Item2 && aTestedSizes[i].Item2 == tuple.Item1))
                    return true;
            }
            return false;
        };

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

                    // Check if tuple has already been tested. Needed for case like 8 (> 4,2 > 2,4)
                    Tuple<int, int> tuple = new Tuple<int, int>(iLastMultiple, currentMultiple);
                    if (AlreadyTested(tuple))
                        break;
                    
                    AddAllValidAreas(aGrid, sPoint, iLastMultiple, currentMultiple);

                    aTestedSizes.Add(tuple);

                    break;
                }
            }
            if (iDivisorIndex == 4)
                break;
        }
    }

    public void Resolve(int _width, int _height, GridModel aBaseGrid)
    {
        m_aGridList = new List<GridModel>();
        m_aSPointList = aBaseGrid.m_aAreaList;
        m_aSPointList.Sort();

        m_iWidth = _width;
        m_iHeight = _height;

        m_aGridList.Add(aBaseGrid);

        // Run
        for (int iSPoint = 0; iSPoint < m_aSPointList.Count; ++iSPoint)
        {
            //Debug.Log("Start with " + m_aSPointList[iSPoint]);

            int nSolutions = m_aGridList.Count;
            for (int iGrid = 0; iGrid < nSolutions; ++iGrid)
            {
                // Generate grid cases with current SPoint
                GenerateGridsFromSPoint(m_aGridList[iGrid], m_aSPointList[iSPoint]);
            }

            // Remove old grids using nSolutions and new list sizes
            m_aGridList.RemoveRange(0, nSolutions);
        }

        Debug.Log("Solutions: " + m_aGridList.Count);
        for (int i = 0; i < m_aGridList.Count; ++i)
        {
            Debug.Log(m_aGridList[i].ToString());
        }
    }
}
