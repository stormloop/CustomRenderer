using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;

public static class FourierGenerator
{
    static ComputeShader generator = CameraScript.INSTANCE.fourierGenerator;

    public static FourierSeries Load(string path)
    {
        Debug.Log("Process path.");
        string[] lines = File.ReadAllLines(path);
        Debug.Log("Process text.");
        List<Rotator> rotators = new List<Rotator>();
        foreach (string line in lines)
        {
            Rotator rotator = new Rotator();
            rotator.Index = int.Parse(line.Split('{')[1].Split('}')[0]);
            float angle = 0;
            angle = float.Parse(line.Split('{')[2].Split('i')[0]);
            rotator.Coefficient =
                float.Parse(line.Split(' ')[1].Split('e')[0])
                * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            rotators.Add(rotator);
        }

        return new FourierSeries(rotators);
    }

    public static string[] Save(FourierSeries series)
    {
        string[] lines = new string[series.Rotators.Count];
        for (int i = 0; i < series.Rotators.Count; i++)
        {
            Rotator rotator = new List<Rotator>(
                series.Rotators.OrderBy((r) => Mathf.Abs(r.Index)).ThenByDescending((r) => r.Index)
            )[i];
            lines[i] =
                $"c_{{{rotator.Index}}}: {rotator.Coefficient.magnitude}e^{{{Mathf.Atan2(rotator.Coefficient.y, rotator.Coefficient.x)}it}}";
        }
        return lines;
    }

    public static FourierSeries Generate(SVG path)
    {
        Vector2[] pathPoints = new Vector2[CameraScript.INSTANCE.IntegralDetail];
        for (int i = 0; i < CameraScript.INSTANCE.IntegralDetail; i++)
            pathPoints[i] = path.GetPointAtFraction(
                (float)(i) / CameraScript.INSTANCE.IntegralDetail
            );
        ComputeBuffer pathPointsBuffer = new ComputeBuffer(pathPoints.Length, 2 * sizeof(float));
        pathPointsBuffer.SetData(pathPoints);
        generator.SetBuffer(
            generator.FindKernel("GenerateFourier"),
            "PathPoints",
            pathPointsBuffer
        );
        ComputeBuffer rotatorsBuffer = new ComputeBuffer(
            CameraScript.INSTANCE.StopIndex - CameraScript.INSTANCE.StartIndex + 1,
            2 * sizeof(float) + sizeof(int)
        );
        rotatorsBuffer.SetData(
            new Rotator[CameraScript.INSTANCE.StopIndex - CameraScript.INSTANCE.StartIndex + 1]
        );
        generator.SetBuffer(generator.FindKernel("GenerateFourier"), "Rotators", rotatorsBuffer);
        generator.SetInt("MinRotator", CameraScript.INSTANCE.StartIndex);
        generator.Dispatch(generator.FindKernel("GenerateFourier"), rotatorsBuffer.count / 7, 1, 1);
        Rotator[] rotators = new Rotator[
            CameraScript.INSTANCE.StopIndex - CameraScript.INSTANCE.StartIndex + 1
        ];
        rotatorsBuffer.GetData(rotators);
        pathPointsBuffer.Dispose();
        rotatorsBuffer.Dispose();

        return new FourierSeries(new List<Rotator>(rotators));
    }

    public static Vector2 Integrate(SVG path, int index)
    {
        Vector2 output = new Vector2();
        for (int i = 0; i <= CameraScript.INSTANCE.IntegralDetail; i++)
        {
            float t = (float)i / CameraScript.INSTANCE.IntegralDetail;
            output +=
                1f
                / CameraScript.INSTANCE.IntegralDetail
                * Multiply(
                    path.GetPointAtFraction(t),
                    new Vector2(
                        Mathf.Cos(-2 * Mathf.PI * index * t),
                        Mathf.Sin(-2 * Mathf.PI * index * t)
                    )
                );
        }
        return output;
    }

    public static Vector2 Multiply(Vector2 a, Vector2 b)
    {
        return new Vector2(a.x * b.x - a.y * b.y, a.x * b.y + a.y * b.x);
    }
}

[System.Serializable]
public class FourierSeries
{
    public List<Rotator> Rotators = new List<Rotator>();

    public FourierSeries(List<Rotator> rotators)
    {
        Rotators = rotators;
    }

    public Vector2 GetPointAtFraction(float t)
    {
        Vector2 output = new Vector2();
        foreach (Rotator rotator in Rotators)
        {
            output += FourierGenerator.Multiply(
                rotator.Coefficient,
                new Vector2(
                    Mathf.Cos(2 * Mathf.PI * rotator.Index * t),
                    Mathf.Sin(2 * Mathf.PI * rotator.Index * t)
                )
            );
        }
        return output;
    }
}

public struct Rotator
{
    public int Index;
    public Vector2 Coefficient;
}
