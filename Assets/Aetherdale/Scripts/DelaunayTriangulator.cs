

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Geometry2D;

public class DelaunayTriangulator : MonoBehaviour
{
    /// <summary>
    /// Create a triangle sufficiently big to hold entire pointlist. Gets created over X and Z ONLY
    /// 
    /// Algorithm from Gorilla Sun
    /// </summary>
    /// <param name="pointList"></param>
    /// <returns></returns>
    public Triangle GetSuperTriangle(List<Vector2> pointList)
    {
        float minX = Mathf.Infinity;
        float minY = Mathf.Infinity;
        float maxX = Mathf.NegativeInfinity;
        float maxY = Mathf.NegativeInfinity;

        foreach (Vector2 point in pointList)
        {
            minX = Mathf.Min(minX, point.x);
            minY = Mathf.Min(minY, point.y);
            maxX = Mathf.Max(maxX, point.x);
            maxY = Mathf.Max(maxY, point.y);
        }

        float dX = (maxX - minX) * 10;
        float dY = (maxY - minY) * 10;

        Vector2 v0 = new Vector2(minX - dX, minY - dY * 3);
        Vector2 v1 = new Vector2(minX - dX, maxY + dY);
        Vector2 v2 = new Vector2(maxX + dX * 3, maxY + dY);

        // return new(new(0, 0), new(300, 600), new(900, 700));
        return new(v0, v1, v2);
    }

    public List<Triangle> BowyerWatsonTriangulate(List<Vector3> pointList)
    {
        List<Vector2> pointList2D = pointList.Select(x => new Vector2(x.x, x.z)).ToList();

        List<Triangle> triangles = new();
        Triangle superTriangle = GetSuperTriangle(pointList2D);
        triangles.Add(superTriangle);

        int iterations = 0;
        foreach (Vector2 point in pointList2D)
        {
            List<Triangle> badTriangles = new();

            foreach (Triangle triangle in triangles)
            {
                if (Vector2.Distance(point, triangle.circumCircle.center) < triangle.circumCircle.radius)
                {
                    badTriangles.Add(triangle);
                }
            }

            List<Line> polygon = new();
            foreach (Triangle badTriangle in badTriangles)
            {
                foreach (Line line in badTriangle.GetLines())
                {
                    bool containedInOtherBadTriangles = false;
                    foreach (Triangle other in badTriangles)
                    {
                        if (other == badTriangle) continue;

                        if (other.ContainsLine(line))
                        {
                            containedInOtherBadTriangles = true;
                            break;
                        }
                    }

                    if (!containedInOtherBadTriangles)
                    {
                        polygon.Add(line);
                    }
                }
            }

            for (int i = triangles.Count() - 1; i >= 0; i--)
            {
                if (badTriangles.Contains(triangles[i]))
                {
                    triangles.RemoveAt(i);
                }
            }

            foreach (Line line in polygon)
            {
                Triangle newTri = new(line.v0, line.v1, point);
                triangles.Add(newTri);
            }

            iterations++;
        }

        for (int i = triangles.Count() - 1; i >= 0; i--)
        {
            Triangle triangle = triangles[i];
            if (superTriangle.ContainsPoint(triangle.v0) || superTriangle.ContainsPoint(triangle.v1) || superTriangle.ContainsPoint(triangle.v2))
            {
                triangles.RemoveAt(i);
            }
        }

        return triangles;
    }

    void OnDrawGizmos()
    {
        // // For some reason we have issues foreach'ing over transforms in editor
        // List<Transform> tempList = transform.Cast<Transform>().ToList();
        // foreach (Transform child in tempList)
        // {
        //     foreach (Transform child2 in tempList)
        //     {
        //         if (child != child2)
        //         {
        //             Geometry.Edge edge = new(child.position, child2.position);
        //             Gizmos.DrawLine(edge.v0, edge.v1);
        //         }
        //     }
        // }

        // foreach (Triangle triangle in BowyerWatsonTriangulate(transform.Cast<Transform>().Select(t => t.position).ToList()))
        // {
        //     Gizmos.color = Color.blue;
        //     Vector3 v0 = new(triangle.v0.x, 0, triangle.v0.y);
        //     Vector3 v1 = new(triangle.v1.x, 0, triangle.v1.y);
        //     Vector3 v2 = new(triangle.v2.x, 0, triangle.v2.y);

        //     Gizmos.DrawLine(v0, v1);
        //     Gizmos.DrawLine(v1, v2);
        //     Gizmos.DrawLine(v2, v0);

        //     // Gizmos.color = Color.red;
        //     // Line[] lines = triangle.GetLines();
        //     // foreach (Line line in lines)
        //     // {
        //     //     Line perp = line.GetPerpendicularBisector();
        //     //     Gizmos.DrawLine(new Vector3(perp.v0.x, 0, perp.v0.y), new Vector3(perp.v1.x, 0, perp.v1.y));
        //     // }

        //     // Gizmos.color = Color.magenta;
        //     // Vector3 circumCirclePos = new(triangle.circumCircle.center.x, 0, triangle.circumCircle.center.y);
        //     // Gizmos.DrawWireSphere(circumCirclePos, triangle.circumCircle.radius);
        // }

    }
}