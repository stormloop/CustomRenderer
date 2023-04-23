using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Renderer
{
    public static Line DefaultLine = new Line(Vector4.zero, Vector4.zero);
    public static CCurve DefaultCCurve = new CCurve(Vector4.zero, Vector4.zero, Vector4.zero);
    public static QCurve DefaultQCurve = new QCurve(Vector4.zero, Vector2.zero, Vector4.zero);
    public static Circle DefaultCircle = new Circle(Vector3.zero, Vector4.zero);
}

public struct Line
{
    public Vector4 Positions;
    public Vector4 ColorAndDepth;
    public float Trail;

    public Line(Vector4 positions, Vector4 colorAndDepth, bool trail = false)
    {
        Positions = positions;
        ColorAndDepth = colorAndDepth;
        Trail = trail ? 1 : 0;
    }
};

public struct CCurve
{
    public Vector4 Positions;
    public Vector4 Controls;
    public Vector4 ColorAndDepth;
    public float Trail;

    public CCurve(Vector4 positions, Vector4 controls, Vector4 colorAndDepth, bool trail = false)
    {
        Positions = positions;
        Controls = controls;
        ColorAndDepth = colorAndDepth;
        Trail = trail ? 1 : 0;
    }
};

public struct QCurve
{
    public Vector4 Positions;
    public Vector2 Control;
    public Vector4 ColorAndDepth;
    public float Trail;

    public QCurve(Vector4 positions, Vector2 control, Vector4 colorAndDepth, bool trail = false)
    {
        Positions = positions;
        Control = control;
        ColorAndDepth = colorAndDepth;
        Trail = trail ? 1 : 0;
    }
};

public struct Circle
{
    public Vector3 PositionAndRadius;
    public Vector4 ColorAndDepth;
    public float Trail;

    public Circle(Vector3 positionAndRadius, Vector4 colorAndDepth, bool trail = false)
    {
        PositionAndRadius = positionAndRadius;
        ColorAndDepth = colorAndDepth;
        Trail = trail ? 1 : 0;
    }
};
