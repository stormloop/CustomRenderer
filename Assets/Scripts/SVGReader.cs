using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Xml;
using System.Globalization;

public static class SVGReader
{
    public static SVG ProcessPath(string path)
    {
        Debug.Log("Process path.");
        return ProcessSVG(File.ReadAllText(path));
    }

    public static SVG ProcessSVG(string contents)
    {
        Debug.Log("Process text.");
        string viewBox = contents.Split("viewBox=\"")[1].Split("\"")[0];
        Vector2 bl = new Vector2(
            float.Parse(viewBox.Split(' ')[0], CultureInfo.InvariantCulture),
            float.Parse(viewBox.Split(' ')[1], CultureInfo.InvariantCulture)
        );
        Vector2 tr = new Vector2(
            float.Parse(viewBox.Split(' ')[2], CultureInfo.InvariantCulture),
            float.Parse(viewBox.Split(' ')[3], CultureInfo.InvariantCulture)
        );

        XmlDocument svgDoc = new XmlDocument();
        svgDoc.LoadXml(contents);
        var nameSpaceManager = new XmlNamespaceManager(svgDoc.NameTable);
        nameSpaceManager.AddNamespace("svg", "http://www.w3.org/2000/svg");

        return new SVG(bl, tr, svgDoc, nameSpaceManager);
    }

    public static List<PathElement> ProcessElements(string elements)
    {
        List<PathElement> output = new List<PathElement>();
        Vector2 startPos = new Vector2();
        Vector2 currentPos = new Vector2();
        Vector2 controlPoint = new Vector2();
        string[] commands = Regex.Split(elements, @"(?=[A-DF-Za-df-z])");

        for (int i = 0; i < commands.Length; i++)
        {
            output.AddRange(
                ProcessElement(
                    commands[i].Split(' ', 2)[0],
                    commands[i].Split(' ', 2)[commands[i].Split(' ', 2).Length - 1],
                    startPos,
                    currentPos,
                    controlPoint,
                    out startPos,
                    out currentPos,
                    out controlPoint
                )
            );
        }

        return output;
    }

    private static List<PathElement> ProcessElement(
        string identifier,
        string @params,
        Vector2 startPosIn,
        Vector2 posIn,
        Vector2 controlIn,
        out Vector2 startPos,
        out Vector2 currentPos,
        out Vector2 controlPoint
    )
    {
        List<PathElement> output = new List<PathElement>();
        startPos = startPosIn;
        currentPos = posIn;
        controlPoint = controlIn;

        switch (identifier)
        {
            case "M":
                int i = 0;
                foreach (string coord in @params.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    currentPos = new Vector2(
                        float.Parse(coord.Split(',')[0], CultureInfo.InvariantCulture),
                        float.Parse(coord.Split(',')[1], CultureInfo.InvariantCulture)
                    );
                    if (i == 0)
                    {
                        i++;
                        startPos = currentPos;
                        posIn = currentPos;
                        continue;
                    }
                    output.Add(new LineElement(posIn, currentPos));
                    posIn = currentPos;
                }
                break;
            case "m":
                i = 0;
                foreach (string coord in @params.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    currentPos =
                        new Vector2(
                            float.Parse(coord.Split(',')[0], CultureInfo.InvariantCulture),
                            float.Parse(coord.Split(',')[1], CultureInfo.InvariantCulture)
                        ) + posIn;
                    if (i == 0)
                    {
                        i++;
                        startPos = currentPos;
                        posIn = currentPos;
                        continue;
                    }
                    output.Add(new LineElement(posIn, currentPos));
                    posIn = currentPos;
                }
                break;
            case "L":
                foreach (string coord in @params.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    currentPos = new Vector2(
                        float.Parse(coord.Split(',')[0], CultureInfo.InvariantCulture),
                        float.Parse(coord.Split(',')[1], CultureInfo.InvariantCulture)
                    );
                    output.Add(new LineElement(posIn, currentPos));
                    posIn = currentPos;
                }
                break;
            case "l":
                foreach (string coord in @params.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    currentPos =
                        new Vector2(
                            float.Parse(coord.Split(',')[0], CultureInfo.InvariantCulture),
                            float.Parse(coord.Split(',')[1], CultureInfo.InvariantCulture)
                        ) + posIn;
                    output.Add(new LineElement(posIn, currentPos));
                    posIn = currentPos;
                }
                break;
            case "H":
                currentPos = new Vector2(float.Parse(@params.Replace(" ", ""), CultureInfo.InvariantCulture), posIn.y);
                output.Add(new LineElement(posIn, currentPos));
                break;
            case "h":
                currentPos = new Vector2(float.Parse(@params.Replace(" ", ""), CultureInfo.InvariantCulture) + posIn.x, posIn.y);
                output.Add(new LineElement(posIn, currentPos));
                break;
            case "V":
                currentPos = new Vector2(posIn.x, float.Parse(@params.Replace(" ", ""), CultureInfo.InvariantCulture));
                output.Add(new LineElement(posIn, currentPos));
                break;
            case "v":
                currentPos = new Vector2(posIn.x, float.Parse(@params.Replace(" ", ""), CultureInfo.InvariantCulture) + posIn.y);
                output.Add(new LineElement(posIn, currentPos));
                break;
            case "C":
                for (
                    i = 0;
                    i < @params.Split(' ', StringSplitOptions.RemoveEmptyEntries).Count();
                    i += 3
                )
                {
                    List<string> list = @params
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .ToList();
                    Vector2 controlStart = new Vector2(
                        float.Parse(list[i].Split(',')[0], CultureInfo.InvariantCulture),
                        float.Parse(list[i].Split(',')[1], CultureInfo.InvariantCulture)
                    );
                    Vector2 controlEnd = new Vector2(
                        float.Parse(list[i + 1].Split(',')[0], CultureInfo.InvariantCulture),
                        float.Parse(list[i + 1].Split(',')[1], CultureInfo.InvariantCulture)
                    );
                    currentPos = new Vector2(
                        float.Parse(list[i + 2].Split(',')[0], CultureInfo.InvariantCulture),
                        float.Parse(list[i + 2].Split(',')[1], CultureInfo.InvariantCulture)
                    );
                    output.Add(new CubicBezierElement(posIn, currentPos, controlStart, controlEnd));
                    posIn = currentPos;
                    controlPoint = controlEnd;
                }
                break;
            case "c":
                for (
                    i = 0;
                    i < @params.Split(' ', StringSplitOptions.RemoveEmptyEntries).Count();
                    i += 3
                )
                {
                    List<string> list = @params
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .ToList();

                    Vector2 controlStart =
                        new Vector2(
                            float.Parse(list[i].Split(',')[0], CultureInfo.InvariantCulture),
                            float.Parse(list[i].Split(',')[1], CultureInfo.InvariantCulture)
                        ) + posIn;
                    Vector2 controlEnd =
                        new Vector2(
                            float.Parse(list[i + 1].Split(',')[0], CultureInfo.InvariantCulture),
                            float.Parse(list[i + 1].Split(',')[1], CultureInfo.InvariantCulture)
                        ) + posIn;
                    currentPos =
                        new Vector2(
                            float.Parse(list[i + 2].Split(',')[0], CultureInfo.InvariantCulture),
                            float.Parse(list[i + 2].Split(',')[1], CultureInfo.InvariantCulture)
                        ) + posIn;
                    output.Add(new CubicBezierElement(posIn, currentPos, controlStart, controlEnd));
                    posIn = currentPos;
                    controlPoint = controlEnd;
                }
                break;
            case "S":
                throw new NotImplementedException();
                break;
            case "s":
                throw new NotImplementedException();
                break;
            case "Q":
                for (
                    i = 0;
                    i < @params.Split(' ', StringSplitOptions.RemoveEmptyEntries).Count();
                    i += 2
                )
                {
                    List<string> list = @params
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .ToList();
                    Vector2 control = new Vector2(
                        float.Parse(list[i].Split(',')[0], CultureInfo.InvariantCulture),
                        float.Parse(list[i].Split(',')[1], CultureInfo.InvariantCulture)
                    );
                    currentPos = new Vector2(
                        float.Parse(list[i + 1].Split(',')[0], CultureInfo.InvariantCulture),
                        float.Parse(list[i + 1].Split(',')[1], CultureInfo.InvariantCulture)
                    );
                    output.Add(new QuadraticBezierElement(posIn, currentPos, control));
                    posIn = currentPos;
                    controlPoint = control;
                }
                break;
            case "q":
                for (
                    i = 0;
                    i < @params.Split(' ', StringSplitOptions.RemoveEmptyEntries).Count();
                    i += 2
                )
                {
                    List<string> list = @params
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .ToList();
                    Vector2 control =
                        new Vector2(
                            float.Parse(list[i].Split(',')[0], CultureInfo.InvariantCulture),
                            float.Parse(list[i].Split(',')[1], CultureInfo.InvariantCulture)
                        ) + posIn;
                    currentPos =
                        new Vector2(
                            float.Parse(list[i + 1].Split(',')[0], CultureInfo.InvariantCulture),
                            float.Parse(list[i + 1].Split(',')[1], CultureInfo.InvariantCulture)
                        ) + posIn;
                    output.Add(new QuadraticBezierElement(posIn, currentPos, control));
                    posIn = currentPos;
                    controlPoint = control;
                }
                break;
            case "T":
                throw new NotImplementedException();
                break;
            case "t":
                throw new NotImplementedException();
                break;
            case "A":
                throw new NotImplementedException();
                break;
            case "a":
                throw new NotImplementedException();
                break;
            case "Z":
                currentPos = startPosIn;
                output.Add(new LineElement(posIn, currentPos));
                break;
            case "z":
                currentPos = startPosIn;
                output.Add(new LineElement(posIn, currentPos));
                break;
        }

        return output;
    }

    public static List<PathElement> ProcessTransformations(
        List<PathElement> elements,
        string transformationsString,
        string originString
    )
    {
        string[] transformations = transformationsString.Split(
            ')',
            StringSplitOptions.RemoveEmptyEntries
        );
        Vector2 origin =
            originString == ""
                ? Vector2.zero
                : originString.Contains('%')
                    ? throw new NotSupportedException()
                    : new Vector2(
                        float.Parse(originString.Split(' ', ',')[0], CultureInfo.InvariantCulture),
                        float.Parse(originString.Split(' ', ',')[1], CultureInfo.InvariantCulture)
                    );

        for (int i = 0; i < transformations.Length; i++)
        {
            elements = ProcessTransformation(
                elements,
                transformations[i].Split('(')[0].Replace(" ", ""),
                transformations[i].Split('(')[1],
                origin
            );
        }
        return elements;
    }

    private static List<PathElement> ProcessTransformation(
        List<PathElement> elements,
        string command,
        string @params,
        Vector2 origin
    )
    {
        switch (command.ToLower())
        {
            case "translate":
                foreach (PathElement element in elements)
                {
                    Vector2[] points = element.GetAllPoints();
                    for (int i = 0; i < points.Count(); i++)
                    {
                        if (@params.Contains(' ') || @params.Contains(','))
                        {
                            points[i] += new Vector2(
                                float.Parse(@params.Split(' ', ',')[0], CultureInfo.InvariantCulture),
                                float.Parse(@params.Split(' ', ',')[1], CultureInfo.InvariantCulture)
                            );
                            continue;
                        }
                        points[i] += new Vector2(float.Parse(@params, CultureInfo.InvariantCulture), 0);
                    }
                    element.SetAllPoints(points);
                }
                break;
            case "scale":
                foreach (PathElement element in elements)
                {
                    Vector2[] points = element.GetAllPoints();
                    for (int i = 0; i < points.Count(); i++)
                    {
                        points[i] -= origin;
                        if (@params.Contains(' ') || @params.Contains(','))
                        {
                            points[i] *= new Vector2(
                                float.Parse(@params.Split(' ', ',')[0], CultureInfo.InvariantCulture),
                                float.Parse(@params.Split(' ', ',')[1], CultureInfo.InvariantCulture)
                            );
                            points[i] += origin;
                            continue;
                        }
                        points[i] *= new Vector2(float.Parse(@params, CultureInfo.InvariantCulture), float.Parse(@params, CultureInfo.InvariantCulture));
                        points[i] += origin;
                    }
                    element.SetAllPoints(points);
                }
                break;
            case "rotate":
                foreach (PathElement element in elements)
                {
                    Vector2[] points = element.GetAllPoints();
                    for (int i = 0; i < points.Count(); i++)
                    {
                        float mag = 0;
                        double deg = 0;
                        if (@params.Contains(' ') || @params.Contains(','))
                        {
                            points[i] -= new Vector2(
                                float.Parse(@params.Split(' ', ',')[1], CultureInfo.InvariantCulture),
                                float.Parse(@params.Split(' ', ',')[2], CultureInfo.InvariantCulture)
                            );
                            mag = points[i].magnitude;
                            deg =
                                Math.Atan2(points[i].y, points[i].x) * Mathf.Rad2Deg
                                + float.Parse(@params.Split(' ', ',')[0], CultureInfo.InvariantCulture);
                            deg *= Mathf.Deg2Rad;
                            points[i] =
                                mag * new Vector2((float)Math.Cos(deg), (float)Math.Sin(deg));
                            points[i] += new Vector2(
                                float.Parse(@params.Split(' ', ',')[1], CultureInfo.InvariantCulture),
                                float.Parse(@params.Split(' ', ',')[2], CultureInfo.InvariantCulture)
                            );
                            continue;
                        }
                        points[i] -= origin;
                        mag = points[i].magnitude;
                        deg =
                            Math.Atan2(points[i].y, points[i].x) * Mathf.Rad2Deg
                            + float.Parse(@params.Split(' ', ',')[0], CultureInfo.InvariantCulture);
                        deg *= Mathf.Deg2Rad;
                        points[i] = mag * new Vector2((float)Math.Cos(deg), (float)Math.Sin(deg));
                        points[i] += origin;
                    }
                    element.SetAllPoints(points);
                }
                break;
            case "skewx":
                throw new NotSupportedException();
            case "skewy":
                throw new NotSupportedException();
            case "matrix":
                foreach (PathElement element in elements)
                {
                    float a = float.Parse(@params.Split(' ', ',')[0], CultureInfo.InvariantCulture);
                    float b = float.Parse(@params.Split(' ', ',')[1], CultureInfo.InvariantCulture);
                    float c = float.Parse(@params.Split(' ', ',')[2], CultureInfo.InvariantCulture);
                    float d = float.Parse(@params.Split(' ', ',')[3], CultureInfo.InvariantCulture);
                    float e = float.Parse(@params.Split(' ', ',')[4], CultureInfo.InvariantCulture);
                    float f = float.Parse(@params.Split(' ', ',')[5], CultureInfo.InvariantCulture);
                    Vector2[] points = element.GetAllPoints();
                    for (int i = 0; i < points.Count(); i++)
                    {
                        points[i] -= origin;
                        points[i] = new Vector2(
                            a * points[i].x + c * points[i].y + e,
                            b * points[i].x + d * points[i].y + f
                        );
                        points[i] += origin;
                    }
                    element.SetAllPoints(points);
                }
                break;
        }

        return elements;
    }
}

[System.Serializable]
public class SVG
{
    public XmlDocument Doc;
    private XmlNamespaceManager namespaceManager;
    public List<PathElement> PathElements = new List<PathElement>();
    public Vector2 BL,
        TR;

    public SVG(Vector2 bl, Vector2 tr, XmlDocument doc, XmlNamespaceManager namespaceManager)
    {
        BL = bl;
        TR = tr;
        Doc = doc;
        this.namespaceManager = namespaceManager;
        ExtractPathElements();
    }

    public void ExtractPathElements()
    {
        if (PathElements.Count > 0)
            PathElements.Clear();
        // Extract all paths from doc.
        XmlNodeList paths = Doc.SelectNodes("//svg:path", namespaceManager);
        // Create PathElements.
        foreach (XmlNode path in paths)
        {
            List<PathElement> output = new List<PathElement>();
            output = SVGReader.ProcessElements(path.Attributes["d"].Value);
            // Get all parent groups.
            XmlNodeList parents = path.SelectNodes("./ancestor::*", namespaceManager);
            // Traverse list from root to child.
            for (int i = parents.Count - 1; i >= 0; i--)
            {
                if (parents[i].Attributes["transform"] == null)
                    continue;
                // Apply transformation.
                output = SVGReader.ProcessTransformations(
                    output,
                    parents[i].Attributes["transform"].Value,
                    parents[i].Attributes["transform-origin"]?.Value ?? ""
                );
            }
            PathElements.AddRange(output);
        }
        // Flip vertically.
        PathElements = SVGReader.ProcessTransformations(
            PathElements,
            "scale(1,-1)",
            $"{((TR + BL).x / 2).ToString(CultureInfo.InvariantCulture)},{((TR + BL).y / 2).ToString(CultureInfo.InvariantCulture)}"
        );
    }

    public void Render()
    {
        foreach (PathElement element in PathElements)
        {
            element.Render();
        }
    }

    public void StopRender()
    {
        foreach (PathElement element in PathElements)
        {
            element.StopRender();
        }
    }

    public float GetLength()
    {
        float length = 0;
        foreach (PathElement element in PathElements)
        {
            length += element.GetLength();
        }
        return length;
    }

    public Vector2 GetPointAtFraction(float t)
    {
        float length = t * GetLength();
        foreach (PathElement element in PathElements)
        {
            if (length > element.GetLength())
                length -= element.GetLength();
            else
                return element.GetPointAtFraction(length / element.GetLength());
        }
        return Vector2.zero;
    }
}

[System.Serializable]
public class PathElement
{
    protected GameObject RenderObject;

    public virtual Vector2 GetStartPos()
    {
        throw new Exception();
    }

    public virtual void Render() { }

    public virtual void StopRender()
    {
        MonoBehaviour.Destroy(RenderObject);
    }

    public virtual float GetLength()
    {
        return 0;
    }

    public virtual Vector2 GetPointAtFraction(float t)
    {
        return Vector2.zero;
    }

    public virtual Vector2[] GetAllPoints()
    {
        return null;
    }

    public virtual void SetAllPoints(Vector2[] points) { }
}

[System.Serializable]
public class LineElement : PathElement
{
    public Vector2 StartPoint,
        EndPoint;

    public LineElement(Vector2 start, Vector2 end)
    {
        StartPoint = start;
        EndPoint = end;
    }

    public override Vector2 GetStartPos()
    {
        return StartPoint;
    }

    public override void Render()
    {
        RenderObject = new GameObject("Line");
        RenderObject.transform.parent = CameraScript.INSTANCE.gameObject.transform;
        RenderObject.AddComponent<LineRenderer>().material = new Material(
            Shader.Find("Unlit/LineRendererShader")
        );

        List<Vector3> positions = new List<Vector3>();
        positions.Add(new Vector3(StartPoint.x, StartPoint.y, 0));
        positions.Add(new Vector3(EndPoint.x, EndPoint.y, 0));

        RenderObject.GetComponent<LineRenderer>().startWidth = 1f;
        RenderObject.GetComponent<LineRenderer>().endWidth = 1f;
        RenderObject.GetComponent<LineRenderer>().positionCount = positions.Count;
        RenderObject.GetComponent<LineRenderer>().SetPositions(positions.ToArray());
    }

    public Line ToStruct()
    {
        return new Line(
            new Vector4(StartPoint.x, StartPoint.y, EndPoint.x, EndPoint.y),
            new Vector4(1, 1, 1, 1)
        );
    }

    public override float GetLength()
    {
        return Vector2.Distance(StartPoint, EndPoint);
    }

    public override Vector2 GetPointAtFraction(float t)
    {
        return t * EndPoint + (1 - t) * StartPoint;
    }

    public override Vector2[] GetAllPoints()
    {
        return new Vector2[] { StartPoint, EndPoint };
    }

    public override void SetAllPoints(Vector2[] points)
    {
        StartPoint = points[0];
        EndPoint = points[1];
    }
}

[System.Serializable]
public class CubicBezierElement : PathElement
{
    public Vector2 StartPoint,
        EndPoint,
        ControlStart,
        ControlEnd;

    public CubicBezierElement(Vector2 start, Vector2 end, Vector2 controlStart, Vector2 controlEnd)
    {
        StartPoint = start;
        EndPoint = end;
        ControlStart = controlStart;
        ControlEnd = controlEnd;
    }

    public override Vector2 GetStartPos()
    {
        return StartPoint;
    }

    public override void Render()
    {
        RenderObject = new GameObject("Cubic Bezier Curve");
        RenderObject.transform.parent = CameraScript.INSTANCE.gameObject.transform;
        RenderObject.AddComponent<LineRenderer>().material = new Material(
            Shader.Find("Unlit/LineRendererShader")
        );

        List<Vector3> positions = new List<Vector3>();
        for (int i = 0; i <= CameraScript.INSTANCE.CurveDetail; i++)
        {
            float t = i / CameraScript.INSTANCE.CurveDetail;
            positions.Add(GetPointAtFraction(t));
        }

        RenderObject.GetComponent<LineRenderer>().startWidth = 1f;
        RenderObject.GetComponent<LineRenderer>().endWidth = 1f;
        RenderObject.GetComponent<LineRenderer>().positionCount = positions.Count;
        RenderObject.GetComponent<LineRenderer>().SetPositions(positions.ToArray());
    }

    public override float GetLength() // Only an estimate.
    {
        List<Vector3> positions = new List<Vector3>();
        float length = 0;
        for (int i = 0; i <= CameraScript.INSTANCE.CurveDetail; i++)
        {
            float t = i / CameraScript.INSTANCE.CurveDetail;
            positions.Add(GetPointAtFraction(t));
            if (positions.Count > 1)
                length += Vector2.Distance(
                    positions[positions.Count - 2],
                    positions[positions.Count - 1]
                );
        }
        return length;
    }

    public override Vector2 GetPointAtFraction(float t)
    {
        return (1 - t) * (1 - t) * (1 - t) * new Vector2(StartPoint.x, StartPoint.y)
            + 3 * (1 - t) * (1 - t) * t * new Vector2(ControlStart.x, ControlStart.y)
            + 3 * (1 - t) * t * t * new Vector2(ControlEnd.x, ControlEnd.y)
            + t * t * t * new Vector2(EndPoint.x, EndPoint.y);
    }

    public CCurve ToStruct()
    {
        return new CCurve(
            new Vector4(StartPoint.x, StartPoint.y, EndPoint.x, EndPoint.y),
            new Vector4(ControlStart.x, ControlStart.y, ControlEnd.x, ControlEnd.y),
            new Vector4(1, 1, 1, 1)
        );
    }

    public override Vector2[] GetAllPoints()
    {
        return new Vector2[] { StartPoint, ControlStart, ControlEnd, EndPoint };
    }

    public override void SetAllPoints(Vector2[] points)
    {
        StartPoint = points[0];
        ControlStart = points[1];
        ControlEnd = points[2];
        EndPoint = points[3];
    }
}

[System.Serializable]
public class QuadraticBezierElement : PathElement
{
    public Vector2 StartPoint,
        EndPoint,
        Control;

    public QuadraticBezierElement(Vector2 start, Vector2 end, Vector2 control)
    {
        StartPoint = start;
        EndPoint = end;
        Control = control;
    }

    public override Vector2 GetStartPos()
    {
        return StartPoint;
    }

    public override void Render()
    {
        RenderObject = new GameObject("Quadratic Bezier Curve");
        RenderObject.transform.parent = CameraScript.INSTANCE.gameObject.transform;
        RenderObject.AddComponent<LineRenderer>().material = new Material(
            Shader.Find("Unlit/LineRendererShader")
        );

        List<Vector3> positions = new List<Vector3>();
        for (int i = 0; i <= CameraScript.INSTANCE.CurveDetail; i++)
        {
            float t = i / CameraScript.INSTANCE.CurveDetail;
            positions.Add(GetPointAtFraction(t));
        }

        RenderObject.GetComponent<LineRenderer>().startWidth = 0.1f;
        RenderObject.GetComponent<LineRenderer>().endWidth = 0.1f;
        RenderObject.GetComponent<LineRenderer>().positionCount = positions.Count;
        RenderObject.GetComponent<LineRenderer>().SetPositions(positions.ToArray());
    }

    public override float GetLength() // Only an estimate.
    {
        List<Vector3> positions = new List<Vector3>();
        float length = 0;
        for (int i = 0; i <= CameraScript.INSTANCE.CurveDetail; i++)
        {
            float t = i / CameraScript.INSTANCE.CurveDetail;
            positions.Add(GetPointAtFraction(t));
            if (positions.Count > 1)
                length += Vector2.Distance(
                    positions[positions.Count - 2],
                    positions[positions.Count - 1]
                );
        }
        return length;
    }

    public override Vector2 GetPointAtFraction(float t)
    {
        return (1 - t) * (1 - t) * new Vector2(StartPoint.x, StartPoint.y)
            + 2 * (1 - t) * t * new Vector2(Control.x, Control.y)
            + t * t * new Vector2(EndPoint.x, EndPoint.y);
    }

    public QCurve ToStruct()
    {
        return new QCurve(
            new Vector4(StartPoint.x, StartPoint.y, EndPoint.x, EndPoint.y),
            new Vector2(Control.x, Control.y),
            new Vector4(1, 1, 1, 1)
        );
    }

    public override Vector2[] GetAllPoints()
    {
        return new Vector2[] { StartPoint, Control, EndPoint };
    }

    public override void SetAllPoints(Vector2[] points)
    {
        StartPoint = points[0];
        Control = points[1];
        EndPoint = points[2];
    }
}
