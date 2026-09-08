using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static partial class StonehillTwoHoleCourseBuilder
{
    public static void BuildSmoothLakePackage()
    {
        SmoothHoleOneLake();
        PackageHoleOneDetail();
    }
    [MenuItem("Stonehill/Hole 1/Smooth Lake and Capture")]
    public static void SmoothHoleOneLake() { RefineHole(1); }

    private static Vector2[] SmoothLakeBoundary(Vector2[] source)
    {
        // Round the survey/tracing corners into a continuous natural bank, retaining
        // the same lake location and overall lobes rather than one-metre grid steps.
        List<Vector2> points=new List<Vector2>(source);
        for(int pass=0;pass<3;pass++)
        {
            List<Vector2> next=new List<Vector2>();
            for(int i=0;i<points.Count;i++)
            {
                Vector2 a=points[i],b=points[(i+1)%points.Count];
                next.Add(Vector2.Lerp(a,b,0.25f)); next.Add(Vector2.Lerp(a,b,0.75f));
            }
            points=next;
        }
        // Collinear subdivisions need no mesh vertex; curved corners retain theirs.
        for(int i=points.Count-1;i>=0;i--)
        {
            Vector2 a=points[(i+points.Count-1)%points.Count],b=points[i],c=points[(i+1)%points.Count];
            if(Mathf.Abs(LakeCross(b-a,c-b))<0.0001f) points.RemoveAt(i);
        }
        if(LakeArea(points.ToArray())<0) points.Reverse();
        return points.ToArray();
    }

    private static void StoreLakeMesh(Mesh mesh,string path)
    {
        Mesh existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing==null) {AssetDatabase.CreateAsset(mesh,path);return;}
        // Update native mesh buffers explicitly in Unity 2018; CopySerialized can
        // leave a renderer's vertex/index buffers stale when topology changes.
        existing.Clear();existing.indexFormat=mesh.indexFormat;
        existing.vertices=mesh.vertices;existing.uv=mesh.uv;existing.triangles=mesh.triangles;
        existing.RecalculateNormals();existing.RecalculateBounds();existing.UploadMeshData(false);
        EditorUtility.SetDirty(existing);UnityEngine.Object.DestroyImmediate(mesh);
    }

    public static void CaptureLakeReview()
    {
        EditorSceneManager.OpenScene(FinalScenePath,OpenSceneMode.Single);
        Terrain terrain=UnityEngine.Object.FindObjectOfType<Terrain>();
        CaptureDetailViews(terrain,"lake-after");
        CaptureDetailCamera(terrain,new Vector2(610f,1390f),new Vector2(610f,1325f),"lake-after-overview",45f);
    }

    private static float LakeCross(Vector2 a,Vector2 b) {return a.x*b.y-a.y*b.x;}
    private static double LakeArea(Vector2[] p)
    {
        double sum=0;
        for(int i=0;i<p.Length;i++) {Vector2 a=p[i],b=p[(i+1)%p.Length]; sum+=(double)a.x*b.y-(double)b.x*a.y;}
        return sum*0.5;
    }
    private static Mesh LakePolygonMesh(Vector2[] p,float level)
    {
        List<int> remaining=new List<int>(); List<int> triangles=new List<int>();
        Vector3[] vertices=new Vector3[p.Length]; Vector2[] uv=new Vector2[p.Length];
        for(int i=0;i<p.Length;i++) {remaining.Add(i); vertices[i]=new Vector3(p[i].x,level,p[i].y); uv[i]=p[i]/12f;}
        while(remaining.Count>3)
        {
            bool found=false;
            for(int j=0;j<remaining.Count;j++)
            {
                int a=remaining[(j+remaining.Count-1)%remaining.Count],b=remaining[j],c=remaining[(j+1)%remaining.Count];
                if(LakeCross(p[b]-p[a],p[c]-p[b])<=0.000001f) continue;
                bool contains=false;
                foreach(int q in remaining)
                {
                    if(q==a || q==b || q==c) continue;
                    if(LakeCross(p[b]-p[a],p[q]-p[a])>=-0.000001f && LakeCross(p[c]-p[b],p[q]-p[b])>=-0.000001f && LakeCross(p[a]-p[c],p[q]-p[c])>=-0.000001f) {contains=true; break;}
                }
                if(contains) continue;
                triangles.Add(a);triangles.Add(c);triangles.Add(b);
                remaining.RemoveAt(j);found=true;break;
            }
            if(!found) throw new InvalidOperationException("Shore polygon cannot be triangulated");
        }
        triangles.Add(remaining[0]);triangles.Add(remaining[2]);triangles.Add(remaining[1]);
        Mesh mesh=new Mesh();mesh.name="Hole 1 smooth continuous lake";
        mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
}
