using System;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour
{

    GameObject sphere;

    // Shader constant
    public float structuralSpringk = 0.5f;
    public float shearSpringk = 0.5f;
    public float bendSpringk = 0.5f;
    public float springDamping = 0.5f;
    public float clothWeight = 100.0f;
    public float gravity = 5.0f;
    public float frictionCoefficient = 1.0f;
    public float amortCoeficient = 1.0f;
    public int Segments = 8;

    // Buffer data 
    public ComputeShader shader;
    ComputeBuffer previousSpeedBuffer;
    ComputeBuffer currentPositionBuffer;
    ComputeBuffer springForceBuffer;

    // Pointer to kernel functions 
    int computeSpring;
    int applyForces;

    // Mesh data 
    Mesh mesh;
    Vector3[] vertices;
    int[] triangles;
    MeshFilter meshFilter;

    void Start()
    {
        CreateShape();
        meshFilter = GetComponent<MeshFilter>();

        sphere = GameObject.Find("Sphere");

        // Shader stuff
        currentPositionBuffer = new ComputeBuffer(vertices.Length, 3 * sizeof(float));
        springForceBuffer = new ComputeBuffer(vertices.Length, 3 * sizeof(float));
        previousSpeedBuffer = new ComputeBuffer(vertices.Length, 3 * sizeof(float));
        computeSpring = shader.FindKernel("computeSpring");
        applyForces = shader.FindKernel("applyForces");
        shader.SetFloat("sphereRad", sphere.GetComponent<SphereCollider>().bounds.extents.x);

        // World var
        shader.SetFloat("structuralSpringk", structuralSpringk);
        shader.SetFloat("shearSpringk", shearSpringk);
        shader.SetFloat("bendSpringk", bendSpringk);
        shader.SetFloat("springDamping", springDamping);
        shader.SetFloat("clothWeight", clothWeight);
        shader.SetFloat("gravity", gravity);
        shader.SetFloat("frictionCoefficient", frictionCoefficient);
        shader.SetInt("segments", Segments);
        shader.SetFloat("amortCoeficient", amortCoeficient);


        Vector3[] emptyArray = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            emptyArray[i] = new Vector3(0, 0, 0);
        }
        SendComputeBuffer("springForceBuffer", springForceBuffer, emptyArray);
        SendComputeBuffer("previousSpeedBuffer", previousSpeedBuffer, emptyArray);
        SendComputeBuffer("currentPositionBuffer", currentPositionBuffer, vertices);
    }

    void SendComputeBuffer(string bufferName, ComputeBuffer computeBuffer, Array bufferArray)
    {
        computeBuffer.SetData(bufferArray);
        shader.SetBuffer(computeSpring, bufferName, computeBuffer);
        shader.SetBuffer(applyForces, bufferName, computeBuffer);
    }

    void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        meshFilter.mesh = mesh;
    }

    void CreateShape()
    {
        mesh = new Mesh();
        triangles = new int[Segments * Segments * 6 * 2];
        vertices = new Vector3[(Segments + 1) * (Segments + 1)];

        for (int i = 0, y = 0; y <= Segments; y++)
        {
            for (int x = 0; x <= Segments; x++, i++)
            {
                vertices[i] = new Vector3(x, 11, y);
                //vertices[i] = new Vector3(x, 0, y);
            }
        }
        int ti = 0;
        int vi = 0;
        for (int y = 0; y < Segments; y++, vi++)
        {
            for (int x = 0; x < Segments; x++, ti += 12, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 1] = vi + Segments + 1;
                triangles[ti + 2] = vi + 1;
                triangles[ti + 3] = vi + 1;
                triangles[ti + 4] = vi + Segments + 1;
                triangles[ti + 5] = vi + Segments + 2;

                triangles[ti + 6] = vi + 1;
                triangles[ti + 7] = vi + Segments + 1;
                triangles[ti + 8] = vi;
                triangles[ti + 9] = vi + Segments + 2;
                triangles[ti + 10] = vi + Segments + 1;
                triangles[ti + 11] = vi + 1;
            }
        }
    }

    void OnDestroy()
    {
        Console.WriteLine("onDestroy");
        currentPositionBuffer.Release();
        previousSpeedBuffer.Release();
        springForceBuffer.Release();
    }

    void Update()
    {
        UpdateShader();
        UpdateMesh();
    }
    private void UpdateShader()
    {
        shader.SetFloat("deltaTime", Time.deltaTime);
        shader.SetVector("spherePos", sphere.GetComponent<Transform>().position);
        shader.Dispatch(computeSpring, 8, 1, 1);
        shader.Dispatch(applyForces, 8, 1, 1);
        currentPositionBuffer.GetData(vertices);
    }
}