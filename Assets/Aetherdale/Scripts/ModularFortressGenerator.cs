using System;
using System.Collections.Generic;
using System.Linq;
using Geometry2D;
using UnityEngine;

[System.Serializable]
public class ModularFortressRoomGenerationSettings
{
    public int numberOfRooms = 12;
    public int maxRoomWidth = 8;
    public int minRoomWidth = 4;

    [Tooltip("How many spaces must be between a corner and the nearest hallway on that wall. Minimum of 1")]
    public int cornerMargin = 1;
    public int pathWidth = 4;

    
    public int GetChunkWidth(int mapWidth)
    {
        int chunkWidth = mapWidth;
        while ((chunkWidth / 2) >= maxRoomWidth)
        {
            chunkWidth /= 2;
        }
        Debug.Log($"Chunked width is " + chunkWidth);
        return chunkWidth;
    }

    public bool Validate(int mapWidth)
    {
        if (cornerMargin < 1 || cornerMargin >= (maxRoomWidth - pathWidth) / 2)
        {
            Debug.LogError($"Invalid corner margin - must be at least 1, and less than {(maxRoomWidth - pathWidth) / 2} (basd on max room width)");
            return false;
        }

        if (minRoomWidth < cornerMargin * 2 + pathWidth)
        {
            Debug.LogError("Minimum room width is not enough to accomodate corner margins + path width");
            return false;
        }

        float minPathableOverlap = (minRoomWidth - (GetChunkWidth(mapWidth) / 2)) * 2 - 2 * cornerMargin;
        if (pathWidth > minPathableOverlap)
        {
            Debug.LogError("Room configuration settings invalid - maximum path size is " + minPathableOverlap);
            return false;
        }

        return true;
    }
}

public class ModularFortressGenerator : MonoBehaviour
{
    public class RoomConnection
    {
        public ModularFortressPiece room1;
        public ModularFortressPiece room2;

        public RoomConnection(ModularFortressPiece room1, ModularFortressPiece room2)
        {
            this.room1 = room1;
            this.room2 = room2;
        }
    }


    public int width = 64;
    public int length = 64;
    [SerializeField] Vector3 scale = Vector3.one;

    [SerializeField] ModularFortressRoomGenerationSettings roomGenSettings;

    [SerializeField] GameObject unitCubePrefab;
    [SerializeField] GameObject pathUnitCubePrefab;

    public ModularFortressLayoutData data = new();


    public float GetWidth()
    {
        return width * scale.x;
    }

    public float GetLength()
    {
        return length * scale.z;
    }

    public Vector3 GetOffset()
    {
        return new(-GetWidth() / 2, 0, -GetLength() / 2);
    }

    public Vector3 CoordinateToPosition(float x, float y, float z)
    {
        return transform.position + Vector3.Scale(GetOffset() + new Vector3(x, y, z), scale) + new Vector3(0.5F, 0.5F, 0.5F);
    }

    public List<ModularFortressPiece> GetChildPieces()
    {
        return transform.Cast<Transform>().Select(x => x.GetComponent<ModularFortressPiece>()).ToList();
    }

    public bool IsGridPointFilled(Vector3 gridpoint)
    {
        foreach (var child in GetChildPieces())
        {
            if (child.ContainsGridPoint(gridpoint))
            {
                return true;
            }
        }

        return false;
    }

    List<Line> triangulationLines = new();
    List<Line> mst = new();
    List<RoomConnection> conns = new();
    public void Generate()
    {
        Clear();

        if (!roomGenSettings.Validate(width))
        {
            Debug.LogError("Faild to validate room generation settings");
            return;
        }

        data = new();
        data.grid = new ModularFortressLayoutData.TileType[width, length];

        GenerateRooms();

        DelaunayTriangulator triangulator = GetComponent<DelaunayTriangulator>();

        List<ModularFortressPiece> pieces = GetRoomPieces();

        List<Triangle> triangulation = triangulator.BowyerWatsonTriangulate(pieces.Select(p => p.transform.position).ToList());

        triangulationLines = new();
        foreach (Triangle triangle in triangulation)
        {
            foreach (Line line in triangle.GetLines())
            {
                triangulationLines.Add(line);
            }
        }

        triangulationLines.Sort((Line l1, Line l2) =>
        {
            return l1.GetLength().CompareTo(l2.GetLength());
        });


        mst = MSTBuilder.BuildMST(triangulationLines);

        SetupEndpoints(mst);

        conns = new();
        foreach (Line line in mst)
        {
            var p1 = GetPieceAtPosition(new(line.v0.x, 0, line.v0.y));
            var p2 = GetPieceAtPosition(new(line.v1.x, 0, line.v1.y));

            RoomConnection conn = new(p1, p2);
            conns.Add(conn);

        }

        GeneratePaths(conns);
    }

    List<Line>[] pathsThroughFortress;
    void SetupEndpoints(List<Line> mst)
    {
        List<Vector2> unmappedEndpoints = GetEndPointsOfMST(mst);

        Vector2 startRoomPos = unmappedEndpoints[UnityEngine.Random.Range(0, unmappedEndpoints.Count - 1)];
        unmappedEndpoints.Remove(startRoomPos);

        // TODO set end pos as furthest endpoint from start pos

        Vector2 currentPos = startRoomPos;
        List<Vector2> mappedPoints = new();

        pathsThroughFortress = new List<Line>[unmappedEndpoints.Count];
        int index = 0;
        TracePaths(new(), mst.ToList(), currentPos, mappedPoints, pathsThroughFortress, ref index);

        Debug.Log(pathsThroughFortress.Count() + " LINES MADE");
    }

    private void TracePaths(List<Line> pathSoFar, List<Line> remainingLines, Vector2 currentPos, List<Vector2> mappedPoints, List<Line>[] lines, ref int linesIndex)
    {
        int iterations = 0;
        while (iterations < 1000)
        {

            List<Line> linesFromCurrentPos = GetLinesConnectedToPoint(currentPos, remainingLines);
            for (int i = linesFromCurrentPos.Count - 1; i >= 0; i--)
            {
                Line line = linesFromCurrentPos[i];

                // Remove backwards
                if (mappedPoints.Contains(line.v0) || mappedPoints.Contains(line.v1))
                {
                    linesFromCurrentPos.RemoveAt(i);
                }
            }
            mappedPoints.Add(currentPos);

            if (linesFromCurrentPos.Count > 1)
            {
                foreach (Line line in linesFromCurrentPos)
                {
                    Vector2 next = line.v0;
                    if (line.v0 == currentPos)
                    {
                        next = line.v1;
                    }

                    List<Line> pathSoFarDup = pathSoFar.ToList();
                    pathSoFarDup.Add(new(currentPos, next));

                    TracePaths(pathSoFarDup, remainingLines, next, mappedPoints, lines, ref linesIndex);
                }
                return;
            }
            else if (linesFromCurrentPos.Count == 1)
            {
                Vector2 next = linesFromCurrentPos[0].v0;
                if (linesFromCurrentPos[0].v0 == currentPos)
                {
                    next = linesFromCurrentPos[0].v1;
                }
                pathSoFar.Add(new(currentPos, next));

                currentPos = next;
            }
            else
            {
                lines[linesIndex] = pathSoFar;
                linesIndex++;
                return;
            }

        }
    }

    List<Vector2> GetAllPointsOfMST(List<Line> mst)
    {
        List<Vector2> points = new();
        foreach (Line line in mst)
        {
            points.Add(line.v0);
            points.Add(line.v1);
        }

        return points;
    }

    List<Vector2> GetEndPointsOfMST(List<Line> mst)
    {
        List<Vector2> points = GetAllPointsOfMST(mst);
        return points.Where(x => points.Where(j => j == x).Count() == 1).ToList();
    }

    List<Line> GetLinesConnectedToPoint(Vector2 point, List<Line> mst)
    {
        List<Line> ret = new();
        foreach (Line line in mst)
        {
            if (line.v0 == point || line.v1 == point)
            {
                ret.Add(line);
            }
        }

        return ret;
    }


    public ModularFortressPiece GetPieceAtPosition(Vector3 position)
    {
        foreach (ModularFortressPiece piece in transform.Cast<Transform>().Select(x => x.GetComponent<ModularFortressPiece>()))
        {
            Vector3 pieceStart = CoordinateToPosition(piece.startGridPos.x, piece.startGridPos.y, piece.startGridPos.z);
            Vector3 pieceEnd = CoordinateToPosition(piece.endGridPos.x, piece.endGridPos.y, piece.endGridPos.z);

            if (position.x >= pieceStart.x && position.x <= pieceEnd.x && position.z >= pieceStart.z && position.z <= pieceEnd.z)
            {
                return piece;
            }

        }

        throw new Exception("No piece found at " + position);
    }
    public void GenerateRooms()
    {
        int chunkWidth = roomGenSettings.GetChunkWidth(width);

        int totalArea = width * length;
        int chunkArea = chunkWidth * chunkWidth;
        int numberOfChunks = totalArea / chunkArea;

        if (roomGenSettings.numberOfRooms > numberOfChunks)
        {
            throw new Exception($"More rooms requested than chunks available - maximum is {numberOfChunks}");
        }

        int chunksSquared = (int)Mathf.Sqrt(numberOfChunks);

        Debug.Log($"There are {numberOfChunks} chunks");
        List<Vector2> filledChunks = new();

        int skips = 0;
        const int MAX_SKIPS = 1000;
        while (filledChunks.Count() < roomGenSettings.numberOfRooms && skips < MAX_SKIPS)
        {
            int x = UnityEngine.Random.Range(0, chunksSquared);
            int y = UnityEngine.Random.Range(0, chunksSquared);

            Vector2 chunkOffset = new Vector2(
                x * chunkWidth,
                y * chunkWidth
            );

            if (filledChunks.Contains(new(x, y)))
            {
                //Debug.Log($"Skipping filled chunk {x}, {y}");
                skips++;
                continue;
            }

            // Determine scale of room
            Vector3 roomFloorScale = new(
                UnityEngine.Random.Range(roomGenSettings.minRoomWidth, roomGenSettings.maxRoomWidth),
                1,
                UnityEngine.Random.Range(roomGenSettings.minRoomWidth, roomGenSettings.maxRoomWidth)
            );

            // Determine opposite corner positions based on room scale
            int startXPos = UnityEngine.Random.Range(0, chunkWidth - (int)roomFloorScale.x);
            int startZPos = UnityEngine.Random.Range(0, chunkWidth - (int)roomFloorScale.z);

            Vector2 startCoordinate = new Vector2(
                startXPos,
                startZPos
            ) + chunkOffset;

            Vector2 endCoordinate = new Vector2(
                roomFloorScale.x - 1,
                roomFloorScale.z - 1
            ) + startCoordinate;

            for (int dataX = (int) startCoordinate.x; dataX <= endCoordinate.x; dataX++)
            {
                for (int dataY = (int)startCoordinate.y; dataY <= endCoordinate.y; dataY++)
                {
                    if (dataX == startCoordinate.x || dataX == endCoordinate.x
                        || dataY == startCoordinate.y || dataY == endCoordinate.y)
                    {
                        data.grid[dataX, dataY] = ModularFortressLayoutData.TileType.Wall;
                    }
                    else
                    {
                        data.grid[dataX, dataY] = ModularFortressLayoutData.TileType.Floor;
                    }
                }
            }

            Vector3 startPos = CoordinateToPosition(startCoordinate.x, 0, startCoordinate.y);

            Vector3 center = startPos + roomFloorScale * 0.5F;
            center -= Vector3.one * 0.5F; // place back on grid

            GameObject newChunkObj = Instantiate(unitCubePrefab);
            newChunkObj.transform.localScale = roomFloorScale;
            newChunkObj.transform.position = center;
            newChunkObj.transform.parent = transform;

            newChunkObj.name = newChunkObj.name + $"[{x}, {y}]";

            ModularFortressPiece piece = newChunkObj.GetComponent<ModularFortressPiece>();
            if (piece == null) newChunkObj.AddComponent<ModularFortressPiece>();

            piece.width = (int)roomFloorScale.x;
            piece.height = (int)roomFloorScale.y;
            piece.length = (int)roomFloorScale.z;
            piece.chunkCoords = new(x, y);
            piece.startGridPos = new(startCoordinate.x, 0, startCoordinate.y);
            piece.endGridPos = new(endCoordinate.x, 0, endCoordinate.y);

            filledChunks.Add(new(x, y));
        }

        if (skips >= MAX_SKIPS)
        {
            throw new Exception("Skipped more than allowable number of iterations placing new rooms in chunks");
        }

    }

    public void GeneratePaths(List<RoomConnection> connections)
    {
        foreach (RoomConnection conn in connections)
        {
            ModularFortressPiece room1 = conn.room1;
            ModularFortressPiece room2 = conn.room2;

            // if (room1.chunkCoords.x == room2.chunkCoords.x) // in line along y axis, check x connections
            // {
            //     ModularFortressPiece lowerXStart = room1.startGridPos.x < room2.startGridPos.x ? room1 : room2;
            //     ModularFortressPiece upperXStart = room1.startGridPos.x < room2.startGridPos.x ? room2 : room1;

            //     float sharedX = lowerXStart.endGridPos.x - upperXStart.startGridPos.x - 1;
            //     if (sharedX < roomGenSettings.pathWidth)
            //     {
            //         Debug.LogError($"X CONNECTION WITH INVALID SHARED WIDTH - {room1.chunkCoords} -> {room2.chunkCoords}: {sharedX} shared");
            //     }
            // }
            GeneratePath(room1, room2, roomGenSettings.pathWidth);
        }
    }

    void GeneratePath(ModularFortressPiece fromPiece, ModularFortressPiece toPiece, int width)
    {
        Vector2 direction = toPiece.chunkCoords - fromPiece.chunkCoords;

        Vector2 stepDirection = direction;
        stepDirection.x = (int)Mathf.Clamp(stepDirection.x, -1, 1);
        stepDirection.y = (int)Mathf.Clamp(stepDirection.y, -1, 1);

        float startXBound = 0;
        if (direction.x > 0) startXBound = fromPiece.endGridPos.x + 1;
        else if (direction.x < 0) startXBound = fromPiece.startGridPos.x - 1;

        float startZBound = 0;
        if (direction.y > 0) startZBound = fromPiece.endGridPos.z + 1;
        else if (direction.y < 0) startZBound = fromPiece.startGridPos.z - 1;

        Vector2 startPos = new(startXBound, startZBound);

        float xPathableOverlapBegin = Mathf.Max(fromPiece.startGridPos.x, toPiece.startGridPos.x) + roomGenSettings.cornerMargin;
        float xPathableOverlapEnd = Mathf.Min(fromPiece.endGridPos.x, toPiece.endGridPos.x) - roomGenSettings.cornerMargin + 1;

        float zPathableOverlapBegin = Mathf.Max(fromPiece.startGridPos.z, toPiece.startGridPos.z) + roomGenSettings.cornerMargin;
        float zPathableOverlapEnd = Mathf.Min(fromPiece.endGridPos.z, toPiece.endGridPos.z) - roomGenSettings.cornerMargin + 1;

        if (fromPiece.InLine(toPiece))// && fromPiece.chunkCoords.x == toPiece.chunkCoords.x)
        {
            if (fromPiece.chunkCoords.x == toPiece.chunkCoords.x) startPos.x += UnityEngine.Random.Range((int)xPathableOverlapBegin, (int)(xPathableOverlapEnd - roomGenSettings.pathWidth));
            if (fromPiece.chunkCoords.y == toPiece.chunkCoords.y) startPos.y += UnityEngine.Random.Range((int)zPathableOverlapBegin, (int)(zPathableOverlapEnd - roomGenSettings.pathWidth));

            Vector3 nextPos = new(startPos.x, 0, startPos.y);
            int iterations = 0;
            while (!IsGridPointFilled(nextPos) && iterations < this.width)
            {
                // iterate cross-wise for path length
                for (int i = 0; i < roomGenSettings.pathWidth; i++)
                {
                    if (i == 0 || i == (roomGenSettings.pathWidth - 1))
                    {
                        data.grid[(int)(nextPos.x + Mathf.Abs(stepDirection.y) * i), (int)(nextPos.z + Mathf.Abs(stepDirection.x) * i)] = ModularFortressLayoutData.TileType.Wall;
                    }

                    Instantiate(pathUnitCubePrefab, CoordinateToPosition(nextPos.x + Mathf.Abs(stepDirection.y) * i, nextPos.y, nextPos.z + Mathf.Abs(stepDirection.x) * i), Quaternion.identity, transform);
                }

                nextPos += new Vector3(stepDirection.x, 0, stepDirection.y);

                iterations++;
            }

            // Mark segments of room boundary that lead to paths as floors
            Vector2 endPos = new(nextPos.x, nextPos.z); //+ stepDirection;
            for (int i = 1; i < (roomGenSettings.pathWidth - 1); i++)
            {
                Vector2 startBasedCoord = startPos - stepDirection + new Vector2(Mathf.Abs(stepDirection.y), Mathf.Abs(stepDirection.x)) * i;
                Vector2 endBasedCoord = endPos + new Vector2(Mathf.Abs(stepDirection.y), Mathf.Abs(stepDirection.x)) * i;

                data.grid[(int)startBasedCoord.x, (int)startBasedCoord.y] = ModularFortressLayoutData.TileType.Floor;
                data.grid[(int)endBasedCoord.x, (int)endBasedCoord.y] = ModularFortressLayoutData.TileType.Floor;
            }
        }
        else
        {
            // Paths are not in line, need to create a connector/corner hallway
        }
    }

    public List<ModularFortressPiece> GetRoomPieces()
    {
        return transform.Cast<Transform>()
            .Where(t => t.gameObject.TryGetComponent(out ModularFortressPiece _))
            .Select(t => t.gameObject.GetComponent<ModularFortressPiece>())
            .ToList();
    }


    public void Clear()
    {
#if UNITY_EDITOR
        // For some reason we have issues foreach'ing over transforms in editor
        List<Transform> tempList = transform.Cast<Transform>().ToList();
        foreach (var child in tempList)
        {
            DestroyImmediate(child.gameObject);
        }
#else
    foreach (Transform child in transform)
    {
        Destroy(child.gameObject);
    }
#endif

    }



    void OnDrawGizmos()
    {
        // try
        // {
        //     for (int x = 0; x < width; x++)
        //     {
        //         for (int y = 0; y < width; y++)
        //         {
        //             Color color = IsGridPointFilled(new Vector3(x, 0, y)) ? Color.red: Color.green;
        //             color *= 0.5F;
        //             // if (filled[x, y, 0])
        //             // {
        //             //     color = Color.red;
        //             // }

        //             Gizmos.color = color;

        //             Vector3 pos = CoordinateToPosition(x, 0, y);
        //             Gizmos.DrawWireCube(pos, Vector3.one);
        //         }
        //     }
        // }
        // catch (Exception)
        // { }

        try
        {
            Color[] colors = new Color[5];
            colors[0] = Color.red;
            colors[1] = Color.yellow;
            colors[2] = Color.green;
            colors[3] = Color.cyan;
            colors[4] = Color.magenta;
            int colorIndex = 0;

            foreach (List<Line> path in pathsThroughFortress)
            {
                Gizmos.color = colors[colorIndex];

                //Debug.Log(path + " length " + path.Count());
                foreach (Line line in path)
                {
                    //Debug.Log(line.v0 + " -> " + line.v1);

                    Vector3 v0 = new(line.v0.x, 0, line.v0.y);
                    Vector3 v1 = new(line.v1.x, 0, line.v1.y);
                    Vector3 direction = v0 - v1;

                    Gizmos.DrawLine(new Vector3(0, transform.position.y + 2, 0) + v0, new Vector3(0, transform.position.y + 2, 0) + v1);
                    Gizmos.DrawLine(new Vector3(0, transform.position.y + 2, 0) + v1, new Vector3(0, transform.position.y + 2, 0) + v1 + (Quaternion.Euler(0, -30, 0) * direction).normalized * 3F);
                    Gizmos.DrawLine(new Vector3(0, transform.position.y + 2, 0) + v1, new Vector3(0, transform.position.y + 2, 0) + v1 + (Quaternion.Euler(0, 30, 0) * direction).normalized * 3F);
                }

                colorIndex++;
                if (colorIndex == 5)
                {
                    colorIndex = 0;
                }
            }
            
            
            // foreach (RoomConnection roomConnection in conns)
            // {
            //     Gizmos.color = Color.blue;
            //     Vector3 direction = roomConnection.room1.transform.position - roomConnection.room2.transform.position;

            //     Gizmos.DrawLine(new Vector3(0, 2, 0) + roomConnection.room1.transform.position, new Vector3(0, 2, 0) + roomConnection.room2.transform.position);
            //     Gizmos.DrawLine(new Vector3(0, 2, 0) + roomConnection.room2.transform.position, new Vector3(0, 2, 0) + roomConnection.room2.transform.position + (Quaternion.Euler(0, -30, 0) * direction).normalized * 3F);
            //     Gizmos.DrawLine(new Vector3(0, 2, 0) + roomConnection.room2.transform.position, new Vector3(0, 2, 0) + roomConnection.room2.transform.position + (Quaternion.Euler(0, 30, 0) * direction ).normalized * 3F);
            // }
        }
        catch (Exception)
        { }
        // foreach (Line line in mst)
        // {
        //     Vector3 v0 = new(line.v0.x, 0, line.v0.y);
        //     Vector3 v1 = new(line.v1.x, 0, line.v1.y);
        //     Gizmos.DrawLine(v0, v1);
        // }

    }
}
