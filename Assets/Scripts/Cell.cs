using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell : MonoBehaviour
{
    private Vector2Int vCoordinates;
    private GameGrid gridObject;

    public int areaId { get; set; }
    private int areaSize; // Define if is an "area origin" cell

    public void Initialize(int x, int y, float size, GameGrid grid)
    {
        areaId = -1;
        areaSize = 0;

        vCoordinates = new Vector2Int(x, y);
        gameObject.transform.localScale = new Vector3(size, size, 1);

        TextMesh text = GetComponentInChildren<TextMesh>();
        if (text != null)
            text.text = "";

        gridObject = grid;
    }

    void OnMouseDown()
    {
        gridObject.BeginSelection(vCoordinates, areaId);
    }

    void OnMouseUp()
    {
        gridObject.StopSelection();
    }

    public bool IsInArea()
    {
        return areaId >= 0;
    }

    public bool IsInGivenArea(int _areaId)
    {
        return areaId == _areaId;
    }

    public int GetAreaOriginValue()
    {
        return areaSize;
    }

    public bool IsAreaOrigin()
    {
        return areaSize > 0;
    }

    public void SetAreaSize(int value)
    {
        areaSize = value; 
        TextMesh text = GetComponentInChildren<TextMesh>();
        if (text != null)
            text.text = areaSize.ToString();
    }
}
