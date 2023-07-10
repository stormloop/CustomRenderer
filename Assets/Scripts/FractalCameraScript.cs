using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FractalCameraScript : MonoBehaviour
{
    public static FractalCameraScript INSTANCE;

    public bool Julia;
    private bool lastJulia;
    public Vector2 InputC;
    private Vector2 lastInputC;
    public int Depth;

    public ComputeShader shader;
    public RenderTexture tex;

    public Vector2 BL = new Vector2(),
        TR = new Vector2();
    private Vector2 lastBL,
        lastTR;

    public void Awake()
    {
        INSTANCE = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        // BL = Vector2.one * -1.5f;
        // TR = Vector2.one * 1.5f;
    }

    // Update is called once per frame
    void Update() { }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        bool bl = true;
        if (
            tex == null
            || !new Vector2(tex.width, tex.height).Equals(new Vector2(Screen.width, Screen.height))
        )
        {
            Destroy(tex);

            tex = new RenderTexture(
                Screen.width,
                Screen.height,
                24,
                RenderTextureFormat.ARGBFloat,
                RenderTextureReadWrite.Linear
            );
            tex.enableRandomWrite = true;
            tex.Create();
            bl = false;
        }

        if (!bl || lastInputC != InputC || lastJulia != Julia || lastBL != BL || lastTR != TR)
        {
            lastInputC = InputC;
            lastJulia = Julia;
            lastBL = BL;
            lastTR = TR;

            // shader.SetTexture(shader.FindKernel("Render"), "Result", tex);
            // shader.SetFloat("DeltaTime", Time.deltaTime);
            // shader.SetFloats("BL", BL.x, BL.y);
            // shader.SetFloats("TR", TR.x, TR.y);
            // shader.SetFloats("InputC", InputC.x, InputC.y);
            // shader.SetBool("Julia", Julia);
            // shader.SetInt("Margin", 50);
            // shader.SetInt("Depth", Depth);
            // shader.SetInts("Resolution", tex.width, tex.height);
            // shader.Dispatch(shader.FindKernel("Render"), tex.width / 7, tex.height / 7, 1);

            ComputeBuffer buffer = new ComputeBuffer(Depth, sizeof(int));
            buffer.SetData(new int[Depth]);
            shader.SetBuffer(shader.FindKernel("GenerateHistogram"), "IterationDepths", buffer);
            shader.SetTexture(shader.FindKernel("GenerateHistogram"), "Result", tex);
            shader.SetFloat("DeltaTime", Time.deltaTime);
            shader.SetFloats("BL", BL.x, BL.y);
            shader.SetFloats("TR", TR.x, TR.y);
            shader.SetFloats("InputC", InputC.x, InputC.y);
            shader.SetBool("Julia", Julia);
            shader.SetInt("Margin", 50);
            shader.SetInt("Depth", Depth);
            shader.SetInts("Resolution", tex.width, tex.height);
            shader.Dispatch(shader.FindKernel("GenerateHistogram"), tex.width / 7, tex.height / 7, 1);

            shader.SetBuffer(shader.FindKernel("RenderHistogram"), "IterationDepths", buffer);
            shader.SetTexture(shader.FindKernel("RenderHistogram"), "Result", tex);
            shader.Dispatch(shader.FindKernel("RenderHistogram"), tex.width / 7, tex.height / 7, 1);
            
            buffer.Dispose();
        }

        Graphics.Blit(tex, dest);
        //Graphics.Blit(src, dest);
    }
}
