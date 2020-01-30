using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell : MonoBehaviour
{
    private Vector2Int vCoordinates;
    private GameGrid gridObject;

    private bool bIsInArea;
    private int areaSize; // Define if is an "area origin" cell

    public void Initialize(int x, int y, float size, GameGrid grid)
    {
        bIsInArea = false;
        areaSize = Random.Range(-7, 7);

        vCoordinates = new Vector2Int(x, y);
        gameObject.transform.localScale = new Vector3(size, size, 1);

        var text = GetComponentInChildren<TextMesh>();
        if (text != null)
            text.text = areaSize.ToString();

        gridObject = grid;
    }

    void OnMouseDown()
    {
        gridObject.BeginSelection(vCoordinates);
    }

    void OnMouseUp()
    {
        gridObject.StopSelection();
    }

    public void SetIsInArea(bool bValue)
    {
        bIsInArea = bValue;
    }

    public bool IsInArea()
    {
        Debug.Log("IsInArea " + bIsInArea);
        return bIsInArea;
    }

    public int GetAreaOriginValue()
    {
        return areaSize;
    }

    public bool IsAreaOrigin()
    {
        return areaSize > 0;
    }
}
