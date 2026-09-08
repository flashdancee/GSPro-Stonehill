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
    public static void SmoothHoleOneLake()
    {
        EditorSceneManager.OpenScene(FinalScenePath,OpenSceneMode.Single);
        Terrain terrain=UnityEngine.Object.FindObjectOfType<Terrain>();
        GameObject lake=GameObject.Find("Spline_Water_Holes01_02_Lake");
        if(terrain==null || lake==null) throw new InvalidOperationException("Missing lake or terrain");
        CaptureDetailViews(terrain,"lake-before");
        Mesh oldMesh=lake.GetComponent<MeshFilter>().sharedMesh;
        float level=lake.transform.TransformPoint(oldMesh.vertices[0]).y;
        Vector2[] shore=SmoothLakeBoundary(Pond);
        Mesh mesh=LakePolygonMesh(shore,level);
        StoreLakeMesh(mesh,DetailFolder+"/Smooth Lake Mesh.asset");
        Mesh saved=AssetDatabase.LoadAssetAtPath<Mesh>(DetailFolder+"/Smooth Lake Mesh.asset");
        lake.GetComponent<MeshFilter>().sharedMesh=saved;
        lake.GetComponent<MeshCollider>().sharedMesh=null;
        lake.GetComponent<MeshCollider>().sharedMesh=saved;
        int removedCollisionTriangles=TrimLakePlayingColliders(shore);
        TerrainData data=terrain.terrainData;
        int r=data.heightmapResolution;
        TerrainData baseline=AssetDatabase.LoadAssetAtPath<TerrainData>(DetailFolder+"/PreLakeTerrain.asset");
        float[,] heights=(baseline!=null ? baseline : data).GetHeights(0,0,r,r);
        int changed=0;
        float minX=10000f,maxX=-10000f,minZ=10000f,maxZ=-10000f;
        foreach(Vector2 p in shore) { minX=Mathf.Min(minX,p.x); maxX=Mathf.Max(maxX,p.x); minZ=Mathf.Min(minZ,p.y); maxZ=Mathf.Max(maxZ,p.y); }
        for(int z=0;z<r;z++) for(int x=0;x<r;x++)
        {
            Vector2 p=new Vector2(terrain.transform.position.x+x*data.size.x/(r-1),terrain.transform.position.z+z*data.size.z/(r-1));
            if(p.x<minX-4f || p.x>maxX+4f || p.y<minZ-4f || p.y>maxZ+4f) continue;
            bool inside=PointInPolygon(p,shore);
            float distance=DistanceToPolygon(p,shore);
            if(!inside && distance>4f) continue;
            // Protect the golf surfaces around the bank; no raising the water to flood them.
            if(!inside && (IsNearPlayingSurface(p,1.5f) || IsNearAnyTee(p,4f))) continue;
            float oldHeight=terrain.transform.position.y+heights[z,x]*data.size.y;
            float target;
            if(inside)
                target=Mathf.Min(oldHeight,level-0.40f-1.6f*Mathf.SmoothStep(0f,1f,Mathf.Clamp01(distance/6f)));
            else
                target=Mathf.Lerp(level-0.40f,oldHeight,Mathf.SmoothStep(0f,1f,distance/4f));
            float value=(target-terrain.transform.position.y)/data.size.y;
            if(Mathf.Abs(value-heights[z,x])>0.000001f) { heights[z,x]=value; changed++; }
        }
        data.SetHeights(0,0,heights);
        // Remove grass billboards from the basin, including cells straddling the shore.
        int dr=data.detailResolution;
        for(int layer=0;layer<data.detailPrototypes.Length;layer++)
        {
            int[,] details=(baseline!=null ? baseline : data).GetDetailLayer(0,0,dr,dr,layer);
            for(int z=0;z<dr;z++) for(int x=0;x<dr;x++)
            {
                Vector2 p=new Vector2(terrain.transform.position.x+(x+0.5f)*data.size.x/dr,terrain.transform.position.z+(z+0.5f)*data.size.z/dr);
                if(p.x<minX-4f || p.x>maxX+4f || p.y<minZ-4f || p.y>maxZ+4f) continue;
                if(PointInPolygon(p,shore) || DistanceToPolygon(p,shore)<3f) details[z,x]=0;
            }
            data.SetDetailLayer(0,0,layer,details);
        }
        terrain.Flush();
        int breakthroughs=0;
        // Dense interior samples catch terrain triangles extending across the shore.
        for(float z=minZ;z<maxZ;z+=0.5f) for(float x=minX;x<maxX;x+=0.5f)
        {
            Vector2 p=new Vector2(x,z);
            if(PointInPolygon(p,shore) && DistanceToPolygon(p,shore)>0.8f && TerrainHeight(terrain,p)>=level-0.015f) breakthroughs++;
        }
        if(breakthroughs!=0) throw new InvalidOperationException("Terrain still protrudes through lake: "+breakthroughs);
        foreach(Vector3 v in saved.vertices) if(Mathf.Abs(v.y-level)>0.00001f) throw new InvalidOperationException("Lake is not planar");
        double polygonArea=Math.Abs(LakeArea(shore));
        double trianglesArea=0;
        Vector3[] vertices=saved.vertices; int[] triangles=saved.triangles;
        for(int i=0;i<triangles.Length;i+=3) trianglesArea+=Vector3.Cross(vertices[triangles[i+1]]-vertices[triangles[i]],vertices[triangles[i+2]]-vertices[triangles[i]]).magnitude*0.5;
        if(Math.Abs(polygonArea-trianglesArea)>0.05) throw new InvalidOperationException("Lake triangles do not cover the shoreline");
        EditorUtility.SetDirty(data);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        if(!EditorSceneManager.SaveScene(SceneManager.GetActiveScene())) throw new IOException("Scene save failed");
        CaptureDetailViews(terrain,"lake-after");
        CaptureDetailCamera(terrain,new Vector2(700f,1370f),new Vector2(623f,1328f),"lake-after-shore",3f);
        CaptureDetailCamera(terrain,new Vector2(610f,1390f),new Vector2(610f,1325f),"lake-after-overview",45f);
        File.WriteAllText(Path.Combine(ReviewFolder,"lake-validation.txt"),"PASS: planar water, complete triangle coverage, zero terrain breakthroughs at 0.5m sampling (>0.8m from bank).\nWater level: "+level+"\nShore vertices: "+shore.Length+"\nArea m2: "+polygonArea+"\nChanged height samples (local basin and <=4m bank only): "+changed+"\nUTC: "+DateTime.UtcNow.ToString("o"));
        Debug.Log("HOLE_ONE_LAKE_COMPLETE level="+level+" vertices="+shore.Length+" terrainSamples="+changed+" removedDryTriangles="+removedCollisionTriangles);
    }

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

    private static int TrimLakePlayingColliders(Vector2[] shore)
    {
        int removed=0;
        foreach(MeshCollider collider in UnityEngine.Object.FindObjectsOfType<MeshCollider>())
        {
            if(!collider.name.StartsWith("Spline_Fairway_")) continue;
            Mesh original=AssetDatabase.LoadAssetAtPath<Mesh>(GeneratedFolder+"/"+collider.name+".asset");
            if(original==null) original=collider.sharedMesh;
            Vector3[] vertices=original.vertices;
            int[] indices=original.triangles;
            List<int> keep=new List<int>(); int localRemoved=0;
            for(int i=0;i<indices.Length;i+=3)
            {
                Vector3 c=collider.transform.TransformPoint((vertices[indices[i]]+vertices[indices[i+1]]+vertices[indices[i+2]])/3f);
                if(PointInPolygon(new Vector2(c.x,c.z),shore)) {localRemoved++; continue;}
                keep.Add(indices[i]);keep.Add(indices[i+1]);keep.Add(indices[i+2]);
            }
            if(localRemoved==0) continue;
            Mesh mesh=UnityEngine.Object.Instantiate(original);mesh.triangles=keep.ToArray();mesh.RecalculateBounds();
            string path=DetailFolder+"/Lake trimmed "+collider.name+".asset";
            StoreLakeMesh(mesh,path);
            Mesh saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            collider.sharedMesh=saved;
            MeshFilter filter=collider.GetComponent<MeshFilter>(); if(filter!=null) filter.sharedMesh=saved;
            removed+=localRemoved;
        }
        return removed;
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
