using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Line
{
    public Vector2Int p1, p2;

    public Line(Vector2Int p1, Vector2Int p2)
    {
        this.p1 = p1;
        this.p2 = p2;
    }

    public Vector2Int[] getPoints(int quantity)
    {
        var points = new Vector2Int[quantity];
        int ydiff = p2.y - p1.y, xdiff = p2.x - p1.x;
        int slope = (int)(p2.y - p1.x) / (p2.x - p1.x);
        int x, y;

        --quantity;

        for (int i = 0; i < quantity; i++)
        {
            Debug.Log("Getting points " + i);
            y = slope == 0 ? 0 : ydiff * (i / quantity);
            x = slope == 0 ? xdiff * (i / quantity) : y / slope;
            points[(int)i] = new Vector2Int((int)Mathf.Round(x) + p1.x, (int)Mathf.Round(y) + p1.y);
        }

        points[quantity] = p2;
        return points;
    }
}
