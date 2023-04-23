using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class PerspectiveRenderer
{
    public static PerspectiveRenderer INSTANCE;

    public PerspectiveRenderer()
    {
        INSTANCE = this;
    }

    public Vector3 NearFarFOV = new Vector3(0.1f, 50, 1.04719f); // Default FOV 60.
    public Vector3 CamPos = new Vector3(0, 0, -10);
    public Quaternion CamRot = new Quaternion(0, 0, 0, 1);
    public ProjectionMatrix Matrix;
    public Func<ProjectionMatrix> MatrixGen;

    public List<Line> Render(OBJ obj)
    {
        List<Line> output = new List<Line>();

        foreach (Edge edge in obj.Edges)
        {
            output.Add(new Line(Render(edge), new Vector4(0.5f, 1, 1, 0.1f)));
        }

        return output;
    }

    public Vector4 Render(Edge edge)
    {
        Vector3 start = Project(edge.Start);
        Vector3 end = Project(edge.End);

        if (start.z < -1 || start.z > 1 || end.z < -1 || end.z > 1)
            return Vector4.zero;

        return new Vector4(start.x, start.y, end.x, end.y);
    }

    public Vector4 Project(Vector4 input)
    {
        Vector4 output = Project(TransformToCamCoords(TransformCoords(input)), Matrix);
        return output;
    }

    public Vector4 Project(Vector4 input, ProjectionMatrix matrix)
    {
        float[,] output = MultiplyMatrix(
            matrix.Matrix,
            new float[4, 1]
            {
                { input.x },
                { input.y },
                { input.z },
                { input.w }
            }
        );
        if (matrix.HasDepth)
            return new Vector4(
                output[0, 0] / output[3, 0],
                output[1, 0] / output[3, 0],
                output[2, 0],
                output[3, 0] / output[3, 0]
            );
        return new Vector4(output[0, 0], output[1, 0], output[2, 0], output[3, 0]);
    }

    public float[,] GetTransformMatrix()
    {
        return new float[4, 4]
        {
            { 1, 0, 0, 0 },
            { 0, 1, 0, 0 },
            { 0, 0, 1, 0 },
            { 0, 0, 0, 1 }
        };
        return new float[4, 4]
        {
            { Mathf.Cos(Time.time), 0, -Mathf.Sin(Time.time), 0 },
            { 0, 1, 0, 0 },
            { Mathf.Sin(Time.time), 0, Mathf.Cos(Time.time), 0 },
            { 0, 0, 0, 1 }
        };
    }

    public Vector4 TransformCoords(Vector4 input)
    {
        float[,] output = MultiplyMatrix(
            GetTransformMatrix(),
            new float[4, 1]
            {
                { input.x },
                { input.y },
                { input.z },
                { input.w }
            }
        );
        return new Vector4(output[0, 0], output[1, 0], output[2, 0], output[3, 0]);
    }

    public float[,] GetCamTransformMatrix()
    {
        return new float[4, 4]
        {
            { 1, 0, 0, -CamPos.x },
            { 0, 1, 0, -CamPos.y },
            { 0, 0, 1, -CamPos.z },
            { 0, 0, 0, 1 }
        };
    }

    public Vector4 TransformToCamCoords(Vector4 input)
    {
        // Translate using matrix.
        float[,] translated = MultiplyMatrix(
            GetCamTransformMatrix(),
            new float[4, 1]
            {
                { input.x },
                { input.y },
                { input.z },
                { input.w }
            }
        );
        // Rotate using quaternions.
        Vector3 result =
            Quaternion.Inverse(CamRot)
            * new Vector3(translated[0, 0], translated[1, 0], translated[2, 0]).normalized;
        result *= new Vector3(translated[0, 0], translated[1, 0], translated[2, 0]).magnitude;

        return new Vector4(result.x, result.y, result.z, translated[3, 0]);
    }

    public ProjectionMatrix GetIsometricMatrix()
    {
        float ratio =
            (CameraScript.INSTANCE.TR.y - CameraScript.INSTANCE.BL.y)
            / (CameraScript.INSTANCE.TR.x - CameraScript.INSTANCE.BL.x);
        Vector2 quadrantSize = new Vector2(NearFarFOV.z, NearFarFOV.z * ratio) * Mathf.Rad2Deg;
        return new ProjectionMatrix(
            new float[4, 4]
            {
                {
                    1 / quadrantSize.x * Mathf.Cos(30 * Mathf.Deg2Rad),
                    0,
                    -1 / quadrantSize.x * Mathf.Cos(30 * Mathf.Deg2Rad),
                    -CamPos.z / quadrantSize.x * Mathf.Cos(30 * Mathf.Deg2Rad)
                },
                {
                    1 / quadrantSize.y * Mathf.Sin(30 * Mathf.Deg2Rad),
                    1 / quadrantSize.y,
                    1 / quadrantSize.y * Mathf.Sin(30 * Mathf.Deg2Rad),
                    CamPos.z / quadrantSize.y * Mathf.Sin(30 * Mathf.Deg2Rad)
                },
                {
                    0,
                    0,
                    2 / (NearFarFOV.y - NearFarFOV.x),
                    -(NearFarFOV.y + NearFarFOV.x) / (NearFarFOV.y - NearFarFOV.x)
                },
                { 0, 0, 0, 1 }
            }
        ); // No scaling based on distance from clipping plane.
    }

    public ProjectionMatrix GetOrthographicMatrix()
    {
        float ratio =
            (CameraScript.INSTANCE.TR.y - CameraScript.INSTANCE.BL.y)
            / (CameraScript.INSTANCE.TR.x - CameraScript.INSTANCE.BL.x);
        Vector2 quadrantSize = new Vector2(NearFarFOV.z, NearFarFOV.z * ratio) * Mathf.Rad2Deg;
        return new ProjectionMatrix(
            new float[4, 4]
            {
                { 1 / quadrantSize.x, 0, 0, 0 },
                { 0, 1 / quadrantSize.y, 0, 0 },
                {
                    0,
                    0,
                    2 / (NearFarFOV.y - NearFarFOV.x),
                    -(NearFarFOV.y + NearFarFOV.x) / (NearFarFOV.y - NearFarFOV.x)
                },
                { 0, 0, 0, 1 }
            }
        ); // No depth at all.
    }

    public ProjectionMatrix GetPerpendicularMatrix()
    {
        float ratio =
            (CameraScript.INSTANCE.TR.y - CameraScript.INSTANCE.BL.y)
            / (CameraScript.INSTANCE.TR.x - CameraScript.INSTANCE.BL.x);
        Vector2 quadrantSize = new Vector2(NearFarFOV.z, NearFarFOV.z * ratio) * Mathf.Rad2Deg;
        return new ProjectionMatrix(
            new float[4, 4]
            {
                {
                    1 / quadrantSize.x,
                    0,
                    Mathf.Cos(30 * Mathf.Deg2Rad) / 2 / quadrantSize.x,
                    CamPos.z * Mathf.Cos(30 * Mathf.Deg2Rad) / 2 / quadrantSize.x
                },
                {
                    0,
                    1 / quadrantSize.y,
                    Mathf.Sin(30 * Mathf.Deg2Rad) / 2 / quadrantSize.y,
                    CamPos.z * Mathf.Sin(30 * Mathf.Deg2Rad) / 2 / quadrantSize.y
                },
                {
                    0,
                    0,
                    2 / (NearFarFOV.y - NearFarFOV.x),
                    -(NearFarFOV.y + NearFarFOV.x) / (NearFarFOV.y - NearFarFOV.x)
                },
                { 0, 0, 0, 1 }
            }
        ); // No scaling based on distance from clipping plane.
    }

    public ProjectionMatrix GetPerspectiveMatrix()
    {
        float ratio =
            (CameraScript.INSTANCE.TR.y - CameraScript.INSTANCE.BL.y)
            / (CameraScript.INSTANCE.TR.x - CameraScript.INSTANCE.BL.x);
        Vector2 quadrantSize = new Vector2(
            Mathf.Tan(NearFarFOV.z / 2),
            Mathf.Tan(NearFarFOV.z / 2) * ratio
        );
        return new ProjectionMatrix(
            new float[4, 4]
            {
                { 1 / quadrantSize.x, 0, 0, 0 },
                { 0, 1 / quadrantSize.y, 0, 0 },
                {
                    0,
                    0,
                    2 / (NearFarFOV.y - NearFarFOV.x),
                    -(NearFarFOV.y + NearFarFOV.x) / (NearFarFOV.y - NearFarFOV.x)
                },
                { 0, 0, 1, 0 }
            },
            true
        ); // With depth.
    }

    public static float[,] MultiplyMatrix(float[,] A, float[,] B)
    {
        int rA = A.GetLength(0);
        int cA = A.GetLength(1);
        int rB = B.GetLength(0);
        int cB = B.GetLength(1);

        if (cA != rB)
        {
            throw new System.Exception("Matrices have non-compatible dimensions.");
        }
        else
        {
            float temp = 0;
            float[,] kHasil = new float[rA, cB];

            for (int i = 0; i < rA; i++)
            {
                for (int j = 0; j < cB; j++)
                {
                    temp = 0;
                    for (int k = 0; k < cA; k++)
                    {
                        temp += A[i, k] * B[k, j];
                    }
                    kHasil[i, j] = temp;
                }
            }

            return kHasil;
        }
    }
}

public class ProjectionMatrix
{
    public float[,] Matrix = new float[4, 4];
    public bool HasDepth = false;

    public ProjectionMatrix(bool hasDepth = false)
    {
        Matrix = new float[4, 4]
        {
            { 1, 0, 0, 0 },
            { 0, 1, 0, 0 },
            { 0, 0, 1, 0 },
            { 0, 0, 0, 1 }
        };
        HasDepth = hasDepth;
    }

    public ProjectionMatrix(float[,] matrix, bool hasDepth = false)
    {
        Matrix = matrix;
        HasDepth = hasDepth;
    }
}
