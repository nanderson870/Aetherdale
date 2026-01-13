using System.Collections.Generic;
using System.Linq;
using Geometry2D;
using UnityEngine;

public class DisjointSet<T>
{
    private Dictionary<T, T> parent = new();

    public void MakeSet(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            parent[item] = item;
        }
    }

    public T Find(T item)
    {
        if (!parent.ContainsKey(item))
            parent[item] = item;

        if (!parent[item].Equals(item))
            parent[item] = Find(parent[item]); // Path compression

        return parent[item];
    }

    public void Union(T a, T b)
    {
        T rootA = Find(a);
        T rootB = Find(b);
        if (!rootA.Equals(rootB))
        {
            parent[rootA] = rootB;
        }
    }

    public bool Connected(T a, T b)
    {
        return Find(a).Equals(Find(b));
    }
}

public static class MSTBuilder
{
    public static List<Line> BuildMST(List<Line> input)
    {
        // Sort edges by length
        input.Sort((a, b) => a.GetLength().CompareTo(b.GetLength()));

        HashSet<Vector2> points = new();

        foreach (Line edge in input)
        {
            points.Add(edge.v0);
            points.Add(edge.v1);
        }

        DisjointSet<Vector2> ds = new();
        ds.MakeSet(points);

        List<Line> mst = new();

        foreach (Line edge in input)
        {
            if (!ds.Connected(edge.v0, edge.v1))
            {
                mst.Add(edge);
                ds.Union(edge.v0, edge.v1);
            }

            // Early exit if we've added enough edges
            if (mst.Count == points.Count - 1)
                break;
        }

        return mst;
    }


}