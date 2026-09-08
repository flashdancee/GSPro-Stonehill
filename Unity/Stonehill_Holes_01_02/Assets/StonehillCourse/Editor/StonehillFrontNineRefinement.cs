using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static partial class StonehillTwoHoleCourseBuilder
{
    private const string RefinementFolder="Assets/StonehillCourse/Refinement";
    private const string SettingsPath=RefinementFolder+"/RefinementSettings.json";
    [Serializable] private class HoleRefinement
    {
        public int hole; public bool enabled; public int seed;
        public float treeSpacing=7.5f,treeHeightMin=10f,treeHeightMax=21f,woodlandRadius=105f,cameraEye=1.7f;
    }
    [Serializable] private class RefinementSettings { public HoleRefinement[] holes; }
    [Serializable] private class WaterExport { public WaterRecord[] waters; }
    [Serializable] private class WaterRecord { public string name; public Vector3[] coords; }
    private static readonly string[] WaterNames={"Holes01_02_Lake","Hole04_Lake","Hole05_Pond","Hole06_UpperPond","Hole06_LowerPond","Hole07_Pond","Hole09_Pond"};
    private static readonly int[] WaterOwners={1,4,5,6,6,7,9};
    private static string RefinementReview { get { return Path.GetFullPath(Path.Combine(Application.dataPath,"../../../Reviews")); } }
    private static RefinementSettings ReadRefinementSettings()
    {
        RefinementSettings s=JsonUtility.FromJson<RefinementSettings>(File.ReadAllText(SettingsPath));
        if(s==null || s.holes==null || s.holes.Length!=9) throw new InvalidDataException("Nine hole settings required");
        for(int i=0;i<9;i++) if(s.holes[i].hole!=i+1 || s.holes[i].treeSpacing<5f) throw new InvalidDataException("Invalid hole settings");
        if(!s.holes[0].enabled) throw new InvalidDataException("Hole 1 reference must remain enabled");
        return s;
    }
    [MenuItem("Stonehill/Build Front-Nine Playable Course")]
    public static void BuildPlayableCourse() { RebuildRefinedFrontNine(); }
    [MenuItem("Stonehill/Refinement/Rebuild Enabled Front Nine")]
    public static void RebuildRefinedFrontNine()
    {
        EditorSceneManager.OpenScene(FinalScenePath,OpenSceneMode.Single);
        EnsureAssetFolder(RefinementFolder);
        BuildPlayableBase();
        ApplyHoleOneDetailLegacy();
        Terrain terrain=UnityEngine.Object.FindObjectOfType<Terrain>();
        SaveDetailAsset(UnityEngine.Object.Instantiate(terrain.terrainData),RefinementFolder+"/PreWaterTerrain.asset");
        // The original Hole 1 baseline is refreshed only following a source rebuild.
        SaveDetailAsset(UnityEngine.Object.Instantiate(terrain.terrainData),DetailFolder+"/PreLakeTerrain.asset");
        ApplyEnabledRefinements(terrain,ReadRefinementSettings());
        CaptureEnabledHoles();
    }
    [MenuItem("Stonehill/Hole 1/Apply Detail and Capture")]
    public static void ApplyHoleOneDetail() { RefineHole(1); }
    [MenuItem("Stonehill/Refinement/Hole 2")] public static void RefineHole02(){RefineHole(2);}
    [MenuItem("Stonehill/Refinement/Hole 3")] public static void RefineHole03(){RefineHole(3);}
    [MenuItem("Stonehill/Refinement/Hole 4")] public static void RefineHole04(){RefineHole(4);}
    [MenuItem("Stonehill/Refinement/Hole 5")] public static void RefineHole05(){RefineHole(5);}
    [MenuItem("Stonehill/Refinement/Hole 6")] public static void RefineHole06(){RefineHole(6);}
    [MenuItem("Stonehill/Refinement/Hole 7")] public static void RefineHole07(){RefineHole(7);}
    [MenuItem("Stonehill/Refinement/Hole 8")] public static void RefineHole08(){RefineHole(8);}
    [MenuItem("Stonehill/Refinement/Hole 9")] public static void RefineHole09(){RefineHole(9);}
    public static void RefineHole(int hole)
    {
        if(hole<1 || hole>9) throw new ArgumentOutOfRangeException("hole");
        EditorSceneManager.OpenScene(FinalScenePath,OpenSceneMode.Single);
        EnsureAssetFolder(RefinementFolder);
        RefinementSettings settings=ReadRefinementSettings();
        Terrain terrain=UnityEngine.Object.FindObjectOfType<Terrain>();
        if(terrain==null) throw new InvalidOperationException("Course terrain missing");
        CaptureRefinedHole(terrain,settings.holes[hole-1],"before");
        settings.holes[hole-1].enabled=true;
        if(AssetDatabase.LoadAssetAtPath<TerrainData>(RefinementFolder+"/PreWaterTerrain.asset")==null)
        {
            TerrainData prior=AssetDatabase.LoadAssetAtPath<TerrainData>(DetailFolder+"/PreLakeTerrain.asset");
            if(prior==null) throw new InvalidOperationException("Missing baseline; run full rebuild");
            SaveDetailAsset(UnityEngine.Object.Instantiate(prior),RefinementFolder+"/PreWaterTerrain.asset");
        }
        ApplyEnabledRefinements(terrain,settings);
        File.WriteAllText(SettingsPath,JsonUtility.ToJson(settings,true)+"\n");
        AssetDatabase.ImportAsset(SettingsPath);
        CaptureRefinedHole(terrain,settings.holes[hole-1],"after");
        // Shared features can affect Hole 1 from the Hole 2 side.
        if(hole!=1) CaptureRefinedHole(terrain,settings.holes[0],"regression");
        Debug.Log("REFINEMENT_HOLE_COMPLETE "+hole);
    }
    private static void ApplyEnabledRefinements(Terrain terrain,RefinementSettings settings)
    {
        Directory.CreateDirectory(RefinementReview);
        var report=new StringBuilder();
        report.AppendLine("Unity technical validation; GSPro shot testing pending.");
        RefineSharedWaters(terrain,settings,report);
        string physics=RefinementPhysicsHash();
        string heights=HeightHash(terrain);
        ApplyCourseTurf(terrain,settings);
        foreach(HoleRefinement h in settings.holes) if(h.enabled && h.hole>1)
            report.AppendLine("Hole "+h.hole+" mature trees: "+PlantRefinementTrees(terrain,h,settings));
        ConfigureRefinedLighting(terrain);
        if(heights!=HeightHash(terrain) || physics!=RefinementPhysicsHash()) throw new InvalidOperationException("Visual pass changed elevations or collider content");
        ValidateMarkers();
        report.AppendLine("PASS: visual pass preserves all elevations and mesh collider content.");
        report.AppendLine("PASS: nine holes, eighteen tees and thirty-six pin markers.");
        report.AppendLine("Height SHA256: "+heights);
        report.AppendLine("Physics SHA256: "+physics);
        report.AppendLine("Managed memory bytes: "+GC.GetTotalMemory(false));
        report.AppendLine("UTC: "+DateTime.UtcNow.ToString("o"));
        EditorUtility.SetDirty(terrain.terrainData);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        if(!EditorSceneManager.SaveScene(SceneManager.GetActiveScene())) throw new IOException("Scene save failed");
        File.WriteAllText(Path.Combine(RefinementReview,"latest-validation.txt"),report.ToString());
        Debug.Log(report.ToString());
    }
    private static void ValidateMarkers()
    {
        for(int h=1;h<=9;h++)
        {
            string n=h.ToString("00");
            foreach(string surface in new[]{"Fairway_Hole","Green_Hole"})
                if(GameObject.Find("Spline_"+surface+n)==null) throw new InvalidOperationException("Missing surface "+n);
            foreach(string tee in new[]{"White","Red"}) if(GameObject.Find("Spline_Tee_Hole"+n+"_"+tee)==null) throw new InvalidOperationException("Missing tee "+n);
            foreach(string name in new[]{"Spline_Green_Hole"+n,"Spline_Tee_Hole"+n+"_White","Spline_Tee_Hole"+n+"_Red"})
            {Mesh source=AssetDatabase.LoadAssetAtPath<Mesh>(GeneratedFolder+"/"+name+".asset");if(GameObject.Find(name).GetComponent<MeshCollider>().sharedMesh!=source)throw new InvalidOperationException("Protected playing collider changed: "+name);}
        }
        int pins=0;
        foreach(Transform t in UnityEngine.Object.FindObjectsOfType<Transform>()) if(t.name.StartsWith("GK_H") && t.name.Contains("_Pin_")) pins++;
        if(pins!=36) throw new InvalidOperationException("Expected 36 pin markers, found "+pins);
    }
    private static float RouteDistance(Vector2 p,int hole)
    {
        Vector2[] r=FrontNineRoutes()[hole-1]; float d=float.MaxValue;
        for(int i=1;i<r.Length;i++) d=Mathf.Min(d,DistanceToSegment(p,r[i-1],r[i]));
        return d;
    }
    private static void ApplyCourseTurf(Terrain terrain,RefinementSettings settings)
    {
        // Recompute the union from source polygons, so neighbouring masks never overwrite one another.
        const int width=2048,height=1280;
        const float left=240,bottom=880,wide=900,tall=600;
        Texture2D mask=new Texture2D(width,height,TextureFormat.RGBA32,false,true);
        mask.wrapMode=TextureWrapMode.Clamp; mask.filterMode=FilterMode.Bilinear;
        Color[] pixels=new Color[width*height];
        Vector2[][] fairways=FrontNineFairways(),greens=FrontNineGreens();
        foreach(HoleRefinement h in settings.holes) if(h.enabled)
        {
            int index=h.hole-1; List<Vector2[]> shapes=new List<Vector2[]>();
            shapes.Add(fairways[index]); shapes.Add(greens[index]);
            foreach(string tee in new[]{"White","Red"})
            {
                Vector3 c=GameObject.Find("Spline_Tee_Hole"+h.hole.ToString("00")+"_"+tee).GetComponent<MeshRenderer>().bounds.center;
                Vector2 p=new Vector2(c.x,c.z); Vector2[] route=FrontNineRoutes()[index];
                shapes.Add(TeeRectangle(p,route[route.Length-2]-p));
            }
            for(int k=0;k<shapes.Count;k++)
            {
                Bounds b=PolygonBounds(shapes[k]);
                int minX=Mathf.Clamp(Mathf.FloorToInt((b.min.x-2-left)*width/wide),0,width-1),maxX=Mathf.Clamp(Mathf.CeilToInt((b.max.x+2-left)*width/wide),0,width-1);
                int minZ=Mathf.Clamp(Mathf.FloorToInt((b.min.z-2-bottom)*height/tall),0,height-1),maxZ=Mathf.Clamp(Mathf.CeilToInt((b.max.z+2-bottom)*height/tall),0,height-1);
                for(int z=minZ;z<=maxZ;z++) for(int x=minX;x<=maxX;x++)
                {
                    Vector2 p=new Vector2(left+(x+0.5f)*wide/width,bottom+(z+0.5f)*tall/height);
                    float v=ShapeMask(p,shapes[k]); Color c=pixels[z*width+x];
                    if(k==0)c.r=Mathf.Max(c.r,v); else if(k==1)c.g=Mathf.Max(c.g,v); else c.b=Mathf.Max(c.b,v);
                    pixels[z*width+x]=c;
                }
            }
        }
        for(int i=0;i<pixels.Length;i++){Color c=pixels[i];c.r*=1-c.g;c.b*=1-c.g;pixels[i]=c;}
        mask.SetPixels(pixels);mask.Apply();SaveDetailAsset(mask,RefinementFolder+"/FrontNineTurfMask.asset");
        Material mat=new Material(Shader.Find("Stonehill/Detailed Terrain"));
        mat.SetTexture("_HoleMask",AssetDatabase.LoadAssetAtPath<Texture2D>(RefinementFolder+"/FrontNineTurfMask.asset"));
        mat.SetTexture("_FineGrass",LoadTexture("Assets/GroundTextures/seamlessgrass.jpg"));
        mat.SetTexture("_FineNormal",LoadTexture(GrassNormalPath));mat.SetVector("_HoleRect",new Vector4(left,bottom,wide,tall));
        SaveDetailAsset(mat,RefinementFolder+"/FrontNineTerrain.mat");
        terrain.materialType=Terrain.MaterialType.Custom;terrain.materialTemplate=AssetDatabase.LoadAssetAtPath<Material>(RefinementFolder+"/FrontNineTerrain.mat");
        // Fine rough/collar tiling is shared; all source paint weights remain intact.
        SplatPrototype[] layers=terrain.terrainData.splatPrototypes;
        layers[0]=NewTerrainLayer(LoadTexture("Assets/GroundTextures/seamlessgrass.jpg"),null,1.4f);
        layers[7]=NewTerrainLayer(LoadTexture("Assets/GroundTextures/seamlessgrass.jpg"),null,0.9f);
        terrain.terrainData.splatPrototypes=layers;
    }
    private static int PlantRefinementTrees(Terrain terrain,HoleRefinement h,RefinementSettings settings)
    {
        string name="Hole "+h.hole.ToString("00")+" - Refined woodland";
        GameObject prior=GameObject.Find(name);if(prior!=null)UnityEngine.Object.DestroyImmediate(prior);
        GameObject root=new GameObject(name);
        var random=new System.Random(h.seed); var placed=new List<Vector2>();
        var prefabs=new[]{LoadPrefab(BirchPrefabPath),LoadPrefab(MaplePrefabPath),LoadPrefab(BroadleafPrefabPath),LoadPrefab(ConiferPrefabPath)};
        for(float z=895;z<1460;z+=h.treeSpacing) for(float x=250;x<1125;x+=h.treeSpacing)
        {
            Vector2 p=new Vector2(x+((float)random.NextDouble()-.5f)*5,z+((float)random.NextDouble()-.5f)*5);
            if(RouteDistance(p,h.hole)>h.woodlandRadius || !PointInAnyPolygon(p,WoodlandZones) || IsNearPlayingSurface(p,12) || IsNearAnyTee(p,16) || IsNearWater(p,6) || TerrainSlope(terrain,p)>40)continue;
            // Stable ownership independent of which holes are enabled; no duplicate shared belts.
            int owner=2;float nearest=RouteDistance(p,2);
            for(int j=3;j<=9;j++){float d=RouteDistance(p,j);if(d<nearest){nearest=d;owner=j;}}
            if(owner!=h.hole || HoleOneDistance(p)<70)continue;
            if(PointInAnyPolygon(p,RockCutZones) && random.NextDouble()<.75)continue;
            bool close=false;foreach(Vector2 q in placed)if(Vector2.Distance(p,q)<h.treeSpacing*.75f){close=true;break;}if(close)continue;
            int choice=random.Next(100),species=choice<42?0:choice<69?1:choice<83?2:3;
            GameObject tree=PrefabUtility.InstantiatePrefab(prefabs[species]) as GameObject;tree.name="H"+h.hole+" mature "+placed.Count.ToString("000");tree.transform.SetParent(root.transform,false);
            Renderer[] renderers=tree.GetComponentsInChildren<Renderer>();Bounds b=new Bounds();bool first=true;
            foreach(Renderer r in renderers){if(first){b=r.bounds;first=false;}else b.Encapsulate(r.bounds);}
            tree.transform.localScale*=Mathf.Lerp(h.treeHeightMin,h.treeHeightMax,(float)random.NextDouble())/Mathf.Max(b.size.y,.5f);
            tree.transform.rotation=Quaternion.Euler(0,(float)random.NextDouble()*360,0);tree.transform.position=new Vector3(p.x,TerrainHeight(terrain,p)-.12f,p.y);
            foreach(Renderer r in renderers){r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;}
            foreach(LODGroup lod in tree.GetComponentsInChildren<LODGroup>())lod.RecalculateBounds();SetStaticRecursively(tree);placed.Add(p);
        }
        return placed.Count;
    }
    private static Bounds PolygonBounds(Vector2[] p)
    {
        Bounds b=new Bounds(new Vector3(p[0].x,0,p[0].y),Vector3.zero);
        foreach(Vector2 q in p)b.Encapsulate(new Vector3(q.x,0,q.y));return b;
    }
    private static void RefineSharedWaters(Terrain terrain,RefinementSettings settings,StringBuilder report)
    {
        TerrainData data=terrain.terrainData,baseline=AssetDatabase.LoadAssetAtPath<TerrainData>(RefinementFolder+"/PreWaterTerrain.asset");
        if(baseline==null || baseline.heightmapResolution!=data.heightmapResolution)throw new InvalidOperationException("Missing compatible pre-water terrain");
        int r=data.heightmapResolution;float[,] current=data.GetHeights(0,0,r,r),original=baseline.GetHeights(0,0,r,r),before=(float[,])current.Clone();
        var shores=new List<Vector2[]>();var export=new List<WaterRecord>();var enabledIndices=new List<int>();
        Vector2[][] source=FrontNineWaters();
        Vector2[][] previousShore=new Vector2[7][];for(int w=0;w<7;w++)previousShore[w]=SmoothLakeBoundary(source[w]);
        // Restore only the previously owned source-water envelopes before replaying the union.
        // This removes obsolete shaping when a protected shoreline is revised, without resetting the course.
        for(int w=0;w<7;w++)if(settings.holes[WaterOwners[w]-1].enabled)
        {Bounds b=PolygonBounds(source[w]);for(int z=Mathf.Max(0,(int)b.min.z-5);z<=Mathf.Min(r-1,(int)b.max.z+5);z++)for(int x=Mathf.Max(0,(int)b.min.x-5);x<=Mathf.Min(r-1,(int)b.max.x+5);x++)
        {Vector2 p=new Vector2(x,z);if(PointInPolygon(p,source[w])||DistanceToPolygon(p,source[w])<=4||PointInPolygon(p,previousShore[w])||DistanceToPolygon(p,previousShore[w])<=4)current[z,x]=original[z,x];}}
        for(int w=0;w<7;w++)
        {
            GameObject water=GameObject.Find("Spline_Water_"+WaterNames[w]);
            if(water==null)throw new InvalidOperationException("Missing water "+WaterNames[w]);
            Mesh mesh=water.GetComponent<MeshFilter>().sharedMesh;
            float level=water.transform.TransformPoint(mesh.vertices[0]).y;
            Vector2[] shore=settings.holes[WaterOwners[w]-1].enabled?ProtectedWaterBoundary(source[w],terrain,report,WaterNames[w]):source[w];
            var record=new WaterRecord{name=WaterNames[w],coords=new Vector3[shore.Length]};
            for(int i=0;i<shore.Length;i++)record.coords[i]=new Vector3(shore[i].x,level,shore[i].y);export.Add(record);
            if(!settings.holes[WaterOwners[w]-1].enabled)continue;
            shores.Add(shore);enabledIndices.Add(w);
            Mesh smooth=LakePolygonMesh(shore,level);smooth.name=WaterNames[w]+" smooth shoreline";
            // Source polygons are world coordinates; mesh storage uses the object's local space.
            Vector3[] local=smooth.vertices;for(int i=0;i<local.Length;i++)local[i]=water.transform.InverseTransformPoint(local[i]);smooth.vertices=local;smooth.RecalculateNormals();smooth.RecalculateBounds();
            string path=RefinementFolder+"/Water-"+WaterNames[w]+".asset";StoreLakeMesh(smooth,path);Mesh saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            water.GetComponent<MeshFilter>().sharedMesh=saved;water.GetComponent<MeshCollider>().sharedMesh=null;water.GetComponent<MeshCollider>().sharedMesh=saved;
            Material lake=AssetDatabase.LoadAssetAtPath<Material>(DetailFolder+"/Hole 1 Lake.mat");if(lake!=null)water.GetComponent<MeshRenderer>().sharedMaterial=lake;
            Bounds b=PolygonBounds(shore);
            for(int z=Mathf.Max(0,Mathf.FloorToInt(b.min.z-5));z<=Mathf.Min(r-1,Mathf.CeilToInt(b.max.z+5));z++)for(int x=Mathf.Max(0,Mathf.FloorToInt(b.min.x-5));x<=Mathf.Min(r-1,Mathf.CeilToInt(b.max.x+5));x++)
            {
                Vector2 p=new Vector2(terrain.transform.position.x+x*data.size.x/(r-1),terrain.transform.position.z+z*data.size.z/(r-1));
                bool inside=PointInPolygon(p,shore);float d=DistanceToPolygon(p,shore);
                if(IsInsideOrNearAnyPolygon(p,FrontNineGreens(),1.5f) || IsNearAnyTee(p,4) || (!inside && d>4))continue;
                float old=terrain.transform.position.y+original[z,x]*data.size.y;
                // Meet the water at the bank instead of creating a submerged rim/floating edge.
                float target=inside?Mathf.Min(old,level-.03f-1.97f*Mathf.SmoothStep(0,1,Mathf.Clamp01(d/6))):Mathf.Lerp(level+.03f,old,Mathf.SmoothStep(0,1,d/4));
                current[z,x]=(target-terrain.transform.position.y)/data.size.y;
            }
            double area=0;Vector3[] v=saved.vertices;int[] t=saved.triangles;
            foreach(Vector3 p in v)if(Mathf.Abs(water.transform.TransformPoint(p).y-level)>.0001f)throw new InvalidOperationException("Nonplanar water");
            for(int i=0;i<t.Length;i+=3)area+=Vector3.Cross(water.transform.TransformPoint(v[t[i+1]])-water.transform.TransformPoint(v[t[i]]),water.transform.TransformPoint(v[t[i+2]])-water.transform.TransformPoint(v[t[i]])).magnitude*.5;
            if(Math.Abs(area-Math.Abs(LakeArea(shore)))>.1)throw new InvalidOperationException("Water area mismatch "+WaterNames[w]);
        }
        data.SetHeights(0,0,current);terrain.Flush();
        int changed=0;
        for(int z=0;z<r;z++)for(int x=0;x<r;x++)if(before[z,x]!=current[z,x])
        {
            Vector2 p=new Vector2(x,z);bool allowed=false;
            foreach(int index in enabledIndices)if(PointInPolygon(p,source[index])||DistanceToPolygon(p,source[index])<=4.001f||PointInPolygon(p,previousShore[index])||DistanceToPolygon(p,previousShore[index])<=4.001f){allowed=true;break;}
            if(!allowed)foreach(Vector2[] shore in shores)if(PointInPolygon(p,shore)||DistanceToPolygon(p,shore)<=4.001f){allowed=true;break;}
            if(!allowed)throw new InvalidOperationException("Water pass changed distant terrain");changed++;
        }
        for(int i=0;i<shores.Count;i++)
        {
            Vector2[] shore=shores[i];Bounds b=PolygonBounds(shore);float level=export[enabledIndices[i]].coords[0].y;
            for(float z=b.min.z;z<b.max.z;z+=.5f)for(float x=b.min.x;x<b.max.x;x+=.5f)
            {Vector2 p=new Vector2(x,z);if(PointInPolygon(p,shore)&&DistanceToPolygon(p,shore)>.8f&&TerrainHeight(terrain,p)>=level-.015f)throw new InvalidOperationException("Terrain breakthrough "+WaterNames[enabledIndices[i]]);}
        }
        int dr=data.detailResolution;
        var shoreBounds=new List<Bounds>();foreach(Vector2[] s in shores){Bounds b=PolygonBounds(s);b.Expand(new Vector3(8,0,8));shoreBounds.Add(b);}
        for(int layer=0;layer<data.detailPrototypes.Length;layer++)
        {
            int[,] details=data.GetDetailLayer(0,0,dr,dr,layer);
            for(int z=0;z<dr;z++)for(int x=0;x<dr;x++)
            {Vector2 p=new Vector2((x+.5f)*data.size.x/dr,(z+.5f)*data.size.z/dr);for(int i=0;i<shores.Count;i++){Bounds b=shoreBounds[i];if(p.x<b.min.x||p.x>b.max.x||p.y<b.min.z||p.y>b.max.z)continue;if(PointInPolygon(p,shores[i])||DistanceToPolygon(p,shores[i])<3){details[z,x]=0;break;}}}
            data.SetDetailLayer(0,0,layer,details);
        }
        ClipDrySurfaces(terrain,shores,report);
        File.WriteAllText(Path.Combine(RefinementReview,"water-boundaries.json"),JsonUtility.ToJson(new WaterExport{waters=export.ToArray()},true));
        report.AppendLine("PASS: "+shores.Count+" refined waters planar, triangulated and clear of terrain on 0.5m interior samples; "+changed+" height samples changed only within basin/bank bounds.");
    }
    private struct ClipVertex
    {
        public Vector3 p; public Vector2 uv;
        public ClipVertex(Vector3 position,Vector2 texture){p=position;uv=texture;}
    }
    private static Vector2[] ProtectedWaterBoundary(Vector2[] source,Terrain terrain,StringBuilder report,string name)
    {
        var protectedShapes=new List<Vector2[]>(FrontNineGreens());
        for(int h=1;h<=9;h++)foreach(string tee in new[]{"White","Red"})
        {Vector3 c=GameObject.Find("Spline_Tee_Hole"+h.ToString("00")+"_"+tee).GetComponent<MeshRenderer>().bounds.center;Vector2 p=new Vector2(c.x,c.z);Vector2[] route=FrontNineRoutes()[h-1];protectedShapes.Add(TeeRectangle(p,route[route.Length-2]-p));}
        Vector2[] result=source;
        foreach(Vector2[] shape in protectedShapes)
        {
            bool overlap=false;foreach(Vector2 p in shape)if(PointInPolygon(p,result)||DistanceToPolygon(p,result)<2){overlap=true;break;}
            if(!overlap)foreach(Vector2 p in result)if(PointInPolygon(p,shape)||DistanceToPolygon(p,shape)<2){overlap=true;break;}
            if(!overlap)continue;
            Vector2 center=PolygonCenter(result),protectedCenter=PolygonCenter(shape),direction=(protectedCenter-center).normalized;
            if(direction.sqrMagnitude<.1f)throw new InvalidOperationException("Water encloses protected playing area: "+name);
            float radius=0;foreach(Vector2 p in shape)radius=Mathf.Max(radius,Vector2.Distance(p,protectedCenter));
            Vector2 plane=protectedCenter-direction*(radius+2),tangent=new Vector2(-direction.y,direction.x);
            var polygon=new List<ClipVertex>();foreach(Vector2 p in result)polygon.Add(new ClipVertex(new Vector3(p.x,0,p.y),Vector2.zero));
            Vector2 a=plane-tangent*1000,b=plane+tangent*1000;
            bool keepPositive=LakeCross(b-a,center-a)>=0;
            polygon=ClipHalf(polygon,a,b,keepPositive);
            if(polygon.Count<3)throw new InvalidOperationException("No safe water remains: "+name);
            result=new Vector2[polygon.Count];for(int i=0;i<result.Length;i++)result[i]=new Vector2(polygon[i].p.x,polygon[i].p.z);
            report.AppendLine("Adjusted "+name+" shoreline to preserve an existing tee/green envelope; photo review required.");
        }
        return SmoothLakeBoundary(result);
    }
    private static List<ClipVertex> ClipHalf(List<ClipVertex> polygon,Vector2 a,Vector2 b,bool inside)
    {
        var result=new List<ClipVertex>();if(polygon.Count==0)return result;
        ClipVertex previous=polygon[polygon.Count-1];float pd=LakeCross(b-a,new Vector2(previous.p.x,previous.p.z)-a);bool pk=inside?pd>=0:pd<=0;
        foreach(ClipVertex next in polygon)
        {
            float nd=LakeCross(b-a,new Vector2(next.p.x,next.p.z)-a);bool nk=inside?nd>=0:nd<=0;
            if(nk!=pk){float f=pd/(pd-nd);result.Add(new ClipVertex(Vector3.Lerp(previous.p,next.p,f),Vector2.Lerp(previous.uv,next.uv,f)));}
            if(nk)result.Add(next);previous=next;pd=nd;pk=nk;
        }
        return result;
    }
    private static void ClipDrySurfaces(Terrain terrain,List<Vector2[]> shores,StringBuilder report)
    {
        var cuts=new List<Vector2[]>();var bounds=new List<Bounds>();
        var shorelineBounds=new List<Bounds>();foreach(Vector2[] s in shores){Bounds b=PolygonBounds(s);b.Expand(new Vector3(8,0,8));shorelineBounds.Add(b);}
        foreach(Vector2[] s in shores)
        {
            Mesh m=LakePolygonMesh(s,0);int[] ti=m.triangles;
            for(int i=0;i<ti.Length;i+=3){Vector2[] tri={s[ti[i]],s[ti[i+2]],s[ti[i+1]]};cuts.Add(tri);bounds.Add(PolygonBounds(tri));}
            UnityEngine.Object.DestroyImmediate(m);
        }
        int clipped=0;
        foreach(MeshCollider c in UnityEngine.Object.FindObjectsOfType<MeshCollider>())
        {
            if(!c.name.StartsWith("Spline_")||c.name.StartsWith("Spline_Water_"))continue;
            Mesh source=AssetDatabase.LoadAssetAtPath<Mesh>(GeneratedFolder+"/"+c.name+".asset");if(source==null)throw new InvalidOperationException("Missing source collider "+c.name);
            if(c.name.StartsWith("Spline_Tee_")||c.name.StartsWith("Spline_Green_")){c.sharedMesh=source;c.GetComponent<MeshFilter>().sharedMesh=source;continue;}
            Bounds surfaceBounds=c.GetComponent<MeshRenderer>().bounds;bool near=false;
            foreach(Bounds b in shorelineBounds)if(surfaceBounds.max.x>=b.min.x&&surfaceBounds.min.x<=b.max.x&&surfaceBounds.max.z>=b.min.z&&surfaceBounds.min.z<=b.max.z){near=true;break;}
            if(!near){c.sharedMesh=source;c.GetComponent<MeshFilter>().sharedMesh=source;continue;}
            Vector3[] sv=source.vertices;Vector2[] uv=source.uv;int[] st=source.triangles;
            var vertices=new List<Vector3>();var texture=new List<Vector2>();var triangles=new List<int>();bool affected=false;
            for(int i=0;i<st.Length;i+=3)
            {
                var polygon=new List<ClipVertex>();for(int k=0;k<3;k++)
                {
                    int n=st[i+k];Vector3 world=c.transform.TransformPoint(sv[n]);Vector2 p=new Vector2(world.x,world.z);
                    // The immediate fairway bank follows its reshaped terrain; do not leave a floating dry collider.
                    if(c.name.StartsWith("Spline_Fairway_"))for(int j=0;j<shores.Count;j++)
                    {Bounds b=shorelineBounds[j];if(p.x<b.min.x||p.x>b.max.x||p.y<b.min.z||p.y>b.max.z)continue;if(DistanceToPolygon(p,shores[j])<=4 && !IsInsideOrNearAnyPolygon(p,FrontNineGreens(),1.5f) && !IsNearAnyTee(p,4)){world.y=TerrainHeight(terrain,p)+.01f;affected=true;break;}}
                    polygon.Add(new ClipVertex(world,uv.Length==sv.Length?uv[n]:Vector2.zero));
                }
                Bounds box=new Bounds(polygon[0].p,Vector3.zero);foreach(ClipVertex v in polygon)box.Encapsulate(v.p);
                var pieces=new List<List<ClipVertex>>{polygon};
                for(int j=0;j<cuts.Count;j++)
                {
                    Bounds b=bounds[j];if(box.max.x<b.min.x || box.min.x>b.max.x || box.max.z<b.min.z || box.min.z>b.max.z)continue;
                    var remaining=new List<List<ClipVertex>>();Vector2[] cut=cuts[j];
                    foreach(List<ClipVertex> piece in pieces)
                    {
                        var inner=piece;for(int edge=0;edge<3 && inner.Count>=3;edge++)
                        {var outer=ClipHalf(inner,cut[edge],cut[(edge+1)%3],false);if(outer.Count>=3)remaining.Add(outer);inner=ClipHalf(inner,cut[edge],cut[(edge+1)%3],true);}
                        if(inner.Count>=3)affected=true;
                    }
                    pieces=remaining;if(pieces.Count==0)break;
                }
                foreach(List<ClipVertex> piece in pieces)for(int k=1;k<piece.Count-1;k++)
                {
                    ClipVertex a=piece[0],b=piece[k],d=piece[k+1];if(Vector3.Cross(b.p-a.p,d.p-a.p).sqrMagnitude<1e-12f)continue;
                    foreach(ClipVertex v in new[]{a,b,d}){triangles.Add(vertices.Count);vertices.Add(c.transform.InverseTransformPoint(v.p));texture.Add(v.uv);}
                }
            }
            if(!affected){c.sharedMesh=source;continue;}
            Mesh result=new Mesh();result.name=c.name+" shoreline clipped";result.indexFormat=IndexFormat.UInt32;result.SetVertices(vertices);result.SetUVs(0,texture);result.SetTriangles(triangles,0);result.RecalculateNormals();result.RecalculateBounds();
            string path=RefinementFolder+"/Clipped-"+c.name+".asset";StoreLakeMesh(result,path);c.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);c.GetComponent<MeshFilter>().sharedMesh=c.sharedMesh;clipped++;
        }
        report.AppendLine("Shoreline polygon subtraction applied to "+clipped+" dry surface colliders; source meshes retained.");
    }
    private static string HashBytes(byte[] bytes){using(SHA256 hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","");}
    private static string HeightHash(Terrain terrain){float[,] h=terrain.terrainData.GetHeights(0,0,terrain.terrainData.heightmapResolution,terrain.terrainData.heightmapResolution);byte[] b=new byte[h.Length*4];Buffer.BlockCopy(h,0,b,0,b.Length);return HashBytes(b);}
    private static string RefinementPhysicsHash()
    {
        var colliders=new List<MeshCollider>();foreach(MeshCollider c in UnityEngine.Object.FindObjectsOfType<MeshCollider>())if(c.name.StartsWith("Spline_"))colliders.Add(c);
        colliders.Sort((a,b)=>string.CompareOrdinal(a.name,b.name));
        using(var stream=new MemoryStream())using(var writer=new BinaryWriter(stream))
        {foreach(MeshCollider c in colliders){writer.Write(c.name);writer.Write(AssetDatabase.GetAssetPath(c.sharedMaterial));Matrix4x4 matrix=c.transform.localToWorldMatrix;for(int j=0;j<16;j++)writer.Write(matrix[j]);foreach(Vector3 v in c.sharedMesh.vertices){writer.Write(v.x);writer.Write(v.y);writer.Write(v.z);}foreach(int i in c.sharedMesh.triangles)writer.Write(i);}writer.Flush();return HashBytes(stream.ToArray());}
    }
    [MenuItem("Stonehill/Refinement/Capture Enabled Holes")]
    public static void CaptureEnabledHoles(){Terrain t=UnityEngine.Object.FindObjectOfType<Terrain>();foreach(HoleRefinement h in ReadRefinementSettings().holes)if(h.enabled)CaptureRefinedHole(t,h,"after");}
    private static void CaptureRefinedHole(Terrain terrain,HoleRefinement h,string prefix)
    {
        string folder=Path.Combine(RefinementReview,"hole-"+h.hole.ToString("00"));Directory.CreateDirectory(folder);
        Vector2[] route=FrontNineRoutes()[h.hole-1];Vector2 green=PolygonCenter(FrontNineGreens()[h.hole-1]);
        Vector3 tc=GameObject.Find("Spline_Tee_Hole"+h.hole.ToString("00")+"_Red").GetComponent<MeshRenderer>().bounds.center;
        CaptureRefinementCamera(terrain,new Vector2(tc.x,tc.z),route[route.Length-2],h.cameraEye,Path.Combine(folder,prefix+"-tee.png"));
        Vector2 approach=Vector2.Lerp(route[1],green,.25f);
        CaptureRefinementCamera(terrain,approach,green,h.cameraEye,Path.Combine(folder,prefix+"-approach.png"));
        CaptureRefinementCamera(terrain,green+(green-approach).normalized*18,green,2.2f,Path.Combine(folder,prefix+"-green.png"));
        for(int w=0;w<7;w++)if(WaterOwners[w]==h.hole || (h.hole==2 && w==0))
        {Vector2 c=PolygonCenter(FrontNineWaters()[w]);CaptureRefinementCamera(terrain,c+new Vector2(35,45),c,18,Path.Combine(folder,prefix+"-water-"+w+".png"));}
        if(File.Exists(Path.Combine(RefinementReview,"latest-validation.txt")))File.Copy(Path.Combine(RefinementReview,"latest-validation.txt"),Path.Combine(folder,"validation.txt"),true);
    }
    private static void CaptureRefinementCamera(Terrain terrain,Vector2 p,Vector2 aim,float eye,string path)
    {
        GameObject go=new GameObject("Refinement review camera");Camera c=go.AddComponent<Camera>();c.transform.position=new Vector3(p.x,TerrainHeight(terrain,p)+eye,p.y);c.transform.LookAt(new Vector3(aim.x,TerrainHeight(terrain,aim)+1,aim.y));c.fieldOfView=62;c.nearClipPlane=.15f;c.farClipPlane=3000;c.clearFlags=CameraClearFlags.Skybox;c.allowHDR=false;
        RenderTexture rt=new RenderTexture(1920,1080,24);rt.antiAliasing=4;RenderTexture prior=RenderTexture.active;Texture2D image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
        try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
        finally{RenderTexture.active=prior;c.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
    }
    public static void ValidateRepeatability()
    {
        ValidateClipCases();
        EditorSceneManager.OpenScene(FinalScenePath,OpenSceneMode.Single);Terrain t=UnityEngine.Object.FindObjectOfType<Terrain>();RefinementSettings s=ReadRefinementSettings();
        ApplyEnabledRefinements(t,s);string first=HeightHash(t)+RefinementPhysicsHash()+TreeSignature();ApplyEnabledRefinements(t,s);string second=HeightHash(t)+RefinementPhysicsHash()+TreeSignature();
        if(first!=second)throw new InvalidOperationException("Repeated refinement changed content");
        File.WriteAllText(Path.Combine(RefinementReview,"repeatability.txt"),"PASS: repeated refinement preserves terrain, collision geometry and tree transforms.\n");
    }
    public static void ValidateFullRebuild()
    {
        RebuildRefinedFrontNine();Terrain t=UnityEngine.Object.FindObjectOfType<Terrain>();string first=HeightHash(t)+RefinementPhysicsHash()+TreeSignature();
        RebuildRefinedFrontNine();t=UnityEngine.Object.FindObjectOfType<Terrain>();string second=HeightHash(t)+RefinementPhysicsHash()+TreeSignature();
        if(first!=second)throw new InvalidOperationException("Full rebuild changed terrain, physics or tree placement");
        ValidateRepeatability();
        File.WriteAllText(Path.Combine(RefinementReview,"full-rebuild.txt"),"PASS: two complete source rebuilds produce identical terrain, collider content and tree transforms; repeated refinement also passed.\n");
    }
    private static double ClipArea(List<ClipVertex> p)
    {double a=0;for(int i=0;i<p.Count;i++){Vector3 v=p[i].p,w=p[(i+1)%p.Count].p;a+=(double)v.x*w.z-(double)w.x*v.z;}return Math.Abs(a)*.5;}
    private static void ValidateClipCases()
    {
        // A right triangle of area 8 cut at x=1 must partition into areas 3.5 and 4.5.
        var tri=new List<ClipVertex>{new ClipVertex(new Vector3(0,0,0),Vector2.zero),new ClipVertex(new Vector3(4,0,0),Vector2.right),new ClipVertex(new Vector3(0,0,4),Vector2.up)};
        var inside=ClipHalf(tri,new Vector2(1,-1),new Vector2(1,5),true);var outside=ClipHalf(tri,new Vector2(1,-1),new Vector2(1,5),false);
        if(Math.Abs(ClipArea(inside)-3.5)>.00001 || Math.Abs(ClipArea(outside)-4.5)>.00001)throw new InvalidOperationException("Shore crossing clipping test failed");
        var disjoint=ClipHalf(tri,new Vector2(-1,-1),new Vector2(-1,5),true);
        if(disjoint.Count!=0)throw new InvalidOperationException("Disjoint clipping test failed");
        var edge=ClipHalf(tri,new Vector2(0,-1),new Vector2(0,5),false);
        if(Math.Abs(ClipArea(edge)-8)>.00001)throw new InvalidOperationException("Coincident boundary clipping test failed");
    }
    private static string TreeSignature(){var rows=new List<string>();foreach(Transform t in UnityEngine.Object.FindObjectsOfType<Transform>())if(t.name.Contains(" mature ")||t.name.StartsWith("H1 "))rows.Add(t.name+"|"+t.position.ToString("R")+"|"+t.localScale.ToString("R")+"|"+t.rotation.ToString("R"));rows.Sort(StringComparer.Ordinal);return HashBytes(Encoding.UTF8.GetBytes(string.Join("\n",rows.ToArray())));}
}
