using System;
using System.Collections.Generic;
using UnityEngine;

// Height (line) = i (x), Width (column) = j (y)

class Resolver
{
    private List<GridModel> m_aGrids;
    private List<GridModel> m_aGeneratedGrids;
    private int m_iWidth;
    private int m_iHeight;

    List<int> m_aDivisors;

    bool IsValidArea(in int[,] aGrid, in Area area, int x, int y, int width, int height)
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

    void AddValidArea(int iGrid, int areaId, int width, int height)
    {
        // Height (line) = i (x), Width (column) = j (y)

        //Display(m_aGridList[iGrid]);
        //Debug.Log("test with area (x,y) " + height + " " + width);

        int startX = m_aGrids[iGrid].m_aAreaList[areaId].x - (height - 1);
        int startY = m_aGrids[iGrid].m_aAreaList[areaId].y - (width - 1);

        //Debug.Log("area x : y " + area.x + " " + area.y);
        //Debug.Log("start x : y " + startX + " " + startY);

        for (int currX = startX; currX < startX + height; ++currX)
        {
            for (int currY = startY; currY < startY + width; ++currY)
            {
                //Debug.Log("Try top left: " + currX + " " + currY);
                //Debug.Log(aGrid.ToString());
                if (IsValidArea(m_aGrids[iGrid].m_aCells, m_aGrids[iGrid].m_aAreaList[areaId], currX, currY, width, height))
                {
                    //Debug.Log("Found valid area");

                    // Copy grid
                    GridModel newGrid = new GridModel(m_aGrids[iGrid]);

                    // Update new grid cells for valid area
                    for (int k = currX; k < currX + height; ++k)
                    {
                        for (int l = currY; l < currY + width; ++l)
                        {
                            //Debug.Log(k + " " + l + " " + (height - 1) + " " + (width - 1));
                            newGrid.m_aCells[k, l] = m_aGrids[iGrid].m_aAreaList[areaId].value;
                        }
                    }

                    newGrid.m_aAreaList[areaId].startX = currX;
                    newGrid.m_aAreaList[areaId].startY = currY;
                    newGrid.m_aAreaList[areaId].width = width;
                    newGrid.m_aAreaList[areaId].height= height;

                    // Push in global grid list
                    //Debug.Log("Add new solution: ");
                    //Debug.Log(newGrid.ToString());
                    m_aGeneratedGrids.Add(newGrid);
                }
            }
        }
    }

    void AddAllValidAreas(int iGrid, int areaId, int sideLength1, int sideLength2)
    {
        AddValidArea(iGrid, areaId, sideLength1, sideLength2);

        if (sideLength1 != sideLength2)
        {
            AddValidArea(iGrid, areaId, sideLength2, sideLength1);
        }
    }

    void GenerateGridsFromOrigin(int iGrid, int areaId)
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

        int currentMultiple = 1;
        int iLastMultiple = m_aGrids[iGrid].m_aAreaList[areaId].value;
        while (true)
        { 
            int iDivisorIndex = 0;
            for (; iDivisorIndex < m_aDivisors.Count; ++iDivisorIndex)
            {
                if (0 == iLastMultiple % m_aDivisors[iDivisorIndex])
                {
                    iLastMultiple /= m_aDivisors[iDivisorIndex];
                    currentMultiple *= m_aDivisors[iDivisorIndex];

                    // Check if tuple has already been tested. Needed for case like 8 (> 4,2 > 2,4)
                    Tuple<int, int> tuple = new Tuple<int, int>(iLastMultiple, currentMultiple);
                    if (AlreadyTested(tuple))
                        break;
                    
                    AddAllValidAreas(iGrid, areaId, iLastMultiple, currentMultiple);

                    aTestedSizes.Add(tuple);

                    break;
                }
            }

            if (iDivisorIndex == m_aDivisors.Count)
            {
                // Handle prime numbers not in divisors list
                if (iLastMultiple != 1)
                {
                    AddAllValidAreas(iGrid, areaId, iLastMultiple, currentMultiple);
                }

                break;
            }

        }
    }


    public void Resolve(in GridModel aBaseGrid)
    {
        //TODO Optimize for large grids
        if (aBaseGrid.m_iHeight > 10 || aBaseGrid.m_iWidth > 10)
            return;

        m_aGeneratedGrids = new List<GridModel>();
        m_aGrids = new List<GridModel>();
        m_aGrids.Add(aBaseGrid);

        Debug.Log(aBaseGrid.ToString());

        m_iWidth = aBaseGrid.m_iWidth;
        m_iHeight = aBaseGrid.m_iHeight;

        m_aDivisors = new List<int> { 2, 3, 5 };

        // Generate very valid grids for each origins
        int nArea = aBaseGrid.m_aAreaList.Count;
        for (int iArea = 0; iArea < nArea; ++iArea)
        {
            int nSolutions = m_aGrids.Count;

            Debug.Log("Start with " + aBaseGrid.m_aAreaList[iArea] + " (" + iArea + "), solutions: " + nSolutions);

            if (nSolutions > 100000)
            {
                Debug.Log("Aborted resolver, too much solutions: " + nSolutions);
                m_aGrids.Clear();
                break;
            }

            for (int iGrid = 0; iGrid < nSolutions; ++iGrid)
            {
                // Generate grid cases with current iArea
                GenerateGridsFromOrigin(iGrid, iArea);
            }

            // Remove old grids using nSolutions and new list sizes
            m_aGrids.RemoveRange(0, nSolutions);
            m_aGrids.AddRange(m_aGeneratedGrids);
            m_aGeneratedGrids.Clear();
        }

        Debug.Log("Solutions: " + m_aGrids.Count);
        for (int i = 0; i < m_aGrids.Count; ++i)
        {
            Debug.Log(m_aGrids[i].ToString());
        }
    }


    public int CheckGridFeasbility(in GridModel aBaseGrid)
    {
        for (int i = 0; i < m_aGrids.Count; ++i)
        {
            int iArea = 0;
            int nArea = aBaseGrid.m_aAreaList.Count;
            for (; iArea < nArea; ++iArea)
            {
                Area baseArea = aBaseGrid.m_aAreaList[iArea];
                Area otherArea = m_aGrids[i].m_aAreaList[iArea];

                // !IsSameArea means not the same grid
                if (!baseArea.IsSameArea(otherArea)
                    || (baseArea.IsCompleted() && !baseArea.HasSameCompletion(otherArea)))
                {
                    break;
                }
            }

            if (iArea == nArea)
                return i;
        }

        return -1;
    }

    public void SetSolutionToUse(in GridModel aGrid)
    {
        m_aGrids.Add(aGrid);
    }

    public Area GetCompletedArea(int solutionId, int areaId)
    {
        return m_aGrids[solutionId].m_aAreaList[areaId];
    }
}
