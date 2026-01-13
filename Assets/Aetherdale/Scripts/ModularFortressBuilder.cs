using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using Unity.Collections;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class ModularFortressBuilder : MonoBehaviour
{
    [Obsolete] [SerializeField] Texture2D inputTexture;
    [SerializeField] ModularFortressGenerator generator;


    [SerializeField] int wallHeight = 3;
    [SerializeField] Vector3 scale = Vector3.one;
    [SerializeField] ModularFortressPiece unitPiecePrefab; //1x1x1 basic all purpose building block
    [SerializeField] ModularFortressPiece[] groundPiecePrefabs;
    [SerializeField] ModularFortressPiece[] wallPiecePrefabs;
    [SerializeField] ModularFortressPiece[] pillarPiecePrefabs;

    ModularFortressLayoutData data;

    bool[,,] filled;

    void Start()
    {
        // if (data == null && inputTexture != null)
        // {
        //     data = DataFromTexture(inputTexture);
        // }   
    }


    public static ModularFortressLayoutData DataFromTexture(Texture2D image)
    {
        Color[] generationPixels = image.GetPixels();

        int width = image.width;
        int height = image.height;

        ModularFortressLayoutData data = new();
        data.grid = new ModularFortressLayoutData.TileType[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (generationPixels[y * width + x] == Color.white)
                {
                    data.grid[x, y] = ModularFortressLayoutData.TileType.Wall;
                }
                else
                {
                    data.grid[x, y] = ModularFortressLayoutData.TileType.Floor;
                }
            }
        }

        return data;
    }

    public float GetWidth()
    {
        return data.width * scale.x;
    }

    public float GetHeight()
    {
        return data.length * scale.z;
    }

    public Vector3 GetOffset()
    {
        return new(-GetWidth() / 2, 0, -GetHeight() / 2);
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

    public void Generate()
    {
        Clear();

        if (generator != null)
        {
            generator.Generate();

            data = generator.data;
        }
        else
        {
            data = DataFromTexture(inputTexture);
        }


        filled = new bool[data.width, wallHeight, data.length];

        GenerateGroundTiles();

        GenerateWalls();

    }

    bool Fits(ModularFortressPiece piece, int x, int y)
    {
        bool fits = true;
        for (int neededX = x; neededX < x + piece.width && neededX < data.width; neededX++)
        {
            for (int neededY = y; neededY < y + piece.length && neededY < data.length; neededY++)
            {
                if (filled[neededX, 0, neededY])
                {
                    fits = false;
                    break;
                }
            }
        }

        return fits
            && piece.width <= (data.width - x)
            && piece.length <= (data.length - y);

    }

    Vector3 CoordinateToPosition(int x, int y, int z)
    {
        return transform.position + Vector3.Scale(GetOffset() + new Vector3(x, y, z), scale) + new Vector3(0.5F, 0.5F, 0.5F);
    }

    void GenerateGroundTiles()
    {
        int width = data.width;
        int height = data.length;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (filled[x, 0, y])
                {
                    //Debug.Log("filled");
                    continue;
                }

                ModularFortressPiece[] possiblePieces = groundPiecePrefabs.Where(piece => Fits(piece, x, y)).ToArray();

                ModularFortressPiece piecePrefab = possiblePieces[UnityEngine.Random.Range(0, possiblePieces.Length)];
                Vector3 pieceOffset = new(0.5F * piecePrefab.width, 0.5F * piecePrefab.height, 0.5F * piecePrefab.length);

                Vector3 position = pieceOffset + CoordinateToPosition(x, 0, y);

                ModularFortressPiece tile = Instantiate(piecePrefab, position, Quaternion.identity, transform);
                tile.transform.localScale = scale;

                for (int placedX = 0; placedX < tile.width; placedX++)
                {
                    for (int placedY = 0; placedY < tile.length; placedY++)
                    {
                        filled[x + placedX, 0, y + placedY] = true;
                    }
                }

#if UNITY_EDITOR
                //SceneVisibilityManager.instance.DisablePicking(tile.gameObject, true);
#endif

            }
        }
    }

    bool[,] GetApplicabilityMatrix(ModularFortressLayoutData.TileType type)
    {
        int width = data.width;
        int length = data.length;

        bool[,] applicable = new bool[width, length];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < length; y++)
            {
                if (data.grid[x, y] == type)
                {
                    applicable[x, y] = true;
                }
            }
        }

        return applicable;
    }


    void GenerateWalls()
    {
        bool[,] isWall = GetApplicabilityMatrix(ModularFortressLayoutData.TileType.Wall);

        int width = isWall.GetLength(0);
        int length = isWall.GetLength(1);

        List<Tuple<Vector2, Vector2>> endpoints = new();
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < length; z++)
            {
                if (isWall[x, z])
                {
                    CreatePiece(pillarPiecePrefabs[UnityEngine.Random.Range(0, pillarPiecePrefabs.Length)], (int) x, 0, (int) z);
                }
            }
        }
    }

    void CreatePiece(ModularFortressPiece piecePrefab, int x, int y, int z)
    {
        Vector3 pos = CoordinateToPosition(x, y, z);

        Instantiate(piecePrefab, pos, Quaternion.identity, transform);
    }

    void OnDrawGizmos()
    {
        // try
        // {
        //     for (int x = 0; x < generationTexture.width; x++)
        //     {
        //         for (int y = 0; y < generationTexture.width; y++)
        //         {
        //             Color color = Color.green;
        //             if (filled[x, y, 0])
        //             {
        //                 color = Color.red;
        //             }

        //             Gizmos.color = color;

        //             Vector3 pos = transform.position + GetOffset() + new Vector3(x * scale.x, 0, y * scale.x) + new Vector3(0.5F, 0, 0.5F);
        //             Gizmos.DrawWireSphere(pos, 0.5F);
        //         }
        //     }
        // }
        // catch (Exception e)
        // {}
    }
}


public class ModularFortressLayoutData
{
    public enum TileType
    {
        None = 0,
        Floor = 1,
        Wall = 2,
    }

    public TileType[,] grid;
    public int width => grid.GetLength(0);
    public int length => grid.GetLength(1);
}