using System;
using System.Collections.Generic;
using UnityEngine;

public class Area : IComparable
{
    // Origin
    public int x; // Height (i)
    public int y; // Width (j)
    public int value;

    // Values found by player or solver
    public int startX, startY;
    public int width, height;

    public Area(int _x, int _y, int _value, int _startX = -1, int _startY = -1, int _width = -1, int _height = -1)
    {
        x = _x;
        y = _y;
        value = _value;
        startX = _startX;
        startY = _startY;
        width = _width;
        height = _height;
    }

    public Area(Area other)
    {
        x = other.x;
        y = other.y;
        value = other.value;
        startX = other.startX;
        startY = other.startY;
        width = other.width;
        height = other.height;
    }

    public void Reset()
    {
        startX = startY = width = height = -1;
    }

    public int CompareTo(object Other)
    {
        Area otherPoint = Other as Area;
        // Biggest first
        if (this.value > otherPoint.value)
            return -1;

        // Manhattan dist to 0,0
        //if ((x + y) <= (otherPoint.x + otherPoint.y))
        //    return -1;

        return 1;
    }

    public bool IsSameArea(Area Other)
    {
        if (this.value == Other.value
            && this.x == Other.x
            && this.y == Other.y)
            return true;

        return false;
    }

    public bool HasSameCompletion(Area Other)
    {
        if (this.startX == Other.startX
            && this.startY == Other.startY
            && this.height == Other.height
            && this.width == Other.width)
            return true;

        return false;
    }

    public bool IsCompleted()
    {
        return startX != -1;
    }

    public override string ToString()
    {
        return "SPoint: (" + y + "," + x + ") : " + value;
    }
};

class GridModel
{
    // Height (line) = i (x), Width (column) = j (y)

    public int m_iWidth;
    public int m_iHeight;

    public List<Area> m_aAreaList;

    public int[,] m_aCells;

    public GridModel(int width, int height)
    {
        m_iWidth = width;
        m_iHeight = height;
        m_aCells = new int[m_iHeight, m_iWidth];
        m_aAreaList = new List<Area>();
    }
    public GridModel(GridModel other)
    {
        m_iWidth = other.m_iWidth;
        m_iHeight = other.m_iHeight;
        m_aAreaList = other.m_aAreaList.ConvertAll(area => new Area(area));
        m_aCells = other.m_aCells.Clone() as int[,];
    }

    public override string ToString()
    {
        string strGrid = "Display Grid\n";

        for (int iHeight = 0; iHeight < m_iHeight; ++iHeight)
        {
            for (int iWidth = 0; iWidth < m_iWidth; ++iWidth)
            {
                strGrid += m_aCells[iHeight, iWidth];
                strGrid += " ";
            }
            strGrid += System.Environment.NewLine;
        }
        return strGrid;
    }

    public int GetFirstDifferentAreaId(GridModel Other)
    {
        int nArea = m_aAreaList.Count;
        for (int i = 0; i < nArea; ++i)
        {
            if (!m_aAreaList[i].IsSameArea(Other.m_aAreaList[i]))
            {
                return -2; // Not the same grid
            }

            if (!m_aAreaList[i].HasSameCompletion(Other.m_aAreaList[i]))
            {
                return i;
            }
        }

        // Nothing different
        return -1;
    }

    public int GetAreaId(int x, int y)
    {
        int nArea = m_aAreaList.Count;
        for (int i = 0; i < nArea; ++i)
        {
            if (m_aAreaList[i].x == x && m_aAreaList[i].y == y)
                return i;
        }

        return -1;
    }
}