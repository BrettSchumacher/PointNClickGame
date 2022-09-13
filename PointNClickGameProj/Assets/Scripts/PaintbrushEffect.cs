using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Spren
{
    public Vector2 pos;
    public Vector2 vel;
    public Vector2 goal;
    public int atGoal;
}

public class PaintbrushEffect : MonoBehaviour
{
    public int resX = 1920;
    public int resY = 1080;
    public int numAgents = 1000;
    public float diffuse = 0.1f;
    public float decay = 0.2f;
    public float agentSpeed = 2f;
    public float accMult = 1f;
    public float spawnRate = 10000f;
    public float agentRad = 3f;
    public float goalRad = 10f;
    public float border = 50f;
    public Color baseColor;
    public ComputeShader paintbrushShader;
    // public Texture2D baseImage;
    public RenderTexture render;
    public Material target;
    public Texture2D paintbrushTex;

    // private Texture2D scaledImage;
    private RenderTexture _paintbrush;
    private Camera _camera;
    private List<Spren> _agents;

    void SetShaderParamters()
    {
        paintbrushShader.SetInt("width", resX);
        paintbrushShader.SetInt("height", resY);
        paintbrushShader.SetInt("numAgents", numAgents);
        paintbrushShader.SetFloat("diffuse", diffuse);
        paintbrushShader.SetFloat("decay", decay);
        paintbrushShader.SetFloat("agentSpeed", agentSpeed);
        paintbrushShader.SetFloat("accMult", accMult);
        paintbrushShader.SetFloat("agentRad", agentRad);
        paintbrushShader.SetFloat("goalRad", goalRad);
        paintbrushShader.SetVector("baseColor", baseColor);
        paintbrushShader.SetFloat("seed", 42f);
    }

    void GenerateAgents()
    {
        Spren temp;
        float offset = border + goalRad;

        for (int i = 0; i < numAgents; i++)
        {
            temp = new Spren();
            temp.pos = new Vector2(resX / 2, resY / 2);
            temp.vel = Vector2.zero;
            temp.goal = new Vector2(Random.Range(offset, resX - offset), Random.Range(offset, resY - offset));
            temp.atGoal = 0;
            _agents.Add(temp);
        }
    }

    void SpawnAgents(Vector2 point, float dt)
    {
        Spren temp;

        int num = (int) Mathf.Min(numAgents - _agents.Count, spawnRate * dt);
        float offset = border + goalRad / 2f;

        for (int i = 0; i < num; i++)
        {
            temp = new Spren();
            temp.pos = point;
            temp.goal = new Vector2(Random.Range(offset, resX - offset), Random.Range(offset, resY - offset));
            temp.vel = temp.goal - temp.pos;
            temp.vel = temp.vel.magnitude > agentSpeed ? agentSpeed * temp.vel.normalized : temp.vel;
            temp.atGoal = 0;
            _agents.Add(temp);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        _agents = new List<Spren>();
        _camera = GetComponent<Camera>();
        SetShaderParamters();
        //GenerateAgents();
        InitRenderTexture();

        // scaledImage = new Texture2D(Screen.width, Screen.height);
        // Color pix;
        // for (int i = 0; i < scaledImage.width; i++)
        // {
        //     for (int j = 0; j < scaledImage.height; j++)
        //     {
        //         pix = baseImage.GetPixel((baseImage.width * i) / scaledImage.width, (baseImage.height * j) / scaledImage.height);
        //         scaledImage.SetPixel(i, j, pix);
        //     }
        // }

        target.mainTexture = _paintbrush;
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            SpawnAgents(Input.mousePosition, Time.deltaTime);
        }
    }

    void FixedUpdate()
    {
        if (_agents.Count < 1 || _camera.activeTexture == null)
        {
            return;
        }

        // Set the target and dispatch the compute shader
        ComputeBuffer agents = new ComputeBuffer(_agents.Count, 4 * 7);
        agents.SetData(_agents);

        _camera.Render();

        paintbrushShader.SetBuffer(0, "agents", agents);
        paintbrushShader.SetTexture(0, "light", _paintbrush);
        paintbrushShader.SetTexture(0, "brushTexture", paintbrushTex);
        paintbrushShader.SetTexture(0, "readTexture", render);
        paintbrushShader.SetFloat("dt", Time.fixedDeltaTime);
        paintbrushShader.SetFloat("time", Time.time);
        int threadGroupsX = Mathf.CeilToInt(numAgents / 64.0f);
        paintbrushShader.Dispatch(0, threadGroupsX, 1, 1);

        paintbrushShader.SetBuffer(1, "agents", agents);
        paintbrushShader.SetTexture(1, "light", _paintbrush);
        threadGroupsX = Mathf.CeilToInt(resX / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(resY / 8.0f);
        paintbrushShader.Dispatch(1, threadGroupsX, threadGroupsY, 1);

        Spren[] temp = new Spren[_agents.Count];
        agents.GetData(temp);
        agents.Release();

        _agents.Clear();
        _agents.AddRange(temp);

        // Blit the result texture to the screen
    }

    /*private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        

        Graphics.Blit(source, destination);
    }*/

    private void InitRenderTexture()
    {
        _paintbrush = new RenderTexture(resX, resY, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _paintbrush.enableRandomWrite = true;
        _paintbrush.Create();
    }
}
