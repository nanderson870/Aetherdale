using System;
using UnityEngine;

public class ModularFortressPiece : MonoBehaviour
{
    public int width = 1;
    public int length = 1;
    public int height = 1;

    public Vector2 chunkCoords = Vector2.zero;
    public Vector3 startGridPos = Vector3.zero;
    public Vector3 endGridPos = Vector3.zero;

    public bool Adjacent(ModularFortressPiece p2)
    {
        // Adjacent
        return Mathf.Abs(chunkCoords.x - p2.chunkCoords.x) + Mathf.Abs(chunkCoords.y - p2.chunkCoords.x) == 1;

    }

    public bool InLine(ModularFortressPiece p2)
    {
        return chunkCoords.x == p2.chunkCoords.x ^ chunkCoords.y == p2.chunkCoords.y;
    }

    public bool ContainsGridPoint(Vector3 gridPos)
    {
        return gridPos.x >= startGridPos.x && gridPos.x <= endGridPos.x
            && gridPos.z >= startGridPos.z && gridPos.z <= endGridPos.z;
    }
}