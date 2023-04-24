using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System;

public class CameraScript : MonoBehaviour
{
    public static CameraScript INSTANCE;

    public bool Gridlines,
        Axis,
        Svg,
        Obj,
        Fourier,
        Trail;
    private bool lastTrail;

    public int CurveDetail;
    public uint IntegralDetail;
    public int StartIndex;
    public int StopIndex;

    public SVG svg;
    public OBJ obj;
    public FourierSeries fourier;

    public ComputeShader shader,
        fourierGenerator;
    public RenderTexture tex,
        trails;

    private Vector2 lastTrailPos;

    public Vector2 BL,
        TR;

    private void Awake()
    {
        Debug.unityLogger.logHandler = new FileLogHandler(@$"{Environment.CurrentDirectory}\Logs\debugLog.log");
        INSTANCE = this;
        new PerspectiveRenderer();
    }

    // Start is called before the first frame update
    void Start()
    {
        BL = Vector2.one * -10.5f;
        TR = Vector2.one * 10.5f;

        PerspectiveRenderer.INSTANCE.MatrixGen = PerspectiveRenderer.INSTANCE.GetPerspectiveMatrix;
        PerspectiveRenderer.INSTANCE.Matrix = PerspectiveRenderer.INSTANCE.MatrixGen();
    }

    // Update is called once per frame
    void Update() { }

    // private void OnDrawGizmos()
    // {
    //     if (!Application.isPlaying)
    //         return;
    //     Vector2 currentPos = Vector2.zero;
    //     float t = (Time.time / 10) % 1;
    //     foreach (
    //         Rotator rotator in fourier.Rotators
    //             .OrderBy((r) => Mathf.Abs(r.Index))
    //             .ThenByDescending((r) => r.Index)
    //     )
    //     {
    //         Gizmos.DrawLine(
    //             currentPos,
    //             currentPos
    //                 + FourierGenerator.Multiply(
    //                     rotator.Coefficient,
    //                     new Vector2(
    //                         Mathf.Cos(2 * Mathf.PI * rotator.Index * t),
    //                         Mathf.Sin(2 * Mathf.PI * rotator.Index * t)
    //                     )
    //                 )
    //         );
    //         currentPos += FourierGenerator.Multiply(
    //             rotator.Coefficient,
    //             new Vector2(
    //                 Mathf.Cos(2 * Mathf.PI * rotator.Index * t),
    //                 Mathf.Sin(2 * Mathf.PI * rotator.Index * t)
    //             )
    //         );
    //     }
    // }

    public void LoadSVG(SVG svg)
    {
        this.svg = svg;
        BL = svg.BL - Vector2.one / 2;
        TR = svg.TR + Vector2.one / 2;
    }

    public void LoadOBJ(OBJ obj)
    {
        this.obj = obj;
    }

    public void LoadFourier(FourierSeries fourier, Vector2 BL, Vector2 TR)
    {
        this.fourier = fourier;
        this.BL = BL - Vector2.one / 2;
        this.TR = TR + Vector2.one / 2;
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (
            tex == null
            || !new Vector2(tex.width, tex.height).Equals(new Vector2(Screen.width, Screen.height))
        )
        {
            Destroy(tex);
            Destroy(trails);

            tex = new RenderTexture(Screen.width, Screen.height, 24);
            trails = new RenderTexture(
                Screen.width,
                Screen.height,
                24,
                RenderTextureFormat.ARGBFloat,
                RenderTextureReadWrite.Linear
            );
            tex.enableRandomWrite = true;
            trails.enableRandomWrite = true;
            tex.Create();
            trails.Create();
        }

        if (Trail && !lastTrail)
            lastTrailPos = new Vector2(float.MaxValue, float.MaxValue);

        lastTrail = Trail;

        List<Line> linesOutput = new List<Line>();
        List<CCurve> cCurveOutput = new List<CCurve>();
        List<QCurve> qCurveOutput = new List<QCurve>();
        List<Circle> circleOutput = new List<Circle>();

        if (Svg)
            for (int i = 0; i < svg.PathElements.Count(); i++)
            {
                if (svg.PathElements[i] is LineElement line)
                {
                    Line lineStruct = line.ToStruct();
                    lineStruct.ColorAndDepth.w = 0.5f;
                    linesOutput.Add(lineStruct);
                    continue;
                }
                if (svg.PathElements[i] is CubicBezierElement cCurve)
                {
                    CCurve cCurveStruct = cCurve.ToStruct();
                    cCurveStruct.ColorAndDepth.w = 0.5f;
                    cCurveOutput.Add(cCurveStruct);
                    continue;
                }
                if (svg.PathElements[i] is QuadraticBezierElement qCurve)
                {
                    QCurve qCurveStruct = qCurve.ToStruct();
                    qCurveStruct.ColorAndDepth.w = 0.5f;
                    qCurveOutput.Add(qCurveStruct);
                    continue;
                }
            }
        if (Obj && obj != null)
        {
            // Transform from NDG to screen coordinates.
            for (int i = 0; i < obj.Edges.Count(); i++)
            {
                Vector4 projected = PerspectiveRenderer.INSTANCE.Render(obj.Edges[i]);
                Line lineStruct = new Line(
                    new Vector4(
                        projected.x * ((TR - BL) / 2).x + (TR.x + BL.x) / 2,
                        projected.y * ((TR - BL) / 2).y + (TR.y + BL.y) / 2,
                        projected.z * ((TR - BL) / 2).x + (TR.x + BL.x) / 2,
                        projected.w * ((TR - BL) / 2).y + (TR.y + BL.y) / 2
                    ),
                    new Vector4(0, 180f/255, 20f/255, 0.1f)
                );
                linesOutput.Add(lineStruct);
            }
        }
        if ((Fourier || Trail) && fourier.Rotators != null)
        {
            Vector2 currentPos = Vector2.zero;
            float t = (Time.time / 10) % 1;
            foreach (
                Rotator rotator in fourier.Rotators
                    .OrderBy((r) => Mathf.Abs(r.Index))
                    .ThenByDescending((r) => r.Index)
            )
            {
                Vector2 newPos =
                    currentPos
                    + FourierGenerator.Multiply(
                        rotator.Coefficient,
                        new Vector2(
                            Mathf.Cos(2 * Mathf.PI * rotator.Index * t),
                            Mathf.Sin(2 * Mathf.PI * rotator.Index * t)
                        )
                    );
                if (Fourier)
                    linesOutput.Add(
                        new Line(
                            new Vector4(currentPos.x, currentPos.y, newPos.x, newPos.y),
                            new Vector4(54f / 256, 74f / 256, 122f / 256, 0.9f)
                        )
                    );
                currentPos = newPos;
            }

            // Add Trail.
            if (Trail)
            {
                if (lastTrailPos.Equals(new Vector2(float.MaxValue, float.MaxValue)))
                    lastTrailPos = currentPos;
                linesOutput.Add(
                    new Line(
                        new Vector4(lastTrailPos.x, lastTrailPos.y, currentPos.x, currentPos.y),
                        new Vector4(229f / 255, 157f / 255, 0, 1),
                        true
                    )
                );
                lastTrailPos = currentPos;
            }
        }

        if (linesOutput.Count() == 0)
            linesOutput = new List<Line>() { Renderer.DefaultLine };
        ComputeBuffer lines = new ComputeBuffer(linesOutput.Count(), 9 * sizeof(float));
        lines.SetData(linesOutput.ToArray());
        if (cCurveOutput.Count() == 0)
            cCurveOutput = new List<CCurve>() { Renderer.DefaultCCurve };
        ComputeBuffer cCurves = new ComputeBuffer(cCurveOutput.Count(), 13 * sizeof(float));
        cCurves.SetData(cCurveOutput.ToArray());
        if (qCurveOutput.Count() == 0)
            qCurveOutput = new List<QCurve>() { Renderer.DefaultQCurve };
        ComputeBuffer qCurves = new ComputeBuffer(qCurveOutput.Count(), 11 * sizeof(float));
        qCurves.SetData(qCurveOutput.ToArray());
        if (circleOutput.Count() == 0)
            circleOutput = new List<Circle>() { Renderer.DefaultCircle };
        ComputeBuffer circles = new ComputeBuffer(circleOutput.Count(), 8 * sizeof(float));
        circles.SetData(circleOutput.ToArray());

        shader.SetTexture(shader.FindKernel("Render"), "Result", tex);
        shader.SetTexture(shader.FindKernel("Render"), "Trails", trails);
        shader.SetFloat("DeltaTime", Time.deltaTime);
        shader.SetFloats("BL", BL.x, BL.y);
        shader.SetFloats("TR", TR.x, TR.y);
        shader.SetInt("Margin", 50);
        shader.SetInt("CurveDetail", CurveDetail);
        shader.SetInts("Resolution", tex.width, tex.height);
        shader.SetBool("Gridlines", Gridlines);
        shader.SetBool("Axis", Axis);
        shader.SetBuffer(shader.FindKernel("Render"), "Lines", lines);
        shader.SetBuffer(shader.FindKernel("Render"), "CCurves", cCurves);
        shader.SetBuffer(shader.FindKernel("Render"), "QCurves", qCurves);
        shader.SetBuffer(shader.FindKernel("Render"), "Circles", circles);
        shader.Dispatch(shader.FindKernel("Render"), tex.width / 7, tex.height / 7, 1);

        lines.Dispose();
        cCurves.Dispose();
        qCurves.Dispose();
        circles.Dispose();

        Graphics.Blit(tex, dest);

        //Graphics.Blit(src, dest);
    }
}
