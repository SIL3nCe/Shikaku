using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell : MonoBehaviour
{
    //TODO Make an enum for empty/already in a rectangle/origin case
    private Vector2Int coordinates;
    
    public void Initialize(int x, int y, int size)
    {
        coordinates = new Vector2Int(x, y);
    }
}
