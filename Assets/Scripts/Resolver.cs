using System;
using System.Collections.Generic;
using UnityEngine;

// Height (line) = i (x), Width (column) = j (y)

class Resolver
{
    private List<GridModel> m_aGridList;
    private int m_iWidth;
    private int m_iHeight;

    bool IsValidArea(int[,] aGrid, Area area, int x, int y, int width, int height)
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
                if (i == area.x && j == area.y)
                    continue;

                if (aGrid[i, j] != 0)
                    return false;
            }
        }

        return true;
    }

    void AddValidArea(GridModel aGrid, int areaId, int width, int height)
    {
        // Height (line) = i (x), Width (column) = j (y)

        //Display(aGrid);
        //Debug.Log("test with area (x,y) " + height + " " + width);

        Area area = aGrid.m_aAreaList[areaId];

        int startX = area.x - (height - 1);
        int startY = area.y - (width - 1);

        //Debug.Log("area x : y " + area.x + " " + area.y);
        //Debug.Log("start x : y " + startX + " " + startY);

        for (int currX = startX; currX < startX + height; ++currX)
        {
            for (int currY = startY; currY < startY + width; ++currY)
            {
                //Debug.Log("Try top left: " + currX + " " + currY);
                //Debug.Log(aGrid.ToString());
                if (IsValidArea(aGrid.m_aCells, area, currX, currY, width, height))
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
                            newGrid.m_aCells[k, l] = area.value;
                        }
                    }

                    newGrid.m_aAreaList[areaId].startX = currX;
                    newGrid.m_aAreaList[areaId].startY = currY;
                    newGrid.m_aAreaList[areaId].width = width;
                    newGrid.m_aAreaList[areaId].height= height;

                    // Push in global grid list
                    //Debug.Log("Add new solution: ");
                    //Debug.Log(newGrid.ToString());
                    m_aGridList.Add(newGrid);
                }
            }
        }
    }

    void AddAllValidAreas(GridModel aGrid, int areaId, int sideLength1, int sideLength2)
    {
        AddValidArea(aGrid, areaId, sideLength1, sideLength2);

        if (sideLength1 != sideLength2)
        {
            AddValidArea(aGrid, areaId, sideLength2, sideLength1);
        }
    }

    void GenerateGridsFromOrigin(GridModel aGrid, int areaId)
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
        int iLastMultiple = aGrid.m_aAreaList[areaId].value;
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
                    
                    AddAllValidAreas(aGrid, areaId, iLastMultiple, currentMultiple);

                    aTestedSizes.Add(tuple);

                    break;
                }
            }
            if (iDivisorIndex == 4)
                break;
        }
    }

    public void Resolve(GridModel aBaseGrid)
    {
        m_aGridList = new List<GridModel>();
        m_aGridList.Add(aBaseGrid);

        m_iWidth = aBaseGrid.m_iWidth;
        m_iHeight = aBaseGrid.m_iHeight;

        // Run
        int nArea = aBaseGrid.m_aAreaList.Count;
        for (int iArea = 0; iArea < nArea; ++iArea)
        {
            int nSolutions = m_aGridList.Count;

            Debug.Log("Start with " + aBaseGrid.m_aAreaList[iArea] + " (" + iArea + "), solutions: " + nSolutions);

            if (nSolutions > 200000)
            {
                Debug.Log("Aborted resolver, more than 200k solutions");
                m_aGridList.Clear();
                break;
            }

            for (int iGrid = 0; iGrid < nSolutions; ++iGrid)
            {
                // Generate grid cases with current iArea
                GenerateGridsFromOrigin(m_aGridList[iGrid], iArea);
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

    public int CheckGridFeasbility(GridModel aBaseGrid)
    {
        for (int i = 0; i < m_aGridList.Count; ++i)
        {
            int iArea = 0;
            int nArea = aBaseGrid.m_aAreaList.Count;
            for (; iArea < nArea; ++iArea)
            {
                Area baseArea = aBaseGrid.m_aAreaList[iArea];
                Area otherArea = m_aGridList[i].m_aAreaList[iArea];

                // !IsSameArea means not the same grid
                // startX != -1 means area has been validated
                if (!baseArea.IsSameArea(otherArea)
                    || (-1 != baseArea.startX && !baseArea.HasSameCompletion(otherArea)))
                {
                    break;
                }
            }

            if (iArea == nArea)
                return i;
        }

        return -1;
    }

    public Area GetCompletedArea(int solutionId, int areaId)
    {
        return m_aGridList[solutionId].m_aAreaList[areaId];
    }
}
