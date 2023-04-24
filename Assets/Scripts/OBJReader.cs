using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

public static class OBJReader
{
    public static OBJ ProcessPath(string path)
    {
        Debug.Log("Process path.");
        return ProcessOBJ(File.ReadAllLines(path));
    }

    // Returns a wireframe object.
    public static OBJ ProcessOBJ(string[] lines)
    {
        Debug.Log("Process text.");
        List<Vector4> vertices = new List<Vector4>();
        foreach (string vertex in lines.Where((s) => s.Substring(0, 2) == "v "))
        {
            vertices.Add(
                new Vector4(
                    float.Parse(vertex.Split(' ')[1], CultureInfo.InvariantCulture),
                    float.Parse(vertex.Split(' ')[2], CultureInfo.InvariantCulture),
                    float.Parse(vertex.Split(' ')[3], CultureInfo.InvariantCulture),
                    vertex.Split(' ').Count() == 5 ? float.Parse(vertex.Split(' ')[4], CultureInfo.InvariantCulture) : 1
                )
            );
        }

        List<Edge> edges = new List<Edge>();
        foreach (string face in lines.Where((s) => s.Substring(0, 2) == "f "))
        {
            for (int i = 1; i < face.Split(' ').Count(); i++)
            {
                Vector4 A = vertices[
                    int.Parse(
                        face.Split(' ')[i == 1 ? face.Split(' ').Count() - 1 : i - 1].Split('/')[0]
                    , CultureInfo.InvariantCulture) - 1
                ];
                Vector4 B = vertices[int.Parse(face.Split(' ')[i].Split('/')[0], CultureInfo.InvariantCulture) - 1];
                if (
                    edges.FirstOrDefault(
                        (e) =>
                            (
                                e.Start.Equals(A) && e.End.Equals(B)
                                || (e.Start.Equals(B) && e.End.Equals(A))
                            )
                    ) == default(Edge)
                )
                    edges.Add(new Edge(A, B));
            }
        }
        return new OBJ(edges);
    }
}

public class OBJ
{
    public List<Edge> Edges = new List<Edge>();

    public OBJ(List<Edge> edges)
    {
        Edges = edges;
    }
}

public class Edge
{
    public Vector4 Start;
    public Vector4 End;

    public Edge(Vector4 start, Vector4 end)
    {
        Start = start;
        End = end;
    }
}
