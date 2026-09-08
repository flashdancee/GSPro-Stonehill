using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

public static partial class StonehillTwoHoleCourseBuilder
{
    private const string DetailFolder = "Assets/StonehillCourse/HoleOneDetail";
    private const string DetailRoot = "Hole 01 - Mature woodland and fine turf";
    private static readonly string ReviewFolder = Path.GetFullPath(Path.Combine(Application.dataPath, "../StonehillArtifacts/HoleOneDetail"));

    private static void ApplyHoleOneDetailLegacy()
    {
        EditorSceneManager.OpenScene(FinalScenePath, OpenSceneMode.Single);
        Terrain terrain = UnityEngine.Object.FindObjectOfType<Terrain>();
        if (terrain == null) throw new InvalidOperationException("Missing course terrain");
        Directory.CreateDirectory(ReviewFolder);
        CaptureDetailViews(terrain, "before");
        // Clone the terrain once: the pre-detail terrain remains available for rollback.
        string terrainPath = DetailFolder + "/HoleOneTerrain.asset";
        TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(terrainPath);
        if (data == null)
        {
            data = UnityEngine.Object.Instantiate(terrain.terrainData);
            data.name = "Stonehill terrain - Hole 1 detail";
            AssetDatabase.CreateAsset(data, terrainPath);
        }
        else if (terrain.terrainData != data) EditorUtility.CopySerialized(terrain.terrainData,data);
        terrain.terrainData = data;
        terrain.GetComponent<TerrainCollider>().terrainData = data;
        float[,] heightsBefore = data.GetHeights(0,0,data.heightmapResolution,data.heightmapResolution);
        string physicsBefore = PhysicsSignature();
        RefineHoleOneTerrain(terrain);
        RefineHoleOneTurf(terrain);
        GameObject previous = GameObject.Find(DetailRoot);
        if (previous != null) UnityEngine.Object.DestroyImmediate(previous);
        GameObject root = new GameObject(DetailRoot);
        int treeCount = PlantHoleOneWoodland(terrain, root.transform);
        AddHoleOneGrass(terrain);
        RefineHoleOneLake();
        ConfigureRefinedLighting(terrain);
        if (PhysicsSignature() != physicsBefore) throw new InvalidOperationException("Playing-surface physics changed");
        float[,] heightsAfter = data.GetHeights(0,0,data.heightmapResolution,data.heightmapResolution);
        for (int z=0; z<data.heightmapResolution; z++)
            for (int x=0; x<data.heightmapResolution; x++)
                if (heightsBefore[z,x] != heightsAfter[z,x]) throw new InvalidOperationException("Terrain elevation changed");
        EditorUtility.SetDirty(data);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene())) throw new IOException("Scene save failed");
        CaptureDetailViews(terrain, "after");
        File.WriteAllText(Path.Combine(ReviewFolder,"validation.txt"), "PASS: terrain elevations identical; all playing-surface mesh/physics references and transforms identical.\nWoodland instances: " + treeCount + "\nScene: " + FinalScenePath + "\nUTC: " + DateTime.UtcNow.ToString("o"));
        Debug.Log("HOLE_ONE_DETAIL_COMPLETE trees=" + treeCount);
    }

    private static void ConfigureRefinedLighting(Terrain terrain)
    {
        // Daylight fill is needed for the package's cutout foliage and dark terrain.
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.63f,0.72f,0.82f);
        RenderSettings.ambientEquatorColor = new Color(0.42f,0.48f,0.36f);
        RenderSettings.ambientGroundColor = new Color(0.23f,0.26f,0.18f);
        foreach (Light light in UnityEngine.Object.FindObjectsOfType<Light>())
            if (light.type == LightType.Directional)
            {
                light.intensity = 1.05f;
                light.color = new Color(1f,0.97f,0.90f);
                light.transform.rotation = Quaternion.Euler(46f,142f,0f);
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 0.72f;
                RenderSettings.sun = light;
            }
        if (RenderSettings.skybox != null)
        {
            Material sky = new Material(RenderSettings.skybox);
            sky.SetFloat("_Exposure",1.1f);
            sky.SetFloat("_AtmosphereThickness",1.0f);
            SaveDetailAsset(sky, DetailFolder + "/Daylight Sky.mat");
            RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>(DetailFolder + "/Daylight Sky.mat");
        }
        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.pixelLightCount = 4;
        QualitySettings.masterTextureLimit = 0;
        QualitySettings.maximumLODLevel = 0;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        QualitySettings.shadowDistance = 220f;
        QualitySettings.shadowResolution = ShadowResolution.High;
        QualitySettings.shadowCascades = 4;
        QualitySettings.lodBias = 2f;
        terrain.heightmapPixelError = 2f;
        terrain.basemapDistance = 1000f;
        RenderSettings.fogColor = new Color(0.66f,0.76f,0.81f);
    }

    private static void SaveDetailAsset(UnityEngine.Object asset, string path)
    {
        UnityEngine.Object existing = AssetDatabase.LoadMainAssetAtPath(path);
        if (existing == null) AssetDatabase.CreateAsset(asset,path);
        else { EditorUtility.CopySerialized(asset, existing); UnityEngine.Object.DestroyImmediate(asset); EditorUtility.SetDirty(existing); }
    }

    private static float HoleOneDistance(Vector2 p)
    {
        float d = float.MaxValue;
        for (int i=1;i<Hole1Route.Length;i++) d = Mathf.Min(d, DistanceToSegment(p,Hole1Route[i-1],Hole1Route[i]));
        return d;
    }

    private static void RefineHoleOneTerrain(Terrain terrain)
    {
        TerrainData data = terrain.terrainData;
        SplatPrototype[] old = data.splatPrototypes;
        float[,,] original = data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);
        // Keep all original layers and remap only the Hole 1 neighbourhood.
        const int baseCount = 9;
        SplatPrototype[] layers = new SplatPrototype[baseCount+5];
        Array.Copy(old,layers,baseCount);
        layers[9] = NewTerrainLayer(LoadTexture("Assets/GroundTextures/seamlessgrass.jpg"),null,1.4f);
        layers[10] = NewTerrainLayer(LoadTexture(FairwayTexturePath),null,2.2f);
        layers[11] = NewTerrainLayer(LoadTexture(GreenTexturePath),null,1.0f);
        layers[12] = NewTerrainLayer(LoadTexture(TeeTexturePath),null,1.5f);
        layers[13] = NewTerrainLayer(LoadTexture("Assets/GroundTextures/seamlessgrass.jpg"),null,0.9f);
        data.splatPrototypes = layers;
        float[,,] weights = new float[data.alphamapHeight,data.alphamapWidth,layers.Length];
        int[] remap = {9,1,2,10,11,12,6,13,8};
        for (int z=0;z<data.alphamapHeight;z++) for (int x=0;x<data.alphamapWidth;x++)
        {
            Vector2 p = new Vector2(terrain.transform.position.x+x*data.size.x/(data.alphamapWidth-1),terrain.transform.position.z+z*data.size.z/(data.alphamapHeight-1));
            float fade = 1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(62f,92f,HoleOneDistance(p)));
            for (int l=0;l<baseCount;l++)
            {
                float value = original[z,x,l];
                // Fold previous pass's appended layers back before reapplying (idempotent).
                if (old.Length > baseCount && remap[l]>=baseCount) value += original[z,x,remap[l]];
                weights[z,x,l] += value*(1f-fade);
                weights[z,x,remap[l]] += value*fade;
            }
        }
        data.SetAlphamaps(0,0,weights);
    }

    private static void RefineHoleOneTurf(Terrain terrain)
    {
        foreach (string suffix in new[]{"Fairway_Hole01","Green_Hole01","Tee_Hole01_White","Tee_Hole01_Red"})
        {
            GameObject surface = GameObject.Find("Spline_"+suffix);
            if (surface == null) throw new InvalidOperationException("Missing " + suffix);
            bool green = suffix.StartsWith("Green");
            Material material = new Material(Shader.Find("Stonehill/Fine Turf"));
            material.SetTexture("_MainTex",LoadTexture(green ? GreenTexturePath : FairwayTexturePath));
            material.SetColor("_Color",green ? new Color(0.39f,0.51f,0.21f) : new Color(0.25f,0.39f,0.115f));
            material.SetFloat("_Tile",green ? 0.9f : 1.6f);
            material.SetFloat("_Stripe",green ? 0.018f : 0.065f);
            string path = DetailFolder+"/"+suffix+".mat";
            SaveDetailAsset(material,path);
            MeshRenderer renderer = surface.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
            renderer.enabled = false;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
        }
        // Shade the terrain itself; physics meshes intentionally remain invisible.
        // This avoids cracks and coarse staircase silhouettes along the spline cells.
        const int width=1024, height=512;
        Texture2D mask=new Texture2D(width,height,TextureFormat.RGBA32,false,true);
        mask.wrapMode=TextureWrapMode.Clamp; mask.filterMode=FilterMode.Bilinear;
        Color[] pixels=new Color[width*height];
        List<Vector2[]> teeShapes=new List<Vector2[]>();
        foreach(string name in new[]{"Spline_Tee_Hole01_White","Spline_Tee_Hole01_Red"})
        {
            Vector3 center=GameObject.Find(name).GetComponent<MeshRenderer>().bounds.center;
            Vector2 c=new Vector2(center.x,center.z);
            teeShapes.Add(TeeRectangle(c,Hole1Route[Hole1Route.Length-2]-c));
        }
        for(int z=0;z<height;z++) for(int x=0;x<width;x++)
        {
            Vector2 p=new Vector2(300f+(x+0.5f)*520f/width,1230f+(z+0.5f)*250f/height);
            float fairway=ShapeMask(p,Hole1Fairway);
            float green=ShapeMask(p,Hole1Green);
            float tee=Mathf.Max(ShapeMask(p,teeShapes[0]),ShapeMask(p,teeShapes[1]));
            pixels[z*width+x]=new Color(fairway*(1f-green),green,tee,0f);
        }
        mask.SetPixels(pixels); mask.Apply();
        SaveDetailAsset(mask,DetailFolder+"/Turf mask.asset");
        Material terrainMaterial=new Material(Shader.Find("Stonehill/Detailed Terrain"));
        terrainMaterial.SetTexture("_HoleMask",AssetDatabase.LoadAssetAtPath<Texture2D>(DetailFolder+"/Turf mask.asset"));
        terrainMaterial.SetTexture("_FineGrass",LoadTexture("Assets/GroundTextures/seamlessgrass.jpg"));
        terrainMaterial.SetTexture("_FineNormal",LoadTexture(GrassNormalPath));
        terrainMaterial.SetVector("_HoleRect",new Vector4(300f,1230f,520f,250f));
        SaveDetailAsset(terrainMaterial,DetailFolder+"/Detailed Terrain.mat");
        terrain.materialType=Terrain.MaterialType.Custom;
        terrain.materialTemplate=AssetDatabase.LoadAssetAtPath<Material>(DetailFolder+"/Detailed Terrain.mat");
    }

    private static float ShapeMask(Vector2 p,Vector2[] polygon)
    {
        float signed=DistanceToPolygon(p,polygon)*(PointInPolygon(p,polygon) ? 1f : -1f);
        return Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(-0.7f,0.8f,signed));
    }

    private static int PlantHoleOneWoodland(Terrain terrain, Transform parent)
    {
        string[] paths = { BirchPrefabPath, MaplePrefabPath, BroadleafPrefabPath, ConiferPrefabPath };
        GameObject[] prefabs = new GameObject[paths.Length];
        for (int i=0;i<paths.Length;i++) prefabs[i] = LoadPrefab(paths[i]);
        System.Random random = new System.Random(1072026);
        List<Vector2> placed = new List<Vector2>();
        // A continuous right-hand belt, following the dogleg from tee to behind green.
        Vector2[] edge = SvgPolyline(new float[]{790,681, 742,650, 680,634, 620,635, 555,638, 490,634, 444,643, 412,668, 386,702, 360,742, 343,782, 350,813});
        for (int segment=1;segment<edge.Length;segment++)
        {
            Vector2 direction = (edge[segment]-edge[segment-1]).normalized;
            Vector2 outward = new Vector2(direction.y,-direction.x);
            float length = Vector2.Distance(edge[segment-1],edge[segment]);
            for (float step=0;step<length;step+=7f)
            for (int row=0;row<4;row++)
            {
                Vector2 p = edge[segment-1]+direction*(step+(float)random.NextDouble()*5f)+outward*(row*10f+(float)random.NextDouble()*8f-4f);
                if (IsNearPlayingSurface(p,9f) || IsNearAnyTee(p,14f) || IsNearWater(p,5f) || TerrainSlope(terrain,p)>43f) continue;
                bool overlaps = false;
                foreach (Vector2 other in placed) if (Vector2.Distance(other,p)<5.7f) { overlaps=true; break; }
                if (overlaps) continue;
                int choice = random.Next(100);
                int species = choice<42 ? 0 : choice<69 ? 1 : choice<83 ? 2 : 3;
                GameObject instance = PrefabUtility.InstantiatePrefab(prefabs[species]) as GameObject;
                instance.name = "H1 " + prefabs[species].name + " " + placed.Count.ToString("000");
                instance.transform.SetParent(parent,false);
                // Normalize actual renderer bounds instead of blindly shrinking disparate prefabs.
                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
                Bounds bounds = new Bounds(); bool first=true;
                foreach (Renderer renderer in renderers) { if(first) { bounds=renderer.bounds; first=false; } else bounds.Encapsulate(renderer.bounds); }
                float height = Mathf.Lerp(row==0 ? 10f:13f,row==0 ? 17f:21f,(float)random.NextDouble());
                instance.transform.localScale *= height/Mathf.Max(bounds.size.y,0.5f);
                instance.transform.rotation = Quaternion.Euler(0f,(float)random.NextDouble()*360f,0f);
                instance.transform.position = new Vector3(p.x,TerrainHeight(terrain,p)-0.12f,p.y);
                foreach (Renderer renderer in renderers) { renderer.shadowCastingMode=ShadowCastingMode.On; renderer.receiveShadows=true; }
                foreach (LODGroup lod in instance.GetComponentsInChildren<LODGroup>()) lod.RecalculateBounds();
                SetStaticRecursively(instance);
                placed.Add(p);
            }
        }
        return placed.Count;
    }

    private static void AddHoleOneGrass(Terrain terrain)
    {
        TerrainData data = terrain.terrainData;
        List<DetailPrototype> prototypes = new List<DetailPrototype>(data.detailPrototypes);
        if(prototypes.Count==2)
        {
            DetailPrototype shortGrass = NewGrassDetail(LoadTexture("Assets/Grasses_Flowers/Grass Textures3/GrassLawn1.png"),0.23f,0.45f,new Color(0.55f,0.64f,0.34f),new Color(0.64f,0.62f,0.36f));
            shortGrass.minHeight=0.13f; shortGrass.maxHeight=0.27f;
            prototypes.Add(shortGrass);
            data.detailPrototypes=prototypes.ToArray();
        }
        int r = data.detailResolution;
        int[,] shortLayer = new int[r,r];
        int[,] wild = data.GetDetailLayer(0,0,r,r,0);
        for(int z=0;z<r;z++) for(int x=0;x<r;x++)
        {
            Vector2 p=new Vector2(terrain.transform.position.x+(x+0.5f)*data.size.x/r,terrain.transform.position.z+(z+0.5f)*data.size.z/r);
            if(HoleOneDistance(p)>78f || IsNearPlayingSurface(p,4.5f) || IsNearAnyTee(p,8f) || IsNearWater(p,2f) || TerrainSlope(terrain,p)>32f) continue;
            float noise=Mathf.PerlinNoise(p.x*0.075f,p.y*0.075f);
            shortLayer[z,x]=noise>0.35f ? 18 : 7;
            if(IsNearWater(p,9f) || (HoleOneDistance(p)>32f && noise>0.55f)) wild[z,x]=3;
        }
        data.SetDetailLayer(0,0,2,shortLayer);
        data.SetDetailLayer(0,0,0,wild);
        terrain.detailObjectDistance=150f;
        terrain.detailObjectDensity=0.9f;
    }

    private static void RefineHoleOneLake()
    {
        GameObject lake=GameObject.Find("Spline_Water_Holes01_02_Lake");
        Material material=new Material(AssetDatabase.LoadAssetAtPath<Material>(CubeMapWaterMaterialPath));
        material.SetColor("_Color",new Color(0.22f,0.30f,0.31f,1f));
        material.SetColor("_ReflectColor",new Color(0.50f,0.58f,0.62f,0.65f));
        material.SetColor("_SpecColor",new Color(0.72f,0.76f,0.78f,1f));
        material.SetFloat("_Shininess",0.42f);
        material.SetTextureScale("_BumpMap",new Vector2(2f,2f));
        SaveDetailAsset(material,DetailFolder+"/Hole 1 Lake.mat");
        lake.GetComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(DetailFolder+"/Hole 1 Lake.mat");
    }

    public static void PackageHoleOneDetail()
    {
        EditorSceneManager.OpenScene(FinalScenePath,OpenSceneMode.Single);
        if(Shader.Find("Hidden/TerrainEngine/Splatmap/Standard-Base")==null)
            throw new InvalidOperationException("Terrain base shader unavailable");
        PackageCourseBundle();
    }

    private static string PhysicsSignature()
    {
        List<string> entries=new List<string>();
        foreach(MeshCollider c in UnityEngine.Object.FindObjectsOfType<MeshCollider>())
            if(c.name.StartsWith("Spline_")) entries.Add(c.name+"|"+AssetDatabase.GetAssetPath(c.sharedMesh)+"|"+AssetDatabase.GetAssetPath(c.sharedMaterial)+"|"+c.transform.localToWorldMatrix.ToString());
        entries.Sort(); return string.Join("\n",entries.ToArray());
    }

    public static void CaptureHoleOneReview()
    {
        EditorSceneManager.OpenScene(FinalScenePath,OpenSceneMode.Single);
        CaptureDetailViews(UnityEngine.Object.FindObjectOfType<Terrain>(),"after");
    }

    private static void CaptureDetailViews(Terrain terrain,string prefix)
    {
        Directory.CreateDirectory(ReviewFolder);
        GameObject tee=GameObject.Find("Spline_Tee_Hole01_Red");
        Vector3 teePoint=tee.GetComponent<MeshRenderer>().bounds.center;
        CaptureDetailCamera(terrain,new Vector2(teePoint.x,teePoint.z),new Vector2(625f,1374f),prefix+"-tee",1.7f);
        CaptureDetailCamera(terrain,new Vector2(570f,1364f),new Vector2(430f,1328f),prefix+"-fairway",1.7f);
        CaptureDetailCamera(terrain,new Vector2(436f,1325f),PolygonCenter(Hole1Green),prefix+"-approach",1.7f);
        CaptureDetailCamera(terrain,new Vector2(390f,1290f),PolygonCenter(Hole1Green),prefix+"-green",2.2f);
    }

    private static void CaptureDetailCamera(Terrain terrain,Vector2 p,Vector2 aim,string name,float eye)
    {
        GameObject go=new GameObject("Hole 1 review camera");
        Camera camera=go.AddComponent<Camera>();
        camera.transform.position=new Vector3(p.x,TerrainHeight(terrain,p)+eye,p.y);
        Vector3 target=new Vector3(aim.x,TerrainHeight(terrain,aim)+1.0f,aim.y);
        camera.transform.rotation=Quaternion.LookRotation(target-camera.transform.position);
        camera.fieldOfView=62f; camera.nearClipPlane=0.15f; camera.farClipPlane=3000f;
        camera.clearFlags=CameraClearFlags.Skybox;
        camera.allowHDR=false;
        RenderTexture rt=new RenderTexture(1920,1080,24); rt.antiAliasing=4;
        RenderTexture previous=RenderTexture.active;
        Texture2D image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
        try { camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt; image.ReadPixels(new Rect(0,0,1920,1080),0,0); image.Apply(); File.WriteAllBytes(Path.Combine(ReviewFolder,name+".png"),image.EncodeToPNG()); }
        finally { RenderTexture.active=previous; camera.targetTexture=null; UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(go); }
    }
}
