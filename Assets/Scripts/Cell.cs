using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell : MonoBehaviour
{
    //TODO Make an enum for empty/already in a rectangle/origin case
    private Vector2Int coordinates;
    
    public void Initialize(int x, int y, float size)
    {
        coordinates = new Vector2Int(x, y);
        gameObject.transform.localScale = new Vector3(size, size, 1);

        var text = GetComponentInChildren<TextMesh>();
        if (text != null)
            text.text = string.Format("ntm");
    }
}
