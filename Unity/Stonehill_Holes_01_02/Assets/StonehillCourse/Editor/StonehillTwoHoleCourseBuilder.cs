using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Generates a playable Stonehill front-nine beta directly in Unity from the
/// municipal orthophoto/DTM, the official 2025 scorecard, and GPS control points.
/// The class name is retained so existing automation hooks remain compatible.
/// </summary>
public static class StonehillTwoHoleCourseBuilder
{
    public const string FinalScenePath =
        "Assets/StonehillCourse/Scenes/Stonehill_9H.unity";

    private const string GeneratedFolder =
        "Assets/StonehillCourse/GeneratedFrontNine";
    private const string CourseRootName = "Stonehill Front-Nine Playable Course";
    private const string RawHeightmapPath =
        "Assets/StonehillCourse/SourceData/stonehill_heightmap_2049_le.raw";
    private const int HeightmapResolution = 2049;

    private const string RoughTexturePath =
        "Assets/Course Painting Textures/Rough.jpg";
    private const string AerialTexturePath =
        "Assets/StonehillCourse/SourceData/stonehill_coop2021_2048.jpg";
    private const string GrassTexturePath =
        "Assets/Course Painting Textures/Grass.jpg";
    private const string GrassNormalPath =
        "Assets/Course Painting Textures/Grass_normal.jpg";
    private const string RockGrassTexturePath =
        "Assets/Course Painting Textures/RockGrassy.jpg";
    private const string RockGrassNormalPath =
        "Assets/Course Painting Textures/RockGrassy_normal.jpg";
    private const string FairwayTexturePath =
        "Assets/Course Painting Textures/Fairway.jpg";
    private const string GreenTexturePath =
        "Assets/Course Painting Textures/Green.jpg";
    private const string TeeTexturePath =
        "Assets/Course Painting Textures/Tee.jpg";
    private const string BunkerTexturePath =
        "Assets/Course Painting Textures/Bunker.jpg";
    private const string FairwayMaterialPath =
        "Assets/Resources/Materials/Theme - Lush/OPCD MAHS Fairway lush.mat";
    private const string GreenMaterialPath =
        "Assets/Resources/Materials/Theme - Lush/OPCD MAHS Green lush.mat";
    private const string TeeMaterialPath =
        "Assets/Resources/Materials/Theme - Lush/OPCD MAHS Tee lush.mat";
    private const string BunkerMaterialPath =
        "Assets/Resources/Materials/Theme - Lush/Blend_OPCD MAHS Sand Raked Dirt Lip lush to OPCD MAHS Rough No Mowlines lush.mat";
    private const string WaterMaterialPath =
        "Assets/Resources/Materials/Theme - Lush/OPCD MAHS Water Base Lake lush.mat";

    private const string RoughPhysicsPath = "Assets/Resources/Physics/GSPrough.physicMaterial";
    private const string FairwayPhysicsPath = "Assets/Resources/Physics/GSPfairway.physicMaterial";
    private const string GreenPhysicsPath = "Assets/Resources/Physics/GSPgreen.physicMaterial";
    private const string TeePhysicsPath = "Assets/Resources/Physics/GSPtee.physicMaterial";
    private const string BunkerPhysicsPath = "Assets/Resources/Physics/GSPsand.physicMaterial";
    private const string WaterPhysicsPath = "Assets/Resources/Physics/GSPwater.physicMaterial";
    private const string CubeMapWaterMaterialPath = "Assets/CubeMapWater/CubeMap_Water.mat";
    private const string BirchPrefabPath = "Assets/Trees/White Birch/White_Birch.prefab";
    private const string ConiferPrefabPath = "Assets/Trees/Conifer/Conifer_Desktop.prefab";
    private const string DouglasFirPrefabPath = "Assets/TreeShare/DouglasFir/DouglasFir.prefab";
    private const string RockGroupPrefabPath = "Assets/Rocks and Boulders/Rocks/Prefabs/Rock1_grup1.prefab";
    private const string RockSinglePrefabPath = "Assets/Rocks and Boulders/Rocks/Prefabs/Rock4A.prefab";
    private const string RockAlternatePrefabPath = "Assets/Rocks and Boulders/Rocks/Prefabs/Rock5B.prefab";
    private const string WildGrassTexturePath =
        "Assets/Grasses_Flowers/Grass Textures3/Wildgrass.png";
    private const string DryGrassTexturePath =
        "Assets/Grasses_Flowers/Grass Textures3/grass07.png";

    private static readonly Vector2[] Hole1Fairway = SvgPolygon(new float[]
    {
        525.957f,678.355f, 541.784f,679.839f, 556.951f,679.689f, 572.449f,678.050f,
        585.143f,675.088f, 598.007f,670.659f, 614.657f,668.365f, 632.300f,664.760f,
        648.788f,664.099f, 662.964f,664.271f, 671.369f,666.741f, 690f,680f,
        710f,690f, 735f,700f, 725f,716f, 700f,708f, 680f,695f,
        672.196f,675.286f, 672.355f,680.386f, 669.880f,685.152f, 666.090f,690.240f,
        659.327f,693.860f, 649.437f,697.956f, 637.235f,699.930f, 619.755f,701.724f,
        606.570f,703.530f, 590.902f,705.658f, 576.227f,707.297f, 568.974f,706.639f,
        555.289f,707.290f, 538.640f,706.461f, 532.866f,714.348f, 519.675f,710.732f,
        505.339f,707.771f, 498.079f,707.113f, 489.343f,706.120f, 478.133f,706.938f,
        466.099f,710.223f, 452.905f,715.474f, 439.876f,730.258f, 426.186f,745.042f,
        415.794f,755.394f, 404.410f,766.068f, 378.683f,792.436f, 377.625f,768.446f,
        388.264f,755.550f, 393.869f,745.363f, 401.787f,735.010f, 415.314f,723.837f,
        426.363f,703.796f, 433.621f,690.976f, 446.156f,683.259f, 458.689f,677.841f,
        478.146f,673.082f, 497.439f,672.267f, 512.105f,674.739f
    });

    private static readonly Vector2[] Hole2Fairway = SvgPolygon(new float[]
    {
        480f,758f, 510f,760f, 540f,766f,
        571.836f,779.198f, 574.966f,771.144f, 582.230f,764.079f, 590.798f,761.127f,
        597.560f,759.485f, 603.991f,760.476f, 609.430f,762.945f, 618.334f,764.592f,
        632.181f,760.153f, 647.019f,758.191f, 655.923f,760.005f, 665.488f,763.620f,
        671.088f,768.056f, 677.024f,775.291f, 683.613f,785.816f, 694.491f,791.242f,
        704.878f,794.202f, 715.928f,792.727f, 722.850f,794.374f, 727.466f,798.487f,
        735.209f,806.701f, 742.138f,810.981f, 753.017f,812.296f, 764.503f,816.556f,
        751.335f,832.608f, 744.622f,826.716f, 737.514f,829.714f, 728.609f,826.255f,
        718.883f,824.441f, 706.191f,819.181f, 687.722f,813.752f, 665.634f,807.988f,
        644.859f,804.703f, 624.753f,798.940f, 608.097f,792.356f, 596.226f,793.341f,
        586.829f,793.504f, 579.745f,793.168f, 574.468f,788.077f,
        545f,780f, 510f,774f, 480f,770f
    });

    private static readonly Vector2[] Hole1Green = SvgPolygon(new float[]
    {
        375.311f,769.512f, 373.006f,771.567f, 370.361f,774.521f, 367.809f,778.631f,
        366.655f,782.164f, 366.653f,786.265f, 367.474f,789.387f, 369.448f,792.110f,
        372.666f,794.078f, 375.801f,794.157f, 378.683f,792.436f, 380.252f,789.148f,
        381.245f,784.715f, 382.153f,781.015f, 384.044f,777.649f, 384.376f,773.782f,
        383.309f,770.915f, 380.421f,768.769f, 377.625f,768.446f
    });

    private static readonly Vector2[] Hole2Green = SvgPolygon(new float[]
    {
        756.564f,821.431f, 758.739f,818.732f, 761.245f,817.321f, 764.503f,816.556f,
        766.462f,816.990f, 768.966f,818.469f, 770.102f,820.936f, 769.832f,824.047f,
        768.010f,828.913f, 765.704f,832.768f, 762.760f,835.689f, 759.386f,836.722f,
        755.583f,836.831f, 752.641f,835.341f, 751.335f,832.608f, 751.990f,827.941f,
        754.058f,824.575f
    });

    private static readonly Vector2[] Hole2Bunker = SvgPolygon(new float[]
    {
        742.400f,829.738f, 741.639f,830.771f, 741.585f,831.960f, 742.184f,832.727f,
        743.167f,833.372f, 744.689f,833.272f, 745.342f,832.239f, 745.773f,831.206f,
        745.558f,830.283f, 744.582f,829.694f, 743.491f,829.527f
    });

    private static readonly Vector2[] Pond = SvgPolygon(new float[]
    {
        535f,713f, 550f,708f, 575f,716f, 610f,710f, 650f,703f,
        690f,700f, 720f,711f, 735f,725f, 729f,742f, 700f,746f,
        668f,731f, 640f,736f, 620f,754f, 600f,742f, 575f,724f,
        558f,742f, 537f,732f
    });

    // Routing is stored in north-up orthophoto coordinates and converted to Unity
    // X/Z by SvgPolyline. These bends and widths were redigitized from the user's
    // highlighted panorama instead of extrapolating straight corridors from GPS.
    // Every route starts at the green and ends at the official white tee distance.
    private static readonly Vector2[] Hole1Route = SvgPolyline(new float[]
    {
        373f,783f, 395f,760f, 430f,720f, 485f,686f,
        555f,680f, 625f,674f, 690f,682f, 766f,716f
    });

    private static readonly Vector2[] Hole2Route = SvgPolyline(new float[]
    {
        761f,827f, 720f,804f, 667f,786f, 610f,775f,
        550f,768f, 500f,763f, 456f,759f
    });

    private static readonly Vector2[] Hole3Route = SvgPolyline(new float[]
    {
        438f,879f, 480f,860f, 535f,850f, 600f,851f, 655f,856f, 695f,857f
    });

    private static readonly Vector2[] Hole4Route = SvgPolyline(new float[]
    {
        711f,879f, 660f,873f, 605f,876f, 545f,885f, 485f,897f, 425f,910f
    });

    private static readonly Vector2[] Hole5Route = SvgPolyline(new float[]
    {
        802f,919f, 794f,890f, 784f,858f, 776f,830f, 767f,801f, 761f,782f
    });

    private static readonly Vector2[] Hole6Route = SvgPolyline(new float[]
    {
        675f,1005f, 708f,992f, 744f,976f, 778f,960f, 802f,949f
    });

    private static readonly Vector2[] Hole7Route = SvgPolyline(new float[]
    {
        769f,1019f, 725f,1038f, 670f,1047f, 610f,1051f, 550f,1054f, 505f,1055f
    });

    private static readonly Vector2[] Hole8Route = SvgPolyline(new float[]
    {
        868f,987f, 846f,1011f, 820f,1038f, 791f,1066f, 763f,1090f
    });

    private static readonly Vector2[] Hole9Route = SvgPolyline(new float[]
    {
        1015f,719f, 1018f,770f, 1015f,815f, 994f,854f,
        960f,889f, 918f,923f, 876f,946f
    });

    private static readonly Vector2[] Hole3Fairway = CorridorPolygon(Hole3Route, new[] { 10f, 17f, 22f, 24f, 18f, 7f });
    private static readonly Vector2[] Hole4Fairway = CorridorPolygon(Hole4Route, new[] { 10f, 18f, 23f, 24f, 17f, 7f });
    private static readonly Vector2[] Hole5Fairway = CorridorPolygon(Hole5Route, new[] { 10f, 15f, 19f, 18f, 12f, 7f });
    private static readonly Vector2[] Hole6Fairway = CorridorPolygon(Hole6Route, new[] { 10f, 17f, 20f, 15f, 7f });
    private static readonly Vector2[] Hole7Fairway = CorridorPolygon(Hole7Route, new[] { 10f, 18f, 23f, 24f, 16f, 7f });
    private static readonly Vector2[] Hole8Fairway = CorridorPolygon(Hole8Route, new[] { 10f, 18f, 20f, 15f, 7f });
    private static readonly Vector2[] Hole9Fairway = CorridorPolygon(Hole9Route, new[] { 11f, 16f, 22f, 24f, 22f, 15f, 7f });

    private static readonly Vector2[] Hole3Green = OrientedEllipse(Hole3Route[0], Hole3Route[1] - Hole3Route[0], 10.5f, 8f);
    private static readonly Vector2[] Hole4Green = OrientedEllipse(Hole4Route[0], Hole4Route[1] - Hole4Route[0], 10.5f, 8f);
    private static readonly Vector2[] Hole5Green = OrientedEllipse(Hole5Route[0], Hole5Route[1] - Hole5Route[0], 9.5f, 7.5f);
    private static readonly Vector2[] Hole6Green = OrientedEllipse(Hole6Route[0], Hole6Route[1] - Hole6Route[0], 9.5f, 7.5f);
    private static readonly Vector2[] Hole7Green = OrientedEllipse(Hole7Route[0], Hole7Route[1] - Hole7Route[0], 10.5f, 8f);
    private static readonly Vector2[] Hole8Green = OrientedEllipse(Hole8Route[0], Hole8Route[1] - Hole8Route[0], 9.5f, 7.5f);
    private static readonly Vector2[] Hole9Green = OrientedEllipse(Hole9Route[0], Hole9Route[1] - Hole9Route[0], 11f, 8.5f);

    private static readonly Vector2[] Hole4Pond = SvgPolygon(new float[]
    {
        586f,906f, 610f,899f, 647f,901f, 685f,908f, 716f,919f,
        704f,933f, 675f,941f, 635f,942f, 602f,933f, 586f,920f
    });

    private static readonly Vector2[] Hole5Pond = SvgPolygon(new float[]
    {
        970f,827f, 991f,821f, 1014f,827f, 1022f,840f,
        1014f,852f, 991f,855f, 971f,847f
    });

    private static readonly Vector2[] Hole6UpperPond = SvgPolygon(new float[]
    {
        873f,936f, 893f,928f, 911f,936f, 920f,951f,
        914f,968f, 899f,980f, 883f,970f, 876f,953f
    });

    private static readonly Vector2[] Hole6LowerPond = SvgPolygon(new float[]
    {
        823f,972f, 840f,966f, 855f,975f, 857f,991f,
        846f,1004f, 830f,1000f, 820f,987f
    });

    private static readonly Vector2[] Hole7Pond = SvgPolygon(new float[]
    {
        728f,1078f, 752f,1068f, 783f,1066f, 817f,1074f, 839f,1088f,
        829f,1102f, 800f,1110f, 762f,1108f, 735f,1097f
    });

    private static readonly Vector2[] Hole9Pond = SvgPolygon(new float[]
    {
        911f,660f, 929f,654f, 944f,663f, 949f,685f,
        946f,710f, 935f,729f, 920f,721f, 912f,700f
    });

    private static readonly Vector2[] Hole4Bunker = OrientedEllipse(new Vector2(492f, 2048f - 886f),
        Hole4Route[0] - Hole4Route[Hole4Route.Length - 1], 8f, 4f);
    private static readonly Vector2[] FrontNineTeeCenters = BuildFrontNineTeeCenters();

    [MenuItem("Stonehill/Build Front-Nine Playable Course")]
    public static void BuildPlayableCourse()
    {
        Terrain terrain = UnityEngine.Object.FindObjectOfType<Terrain>();
        if (terrain == null || terrain.terrainData == null)
            throw new InvalidOperationException("Create/open the Stonehill terrain before building the course.");

        Vector2[][] fairways = FrontNineFairways();
        Vector2[][] greens = FrontNineGreens();
        Vector2[][] routes = FrontNineRoutes();
        int[] pars = { 5, 4, 4, 4, 3, 3, 4, 3, 4 };
        int[] whiteYards = { 471, 344, 287, 318, 156, 151, 290, 161, 310 };
        int[] redYards = { 446, 308, 271, 293, 144, 132, 227, 128, 300 };
        Vector2[] whiteTees;
        Vector2[] redTees;
        BuildFrontNineTeePositions(routes, whiteYards, redYards, out whiteTees, out redTees);

        DeleteGeneratedContent();
        ResetAndShapeTerrain(terrain, greens, whiteTees, redTees, routes);
        ConfigureBaseRough(terrain);
        ConfigureSceneLighting();

        GameObject root = new GameObject(CourseRootName);
        GameObject surfaces = NewParent("Playing Surfaces", root.transform);
        GameObject markers = NewParent("GreenKeeper Markers - Front Nine", root.transform);

        for (int i = 0; i < 9; i++)
        {
            string suffix = (i + 1).ToString("00");
            CreateSurface("Spline_Fairway_Hole" + suffix, fairways[i], 1.0f, 0.010f, 0f, false,
                FairwayMaterialPath, FairwayPhysicsPath, terrain, surfaces.transform);
            CreateSurface("Spline_Green_Hole" + suffix, greens[i], 0.5f, 0.024f, 1.5f, false,
                GreenMaterialPath, GreenPhysicsPath, terrain, surfaces.transform);
        }

        CreateSurface("Spline_Bunker_Hole02", Hole2Bunker, 0.5f, 0.012f, 0f, false,
            BunkerMaterialPath, BunkerPhysicsPath, terrain, surfaces.transform);
        CreateSurface("Spline_Bunker_Hole04", Hole4Bunker, 0.5f, 0.012f, 0f, false,
            BunkerMaterialPath, BunkerPhysicsPath, terrain, surfaces.transform);
        Vector2[][] waters = FrontNineWaters();
        string[] waterNames =
        {
            "Holes01_02_Lake", "Hole04_Lake", "Hole05_Pond", "Hole06_UpperPond",
            "Hole06_LowerPond", "Hole07_Pond", "Hole09_Pond"
        };
        for (int i = 0; i < waters.Length; i++)
            CreateSurface("Spline_Water_" + waterNames[i], waters[i], 1.0f, 0.040f, 0f, true,
                WaterMaterialPath, WaterPhysicsPath, terrain, surfaces.transform);

        for (int i = 0; i < 9; i++)
        {
            string suffix = (i + 1).ToString("00");
            Vector2 aim = routes[i].Length > 1 ? routes[i][routes[i].Length - 2] : routes[i][0];
            CreateTeeDeck("Spline_Tee_Hole" + suffix + "_White", whiteTees[i], aim - whiteTees[i],
                terrain, surfaces.transform);
            CreateTeeDeck("Spline_Tee_Hole" + suffix + "_Red", redTees[i], aim - redTees[i],
                terrain, surfaces.transform);
            CreateHoleMarkers(i + 1, pars[i], whiteYards[i], redYards[i],
                whiteTees[i], redTees[i], PolygonCenter(greens[i]), terrain, markers.transform);
        }

        AddExistingEnvironmentAssets(root, terrain);
        ConfigureTerrainDetails(terrain);
        AddCourseInformation(root);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), FinalScenePath))
            throw new IOException("Unity could not save " + FinalScenePath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = root;
        Debug.Log("STONEHILL_FRONT_NINE_READY scene=" + FinalScenePath +
                  " holes=9 tees=white/red yardages=official-2025");
    }

    [MenuItem("Stonehill/Package Front-Nine Course Bundle")]
    public static void PackageCourseBundle()
    {
        SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(FinalScenePath);
        if (scene == null)
            throw new FileNotFoundException("The generated Stonehill scene is missing.", FinalScenePath);

        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string outputRoot = Path.Combine(projectRoot, "StonehillBuildOutput");
        Directory.CreateDirectory(outputRoot);

        Terrain[] terrains = UnityEngine.Object.FindObjectsOfType<Terrain>();
        bool[] keepTerrainColliders = new bool[terrains.Length];
        for (int i = 0; i < keepTerrainColliders.Length; i++)
            keepTerrainColliders[i] = false;

        BuildCourseDLL.Encryption.MyScene = scene;
        BuildCourseDLL.Encryption.gspdir = outputRoot;
        BuildCourseDLL.Encryption.Terrains = terrains;
        BuildCourseDLL.Encryption.TerrainToggles = keepTerrainColliders;
        BuildCourseDLL.Encryption.DisableTerrains();

        try
        {
            BuildCourseDLL.Encryption.BuildBundle();
        }
        finally
        {
            BuildCourseDLL.Encryption.CleanUpDirectory();
        }

        Debug.Log("STONEHILL_FRONT_NINE_BUNDLE_BUILT output=" + outputRoot);
    }

    private static void DeleteGeneratedContent()
    {
        GameObject previous = GameObject.Find(CourseRootName);
        if (previous != null)
            UnityEngine.Object.DestroyImmediate(previous);

        if (AssetDatabase.IsValidFolder(GeneratedFolder))
            AssetDatabase.DeleteAsset(GeneratedFolder);
        EnsureAssetFolder(GeneratedFolder);
    }

    private static void ResetAndShapeTerrain(
        Terrain terrain,
        Vector2[][] greens,
        Vector2[] whiteTees,
        Vector2[] redTees,
        Vector2[][] routes)
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string rawPath = Path.Combine(projectRoot, RawHeightmapPath);
        if (!File.Exists(rawPath))
            throw new FileNotFoundException("Missing prepared Stonehill heightmap", rawPath);

        byte[] bytes = File.ReadAllBytes(rawPath);
        int expectedLength = HeightmapResolution * HeightmapResolution * 2;
        if (bytes.Length != expectedLength)
            throw new InvalidDataException("Unexpected Stonehill RAW byte length: " + bytes.Length);

        TerrainData data = terrain.terrainData;
        if (data.heightmapResolution != HeightmapResolution)
            throw new InvalidOperationException("Stonehill terrain must use a 2049 heightmap.");

        // Rebuild from the source on every run so terrain shaping is deterministic and
        // never compounds when the menu command is executed repeatedly.
        float[,] heights = new float[HeightmapResolution, HeightmapResolution];
        for (int unityRow = 0; unityRow < HeightmapResolution; unityRow++)
        {
            int sourceRow = HeightmapResolution - 1 - unityRow;
            for (int column = 0; column < HeightmapResolution; column++)
            {
                int offset = (sourceRow * HeightmapResolution + column) * 2;
                ushort value = (ushort)(bytes[offset] | (bytes[offset + 1] << 8));
                heights[unityRow, column] = value / 65535f;
            }
        }

        for (int i = 0; i < greens.Length; i++)
            FlattenPolygonHeight(terrain, heights, greens[i], 7f);

        for (int i = 0; i < whiteTees.Length; i++)
        {
            Vector2 aim = routes[i].Length > 1 ? routes[i][routes[i].Length - 2] : routes[i][0];
            FlattenPolygonHeight(terrain, heights,
                TeeRectangle(whiteTees[i], aim - whiteTees[i]), 4f);
            FlattenPolygonHeight(terrain, heights,
                TeeRectangle(redTees[i], aim - redTees[i]), 4f);
        }

        data.SetHeights(0, 0, heights);
        TerrainCollider collider = terrain.GetComponent<TerrainCollider>();
        if (collider != null)
            collider.terrainData = data;
        EditorUtility.SetDirty(data);
        EditorUtility.SetDirty(terrain);
    }

    private static void FlattenPolygonHeight(
        Terrain terrain, float[,] heights, Vector2[] polygon, float featherMetres)
    {
        TerrainData data = terrain.terrainData;
        Vector3 origin = terrain.transform.position;
        float xScale = (HeightmapResolution - 1f) / data.size.x;
        float zScale = (HeightmapResolution - 1f) / data.size.z;

        Vector2 center = PolygonCenter(polygon);
        float target = SampleHeightArray(heights, data, origin, center);
        for (int i = 0; i < polygon.Length; i++)
            target += SampleHeightArray(heights, data, origin, polygon[i]);
        target /= polygon.Length + 1f;

        float minX = center.x;
        float maxX = center.x;
        float minZ = center.y;
        float maxZ = center.y;
        for (int i = 0; i < polygon.Length; i++)
        {
            minX = Mathf.Min(minX, polygon[i].x);
            maxX = Mathf.Max(maxX, polygon[i].x);
            minZ = Mathf.Min(minZ, polygon[i].y);
            maxZ = Mathf.Max(maxZ, polygon[i].y);
        }

        int firstX = Mathf.Clamp(Mathf.FloorToInt((minX - featherMetres - origin.x) * xScale),
            0, HeightmapResolution - 1);
        int lastX = Mathf.Clamp(Mathf.CeilToInt((maxX + featherMetres - origin.x) * xScale),
            0, HeightmapResolution - 1);
        int firstZ = Mathf.Clamp(Mathf.FloorToInt((minZ - featherMetres - origin.z) * zScale),
            0, HeightmapResolution - 1);
        int lastZ = Mathf.Clamp(Mathf.CeilToInt((maxZ + featherMetres - origin.z) * zScale),
            0, HeightmapResolution - 1);

        for (int z = firstZ; z <= lastZ; z++)
        {
            float worldZ = origin.z + z / zScale;
            for (int x = firstX; x <= lastX; x++)
            {
                float worldX = origin.x + x / xScale;
                Vector2 point = new Vector2(worldX, worldZ);
                bool inside = PointInPolygon(point, polygon);
                float distance = inside ? 0f : DistanceToPolygon(point, polygon);
                if (!inside && distance >= featherMetres)
                    continue;

                float blend = inside ? 1f : 1f - distance / featherMetres;
                blend = blend * blend * (3f - 2f * blend);
                heights[z, x] = Mathf.Lerp(heights[z, x], target, blend);
            }
        }
    }

    private static float SampleHeightArray(
        float[,] heights, TerrainData data, Vector3 origin, Vector2 point)
    {
        int x = Mathf.Clamp(Mathf.RoundToInt(
            (point.x - origin.x) / data.size.x * (HeightmapResolution - 1)),
            0, HeightmapResolution - 1);
        int z = Mathf.Clamp(Mathf.RoundToInt(
            (point.y - origin.z) / data.size.z * (HeightmapResolution - 1)),
            0, HeightmapResolution - 1);
        return heights[z, x];
    }

    private static float DistanceToPolygon(Vector2 point, Vector2[] polygon)
    {
        float closest = float.MaxValue;
        int previous = polygon.Length - 1;
        for (int current = 0; current < polygon.Length; current++)
        {
            closest = Mathf.Min(closest,
                DistanceToSegment(point, polygon[previous], polygon[current]));
            previous = current;
        }
        return closest;
    }

    private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 segment = b - a;
        float denominator = segment.sqrMagnitude;
        if (denominator < 0.0001f)
            return Vector2.Distance(point, a);
        float t = Mathf.Clamp01(Vector2.Dot(point - a, segment) / denominator);
        return Vector2.Distance(point, a + segment * t);
    }

    private static void ConfigureBaseRough(Terrain terrain)
    {
        Texture2D aerial = LoadTexture(AerialTexturePath);
        Texture2D grass = LoadTexture(GrassTexturePath);
        Texture2D grassNormal = LoadTexture(GrassNormalPath);
        Texture2D rockGrass = LoadTexture(RockGrassTexturePath);
        Texture2D rockGrassNormal = LoadTexture(RockGrassNormalPath);
        Texture2D fairway = LoadTexture(FairwayTexturePath);
        Texture2D green = LoadTexture(GreenTexturePath);
        Texture2D tee = LoadTexture(TeeTexturePath);
        Texture2D bunker = LoadTexture(BunkerTexturePath);

        TerrainData data = terrain.terrainData;
        data.splatPrototypes = new[]
        {
            NewTerrainLayer(grass, grassNormal, 22f),
            NewTerrainLayer(rockGrass, rockGrassNormal, 32f),
            NewTerrainLayer(aerial, null, data.size.x),
            NewTerrainLayer(fairway, grassNormal, 12f),
            NewTerrainLayer(green, grassNormal, 8f),
            NewTerrainLayer(tee, grassNormal, 8f),
            NewTerrainLayer(bunker, null, 7f)
        };

        Vector2[][] routes = FrontNineRoutes();
        Vector2[][] fairways = FrontNineFairways();
        Vector2[][] greens = FrontNineGreens();
        Vector2[][] bunkers = FrontNineBunkers();
        int[] whiteYards = { 471, 344, 287, 318, 156, 151, 290, 161, 310 };
        int[] redYards = { 446, 308, 271, 293, 144, 132, 227, 128, 300 };
        Vector2[] whiteTees;
        Vector2[] redTees;
        BuildFrontNineTeePositions(routes, whiteYards, redYards, out whiteTees, out redTees);
        List<Vector2[]> teePolygons = new List<Vector2[]>();
        for (int i = 0; i < 9; i++)
        {
            Vector2 aim = routes[i].Length > 1 ? routes[i][routes[i].Length - 2] : routes[i][0];
            teePolygons.Add(TeeRectangle(whiteTees[i], aim - whiteTees[i]));
            teePolygons.Add(TeeRectangle(redTees[i], aim - redTees[i]));
        }

        float[,,] weights = new float[data.alphamapHeight, data.alphamapWidth, 7];
        for (int z = 0; z < data.alphamapHeight; z++)
        {
            for (int x = 0; x < data.alphamapWidth; x++)
            {
                float nx = x / (float)(data.alphamapWidth - 1);
                float nz = z / (float)(data.alphamapHeight - 1);
                float slope = data.GetSteepness(nx, nz);
                float rockWeight = Mathf.Clamp01((slope - 10f) / 30f) * 0.50f + 0.07f;
                float aerialWeight = 0.035f;
                float grassWeight = 1f - rockWeight - aerialWeight;
                weights[z, x, 0] = Mathf.Max(0.10f, grassWeight);
                weights[z, x, 1] = rockWeight;
                weights[z, x, 2] = aerialWeight;

                Vector2 worldPoint = new Vector2(
                    terrain.transform.position.x + nx * data.size.x,
                    terrain.transform.position.z + nz * data.size.z);
                if (worldPoint.x < 250f || worldPoint.x > 1100f ||
                    worldPoint.y < 900f || worldPoint.y > 1460f)
                    continue;

                int paintedLayer = -1;
                if (PointInAnyPolygon(worldPoint, greens))
                    paintedLayer = 4;
                else if (PointInAnyPolygon(worldPoint, bunkers))
                    paintedLayer = 6;
                else if (PointInAnyPolygon(worldPoint, teePolygons))
                    paintedLayer = 5;
                else if (PointInAnyPolygon(worldPoint, fairways))
                    paintedLayer = 3;

                if (paintedLayer >= 0)
                {
                    for (int layer = 0; layer < 7; layer++)
                        weights[z, x, layer] = 0f;
                    weights[z, x, paintedLayer] = 1f;
                }
            }
        }
        data.SetAlphamaps(0, 0, weights);

        TerrainCollider collider = terrain.GetComponent<TerrainCollider>();
        if (collider == null)
            collider = terrain.gameObject.AddComponent<TerrainCollider>();
        collider.terrainData = data;
        collider.material = LoadPhysics(RoughPhysicsPath);
        EditorUtility.SetDirty(data);
        EditorUtility.SetDirty(collider);
    }

    private static Texture2D LoadTexture(string path)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null)
            throw new FileNotFoundException("Missing terrain texture", path);
        return texture;
    }

    private static SplatPrototype NewTerrainLayer(Texture2D texture, Texture2D normal, float tileSize)
    {
        return new SplatPrototype
        {
            texture = texture,
            normalMap = normal,
            tileSize = new Vector2(tileSize, tileSize),
            tileOffset = Vector2.zero,
            metallic = 0f,
            smoothness = 0f
        };
    }

    private static void ConfigureSceneLighting()
    {
        GameObject postProcessing = GameObject.Find("PostProcessingVolume");
        if (postProcessing != null)
            postProcessing.SetActive(false);

        Material sky = AssetDatabase.LoadAssetAtPath<Material>(
            GeneratedFolder + "/Stonehill Runtime Sky.mat");
        if (sky == null)
        {
            Shader shader = Shader.Find("Skybox/Procedural");
            if (shader == null)
                throw new InvalidOperationException("Procedural skybox shader is unavailable.");
            sky = new Material(shader);
            sky.name = "Stonehill Runtime Sky";
            sky.SetFloat("_SunSize", 0.035f);
            sky.SetFloat("_SunSizeConvergence", 5f);
            sky.SetFloat("_AtmosphereThickness", 0.75f);
            sky.SetColor("_SkyTint", new Color(0.34f, 0.48f, 0.64f, 1f));
            sky.SetColor("_GroundColor", new Color(0.24f, 0.27f, 0.23f, 1f));
            sky.SetFloat("_Exposure", 0.46f);
            AssetDatabase.CreateAsset(sky, GeneratedFolder + "/Stonehill Runtime Sky.mat");
        }
        RenderSettings.skybox = sky;
        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 0.38f;
        RenderSettings.reflectionIntensity = 0.18f;
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.34f, 0.43f, 0.48f, 1f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 850f;
        RenderSettings.fogEndDistance = 2600f;

        Light[] lights = UnityEngine.Object.FindObjectsOfType<Light>();
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i].type != LightType.Directional)
                continue;
            lights[i].intensity = 0.58f;
            lights[i].color = new Color(1f, 0.94f, 0.84f, 1f);
            lights[i].shadows = LightShadows.Soft;
            EditorUtility.SetDirty(lights[i]);
        }
    }

    private static GameObject CreateSurface(
        string name,
        Vector2[] polygon,
        float gridSize,
        float heightOffset,
        float smoothingRadius,
        bool constantHeight,
        string materialPath,
        string physicsPath,
        Terrain terrain,
        Transform parent)
    {
        Mesh mesh = CreateGridMesh(name, polygon, gridSize, heightOffset, smoothingRadius,
            constantHeight, terrain);
        string meshPath = GeneratedFolder + "/" + name + ".asset";
        AssetDatabase.CreateAsset(mesh, meshPath);

        GameObject surface = new GameObject(name);
        surface.transform.SetParent(parent, false);
        MeshFilter filter = surface.AddComponent<MeshFilter>();
        MeshRenderer renderer = surface.AddComponent<MeshRenderer>();
        MeshCollider collider = surface.AddComponent<MeshCollider>();
        filter.sharedMesh = mesh;
        renderer.sharedMaterial = LoadOrCreatePlayableMaterial(materialPath);
        collider.sharedMesh = mesh;
        collider.sharedMaterial = LoadPhysics(physicsPath);
        if (physicsPath == WaterPhysicsPath)
        {
            int waterLayer = LayerMask.NameToLayer("Water");
            if (waterLayer >= 0)
                surface.layer = waterLayer;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = true;
        }
        else
        {
            // Turf/sand appearance is painted directly into TerrainData. Keep this
            // conforming mesh only for the GSPro PhysicMaterial hit classification.
            renderer.enabled = false;
        }
        surface.isStatic = true;
        return surface;
    }

    private static void CreateTeeDeck(
        string name,
        Vector2 center,
        Vector2 playDirection,
        Terrain terrain,
        Transform parent)
    {
        Vector2[] rectangle = TeeRectangle(center, playDirection);
        CreateSurface(name, rectangle, 0.5f, 0.006f, 0f, false,
            TeeMaterialPath, TeePhysicsPath, terrain, parent);
    }

    private static Vector2[] TeeRectangle(Vector2 center, Vector2 playDirection)
    {
        Vector2 forward = playDirection.normalized;
        Vector2 right = new Vector2(forward.y, -forward.x);
        const float halfLength = 6.5f;
        const float halfWidth = 4.5f;
        return new[]
        {
            center - forward * halfLength - right * halfWidth,
            center + forward * halfLength - right * halfWidth,
            center + forward * halfLength + right * halfWidth,
            center - forward * halfLength + right * halfWidth
        };
    }

    private static Mesh CreateGridMesh(
        string name,
        Vector2[] polygon,
        float gridSize,
        float heightOffset,
        float smoothingRadius,
        bool constantHeight,
        Terrain terrain)
    {
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minZ = float.MaxValue;
        float maxZ = float.MinValue;
        for (int i = 0; i < polygon.Length; i++)
        {
            minX = Mathf.Min(minX, polygon[i].x);
            maxX = Mathf.Max(maxX, polygon[i].x);
            minZ = Mathf.Min(minZ, polygon[i].y);
            maxZ = Mathf.Max(maxZ, polygon[i].y);
        }

        minX = Mathf.Floor(minX / gridSize) * gridSize;
        minZ = Mathf.Floor(minZ / gridSize) * gridSize;
        int cellsX = Mathf.CeilToInt((maxX - minX) / gridSize);
        int cellsZ = Mathf.CeilToInt((maxZ - minZ) / gridSize);

        float flatHeight = 0f;
        if (constantHeight)
        {
            for (int i = 0; i < polygon.Length; i++)
                flatHeight += TerrainHeight(terrain, polygon[i]);
            flatHeight = flatHeight / polygon.Length + heightOffset;
        }

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();
        Dictionary<long, int> vertexMap = new Dictionary<long, int>();

        for (int z = 0; z < cellsZ; z++)
        {
            for (int x = 0; x < cellsX; x++)
            {
                Vector2 center = new Vector2(
                    minX + (x + 0.5f) * gridSize,
                    minZ + (z + 0.5f) * gridSize);
                if (!PointInPolygon(center, polygon))
                    continue;

                int v00 = GridVertex(x, z, minX, minZ, gridSize, heightOffset, smoothingRadius,
                    constantHeight, flatHeight, terrain, vertices, uvs, vertexMap);
                int v01 = GridVertex(x, z + 1, minX, minZ, gridSize, heightOffset, smoothingRadius,
                    constantHeight, flatHeight, terrain, vertices, uvs, vertexMap);
                int v11 = GridVertex(x + 1, z + 1, minX, minZ, gridSize, heightOffset, smoothingRadius,
                    constantHeight, flatHeight, terrain, vertices, uvs, vertexMap);
                int v10 = GridVertex(x + 1, z, minX, minZ, gridSize, heightOffset, smoothingRadius,
                    constantHeight, flatHeight, terrain, vertices, uvs, vertexMap);
                // Match Unity Terrain's two-triangle cell topology. The prior four-way
                // center fan interpolated differently from the terrain and caused the
                // thin playing surface to weave above and below it between vertices.
                triangles.Add(v00); triangles.Add(v01); triangles.Add(v11);
                triangles.Add(v00); triangles.Add(v11); triangles.Add(v10);
            }
        }

        if (triangles.Count == 0)
            throw new InvalidOperationException("Generated surface has no triangles: " + name);

        Mesh mesh = new Mesh();
        mesh.name = name + " Mesh";
        if (vertices.Count > 65000)
            mesh.indexFormat = IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static int GridVertex(
        int x,
        int z,
        float minX,
        float minZ,
        float gridSize,
        float heightOffset,
        float smoothingRadius,
        bool constantHeight,
        float flatHeight,
        Terrain terrain,
        List<Vector3> vertices,
        List<Vector2> uvs,
        Dictionary<long, int> vertexMap)
    {
        long key = ((long)x << 32) ^ (uint)z;
        int existing;
        if (vertexMap.TryGetValue(key, out existing))
            return existing;

        float worldX = minX + x * gridSize;
        float worldZ = minZ + z * gridSize;
        float worldY = constantHeight
            ? flatHeight
            : SmoothedTerrainHeight(terrain, new Vector2(worldX, worldZ), smoothingRadius) + heightOffset;
        int index = vertices.Count;
        vertices.Add(new Vector3(worldX, worldY, worldZ));
        uvs.Add(new Vector2(worldX / 12f, worldZ / 12f));
        vertexMap.Add(key, index);
        return index;
    }

    private static void CreateHoleMarkers(
        int hole,
        int par,
        int whiteYards,
        int redYards,
        Vector2 white,
        Vector2 red,
        Vector2 pinCenter,
        Terrain terrain,
        Transform parent)
    {
        GameObject holeParent = NewParent(
            "Hole " + hole.ToString("00") + " - Par " + par,
            parent);
        CreateMarker("GK_H" + hole.ToString("00") + "_Tee_White_" + whiteYards + "yd",
            white, terrain, holeParent.transform);
        CreateMarker("GK_H" + hole.ToString("00") + "_Tee_Red_" + redYards + "yd",
            red, terrain, holeParent.transform);

        Vector2[] offsets = new Vector2[]
        {
            new Vector2(-1.7f, 0.8f),
            new Vector2(1.6f, 1.1f),
            new Vector2(-0.8f, -1.8f),
            new Vector2(1.3f, -1.3f)
        };
        string[] days = new[] { "Thursday", "Friday", "Saturday", "Sunday" };
        for (int i = 0; i < days.Length; i++)
            CreateMarker("GK_H" + hole.ToString("00") + "_Pin_" + days[i],
                pinCenter + offsets[i], terrain, holeParent.transform);
    }

    private static void CreateMarker(string name, Vector2 point, Terrain terrain, Transform parent)
    {
        GameObject marker = new GameObject(name);
        marker.transform.SetParent(parent, false);
        marker.transform.position = new Vector3(
            point.x,
            TerrainHeight(terrain, point) + 0.060f,
            point.y);
    }

    private static void AddCourseInformation(GameObject root)
    {
        GameObject information = NewParent("COURSE INFO - Stonehill Golf Club, Sudbury ON", root.transform);
        NewParent("FRONT NINE ACTIVE - PAR 34", information.transform);
        NewParent("Official 2025 white yardages: 471, 344, 287, 318, 156, 151, 290, 161, 310", information.transform);
        NewParent("Official 2025 red yardages: 446, 308, 271, 293, 144, 132, 227, 128, 300", information.transform);
        NewParent("White is the requested GSPro display name for Stonehill's published Blue tee", information.transform);
        NewParent("Routing aligned from orthophoto, official map and independent GPS control points", information.transform);
    }

    private static float TerrainHeight(Terrain terrain, Vector2 point)
    {
        return terrain.SampleHeight(new Vector3(point.x, 0f, point.y)) + terrain.transform.position.y;
    }

    private static float SmoothedTerrainHeight(Terrain terrain, Vector2 point, float radius)
    {
        if (radius <= 0.01f)
            return TerrainHeight(terrain, point);

        float total = 0f;
        int samples = 0;
        for (int z = -1; z <= 1; z++)
        {
            for (int x = -1; x <= 1; x++)
            {
                total += TerrainHeight(terrain, point + new Vector2(x * radius, z * radius));
                samples++;
            }
        }
        return total / samples;
    }

    private static void ConfigureTerrainDetails(Terrain terrain)
    {
        TerrainData data = terrain.terrainData;
        Texture2D wildGrass = LoadTexture(WildGrassTexturePath);
        Texture2D dryGrass = LoadTexture(DryGrassTexturePath);
        data.detailPrototypes = new[]
        {
            NewGrassDetail(wildGrass, 0.55f, 1.15f,
                new Color(0.30f, 0.43f, 0.19f, 1f), new Color(0.47f, 0.38f, 0.20f, 1f)),
            NewGrassDetail(dryGrass, 0.45f, 0.90f,
                new Color(0.36f, 0.43f, 0.21f, 1f), new Color(0.52f, 0.42f, 0.23f, 1f))
        };
        data.SetDetailResolution(512, 16);

        int resolution = data.detailResolution;
        int[,] wild = new int[resolution, resolution];
        int[,] dry = new int[resolution, resolution];
        for (int z = 0; z < resolution; z++)
        {
            float nz = z / (float)(resolution - 1);
            for (int x = 0; x < resolution; x++)
            {
                float nx = x / (float)(resolution - 1);
                Vector2 point = new Vector2(
                    terrain.transform.position.x + nx * data.size.x,
                    terrain.transform.position.z + nz * data.size.z);
                if (point.x < 250f || point.x > 1100f || point.y < 900f || point.y > 1460f ||
                    IsNearPlayingSurface(point, 7f) || IsNearAnyTee(point, 10f) || IsInsideWater(point) ||
                    data.GetSteepness(nx, nz) > 26f)
                    continue;

                int hash = unchecked(x * 73856093 ^ z * 19349663 ^ 0x5f3759df) & 1023;
                if (hash < 230)
                    wild[z, x] = 1 + (hash % 2);
                else if (hash > 870)
                    dry[z, x] = 1;
            }
        }
        data.SetDetailLayer(0, 0, 0, wild);
        data.SetDetailLayer(0, 0, 1, dry);
        terrain.detailObjectDistance = 180f;
        terrain.detailObjectDensity = 0.65f;
        EditorUtility.SetDirty(data);
        EditorUtility.SetDirty(terrain);
    }

    private static DetailPrototype NewGrassDetail(
        Texture2D texture, float minWidth, float maxWidth, Color healthy, Color dry)
    {
        return new DetailPrototype
        {
            prototypeTexture = texture,
            renderMode = DetailRenderMode.GrassBillboard,
            usePrototypeMesh = false,
            minWidth = minWidth,
            maxWidth = maxWidth,
            minHeight = minWidth * 1.4f,
            maxHeight = maxWidth * 1.45f,
            noiseSpread = 0.18f,
            healthyColor = healthy,
            dryColor = dry
        };
    }

    private static void AddExistingEnvironmentAssets(GameObject root, Terrain terrain)
    {
        GameObject environment = NewParent("Environment - Stonehill Existing Assets", root.transform);
        GameObject trees = NewParent("Young Birch Pine and Fir Mix", environment.transform);
        GameObject rocks = NewParent("Small Canadian Shield Outcrops", environment.transform);

        GameObject[] treePrefabs =
        {
            LoadPrefab(BirchPrefabPath), LoadPrefab(ConiferPrefabPath), LoadPrefab(DouglasFirPrefabPath)
        };
        GameObject[] rockPrefabs =
        {
            LoadPrefab(RockSinglePrefabPath), LoadPrefab(RockAlternatePrefabPath)
        };

        System.Random random = new System.Random(20250902);
        int treesPlaced = 0;
        for (int attempt = 0; attempt < 5000 && treesPlaced < 190; attempt++)
        {
            Vector2 point = new Vector2(
                Mathf.Lerp(265f, 1090f, (float)random.NextDouble()),
                Mathf.Lerp(905f, 1455f, (float)random.NextDouble()));
            if (IsNearPlayingSurface(point, 18f) || IsNearAnyTee(point, 18f) ||
                IsInsideWater(point) || TerrainSlope(terrain, point) > 34f)
                continue;

            int choice = random.Next(100);
            int prefabIndex = choice < 48 ? 0 : (choice < 82 ? 1 : 2);
            float scale = Mathf.Lerp(0.48f, 0.90f, (float)random.NextDouble());
            PlaceEnvironmentPrefab(treePrefabs[prefabIndex], "Tree", treesPlaced,
                point, scale, 0f, (float)random.NextDouble() * 360f, terrain, trees.transform);
            treesPlaced++;
        }

        int rocksPlaced = 0;
        for (int attempt = 0; attempt < 4000 && rocksPlaced < 46; attempt++)
        {
            Vector2 point = new Vector2(
                Mathf.Lerp(270f, 1080f, (float)random.NextDouble()),
                Mathf.Lerp(910f, 1445f, (float)random.NextDouble()));
            float slope = TerrainSlope(terrain, point);
            if (slope < 11f || slope > 38f || IsNearPlayingSurface(point, 24f) ||
                IsNearAnyTee(point, 24f) || IsInsideWater(point))
                continue;

            float scale = Mathf.Lerp(0.14f, 0.30f, (float)random.NextDouble());
            PlaceEnvironmentPrefab(rockPrefabs[rocksPlaced % rockPrefabs.Length], "Rock", rocksPlaced,
                point, scale, -0.22f, (float)random.NextDouble() * 360f, terrain, rocks.transform);
            rocksPlaced++;
        }

        NewParent("PLACEMENT RULE - no trees within 18m or rocks within 24m of playing surfaces",
            environment.transform);
    }

    private static GameObject LoadPrefab(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
            throw new FileNotFoundException("Missing environment prefab", path);
        return prefab;
    }

    private static void PlaceEnvironmentPrefab(
        GameObject prefab, string prefix, int index, Vector2 point, float scale,
        float yOffset, float yaw, Terrain terrain, Transform parent)
    {
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
            throw new InvalidOperationException("Could not instantiate " + prefab.name);
        instance.name = prefix + "_" + (index + 1).ToString("000");
        instance.transform.SetParent(parent, true);
        instance.transform.position = new Vector3(point.x, TerrainHeight(terrain, point) + yOffset, point.y);
        instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        instance.transform.localScale *= scale;
        SetStaticRecursively(instance);
    }

    private static float TerrainSlope(Terrain terrain, Vector2 point)
    {
        TerrainData data = terrain.terrainData;
        float nx = Mathf.InverseLerp(terrain.transform.position.x,
            terrain.transform.position.x + data.size.x, point.x);
        float nz = Mathf.InverseLerp(terrain.transform.position.z,
            terrain.transform.position.z + data.size.z, point.y);
        return data.GetSteepness(nx, nz);
    }

    private static void AddLegacyEnvironmentAssets(GameObject root, Terrain terrain)
    {
        GameObject environment = NewParent("Environment - Existing Project Assets", root.transform);
        GameObject trees = NewParent("Mixed Birch and Conifers", environment.transform);
        GameObject rocks = NewParent("Canadian Shield Rock Groups", environment.transform);

        PlacePrefabSet(BirchPrefabPath, "Birch", new[]
        {
            new Vector3(343f,1248f,.82f), new Vector3(350f,1292f,.95f),
            new Vector3(392f,1238f,.88f), new Vector3(410f,1302f,1.02f),
            new Vector3(425f,1404f,.90f), new Vector3(458f,1413f,1.06f),
            new Vector3(492f,1418f,.83f), new Vector3(526f,1423f,.98f),
            new Vector3(562f,1420f,.86f), new Vector3(603f,1417f,1.04f),
            new Vector3(644f,1410f,.92f), new Vector3(687f,1402f,1.00f),
            new Vector3(718f,1388f,.84f), new Vector3(548f,1300f,.91f),
            new Vector3(582f,1284f,.80f), new Vector3(621f,1280f,1.03f),
            new Vector3(691f,1295f,.87f), new Vector3(728f,1281f,.98f),
            new Vector3(552f,1188f,.92f), new Vector3(590f,1182f,1.04f),
            new Vector3(632f,1180f,.85f), new Vector3(674f,1185f,.96f),
            new Vector3(716f,1188f,.88f), new Vector3(755f,1191f,1.00f),
            new Vector3(790f,1210f,.86f), new Vector3(786f,1252f,.95f)
        }, terrain, trees.transform, 0f);

        PlacePrefabSet(ConiferPrefabPath, "Conifer", new[]
        {
            new Vector3(332f,1260f,.88f), new Vector3(365f,1227f,1.05f),
            new Vector3(440f,1428f,.96f), new Vector3(510f,1434f,1.12f),
            new Vector3(578f,1431f,.91f), new Vector3(662f,1421f,1.06f),
            new Vector3(724f,1402f,.94f), new Vector3(535f,1278f,.87f),
            new Vector3(606f,1270f,1.04f), new Vector3(681f,1280f,.92f),
            new Vector3(536f,1174f,1.08f), new Vector3(614f,1168f,.93f),
            new Vector3(696f,1171f,1.02f), new Vector3(772f,1184f,.90f),
            new Vector3(804f,1230f,1.06f)
        }, terrain, trees.transform, 0f);

        PlacePrefabSet(DouglasFirPrefabPath, "Fir", new[]
        {
            new Vector3(378f,1218f,.62f), new Vector3(477f,1437f,.68f),
            new Vector3(618f,1435f,.64f), new Vector3(709f,1416f,.67f),
            new Vector3(571f,1268f,.61f), new Vector3(739f,1300f,.66f),
            new Vector3(577f,1164f,.64f), new Vector3(745f,1174f,.63f)
        }, terrain, trees.transform, 0f);

        PlacePrefabSet(RockGroupPrefabPath, "RockGroup", new[]
        {
            new Vector3(318f,1345f,.65f), new Vector3(350f,1415f,.78f),
            new Vector3(440f,1450f,.62f), new Vector3(560f,1460f,.72f),
            new Vector3(680f,1440f,.68f), new Vector3(775f,1395f,.80f),
            new Vector3(825f,1325f,.66f), new Vector3(830f,1240f,.74f),
            new Vector3(780f,1155f,.70f), new Vector3(650f,1135f,.76f),
            new Vector3(520f,1140f,.64f), new Vector3(390f,1170f,.72f)
        }, terrain, rocks.transform, -.30f, 14f);

        PlacePrefabSet(RockSinglePrefabPath, "Boulder", new[]
        {
            new Vector3(330f,1385f,.58f), new Vector3(400f,1442f,.72f),
            new Vector3(500f,1450f,.55f), new Vector3(610f,1450f,.66f),
            new Vector3(730f,1415f,.60f), new Vector3(818f,1290f,.70f),
            new Vector3(740f,1140f,.62f), new Vector3(560f,1130f,.68f)
        }, terrain, rocks.transform, -.22f, 14f);
    }

    private static void PlacePrefabSet(
        string prefabPath, string namePrefix, Vector3[] placements,
        Terrain terrain, Transform parent, float yOffset,
        float playingSurfaceClearance = 4f)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
            throw new FileNotFoundException("Missing environment prefab", prefabPath);

        for (int i = 0; i < placements.Length; i++)
        {
            Vector2 point = new Vector2(placements[i].x, placements[i].y);
            if (IsNearPlayingSurface(point, playingSurfaceClearance))
            {
                Debug.Log("Skipping " + namePrefix + " placement inside the playing corridor at " + point);
                continue;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
                throw new InvalidOperationException("Could not instantiate " + prefabPath);
            instance.name = namePrefix + "_" + (i + 1).ToString("00");
            instance.transform.SetParent(parent, true);
            instance.transform.position = new Vector3(
                point.x, TerrainHeight(terrain, point) + yOffset, point.y);
            instance.transform.rotation = Quaternion.Euler(0f, (i * 137.5f) % 360f, 0f);
            instance.transform.localScale *= placements[i].z;
            SetStaticRecursively(instance);
        }
    }

    private static bool IsNearPlayingSurface(Vector2 point, float clearance)
    {
        Vector2[][] fairways = FrontNineFairways();
        Vector2[][] greens = FrontNineGreens();
        for (int i = 0; i < fairways.Length; i++)
        {
            if (IsInsideOrNearPolygon(point, fairways[i], clearance) ||
                IsInsideOrNearPolygon(point, greens[i], clearance))
                return true;
        }
        return false;
    }

    private static bool IsInsideWater(Vector2 point)
    {
        Vector2[][] waters = FrontNineWaters();
        for (int i = 0; i < waters.Length; i++)
        {
            if (PointInPolygon(point, waters[i]))
                return true;
        }
        return false;
    }

    private static bool IsNearAnyTee(Vector2 point, float clearance)
    {
        float clearanceSquared = clearance * clearance;
        for (int i = 0; i < FrontNineTeeCenters.Length; i++)
        {
            if ((point - FrontNineTeeCenters[i]).sqrMagnitude <= clearanceSquared)
                return true;
        }
        return false;
    }

    private static bool IsInsideOrNearPolygon(Vector2 point, Vector2[] polygon, float clearance)
    {
        if (PointInPolygon(point, polygon))
            return true;

        float clearanceSquared = clearance * clearance;
        for (int i = 0; i < polygon.Length; i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[(i + 1) % polygon.Length];
            Vector2 segment = b - a;
            float denominator = segment.sqrMagnitude;
            float t = denominator > 0.0001f
                ? Mathf.Clamp01(Vector2.Dot(point - a, segment) / denominator)
                : 0f;
            Vector2 closest = a + segment * t;
            if ((point - closest).sqrMagnitude <= clearanceSquared)
                return true;
        }
        return false;
    }

    private static void SetStaticRecursively(GameObject root)
    {
        root.isStatic = true;
        foreach (Transform child in root.transform)
            SetStaticRecursively(child.gameObject);
    }

    private static Vector2[][] FrontNineRoutes()
    {
        return new[]
        {
            Hole1Route, Hole2Route, Hole3Route, Hole4Route, Hole5Route,
            Hole6Route, Hole7Route, Hole8Route, Hole9Route
        };
    }

    private static Vector2[][] FrontNineFairways()
    {
        return new[]
        {
            Hole1Fairway, Hole2Fairway, Hole3Fairway, Hole4Fairway, Hole5Fairway,
            Hole6Fairway, Hole7Fairway, Hole8Fairway, Hole9Fairway
        };
    }

    private static Vector2[][] FrontNineGreens()
    {
        return new[]
        {
            Hole1Green, Hole2Green, Hole3Green, Hole4Green, Hole5Green,
            Hole6Green, Hole7Green, Hole8Green, Hole9Green
        };
    }

    private static Vector2[][] FrontNineBunkers()
    {
        return new[] { Hole2Bunker, Hole4Bunker };
    }

    private static Vector2[][] FrontNineWaters()
    {
        return new[]
        {
            Pond, Hole4Pond, Hole5Pond, Hole6UpperPond,
            Hole6LowerPond, Hole7Pond, Hole9Pond
        };
    }

    private static Vector2[] BuildFrontNineTeeCenters()
    {
        Vector2[][] routes = FrontNineRoutes();
        int[] whiteYards = { 471, 344, 287, 318, 156, 151, 290, 161, 310 };
        int[] redYards = { 446, 308, 271, 293, 144, 132, 227, 128, 300 };
        Vector2[] whiteTees;
        Vector2[] redTees;
        BuildFrontNineTeePositions(routes, whiteYards, redYards, out whiteTees, out redTees);
        Vector2[] result = new Vector2[18];
        for (int i = 0; i < 9; i++)
        {
            result[i * 2] = whiteTees[i];
            result[i * 2 + 1] = redTees[i];
        }
        return result;
    }

    private static void BuildFrontNineTeePositions(
        Vector2[][] routes,
        int[] whiteYards,
        int[] redYards,
        out Vector2[] whiteTees,
        out Vector2[] redTees)
    {
        whiteTees = new Vector2[routes.Length];
        redTees = new Vector2[routes.Length];
        for (int i = 0; i < routes.Length; i++)
        {
            // A route begins at its green. Walking the official scorecard distance
            // along it makes the markers, tee decks, and metadata agree in world scale.
            whiteTees[i] = PointAtDistanceFromStart(routes[i], whiteYards[i] * 0.9144f);
            redTees[i] = PointAtDistanceFromStart(routes[i], redYards[i] * 0.9144f);
        }
    }

    private static Vector2 PointAtFractionFromStart(Vector2[] route, float fraction)
    {
        float total = 0f;
        for (int i = 0; i < route.Length - 1; i++)
            total += Vector2.Distance(route[i], route[i + 1]);
        return PointAtDistanceFromStart(route, total * Mathf.Clamp01(fraction));
    }

    private static Vector2 PointAtDistanceFromStart(Vector2[] route, float distance)
    {
        float remaining = distance;
        for (int i = 0; i < route.Length - 1; i++)
        {
            float segment = Vector2.Distance(route[i], route[i + 1]);
            if (remaining <= segment)
                return Vector2.Lerp(route[i], route[i + 1], remaining / segment);
            remaining -= segment;
        }

        Vector2 direction = (route[route.Length - 1] - route[route.Length - 2]).normalized;
        return route[route.Length - 1] + direction * remaining;
    }

    private static Vector2 PolygonCenter(Vector2[] polygon)
    {
        Vector2 total = Vector2.zero;
        for (int i = 0; i < polygon.Length; i++)
            total += polygon[i];
        return total / polygon.Length;
    }

    private static Vector2[] CorridorPolygon(Vector2[] route, float[] halfWidths)
    {
        if (route == null || route.Length < 2 || halfWidths == null || halfWidths.Length != route.Length)
            throw new ArgumentException("A fairway corridor requires one width per route point.");

        Vector2[] polygon = new Vector2[route.Length * 2];
        for (int i = 0; i < route.Length; i++)
        {
            Vector2 tangent;
            if (i == 0)
                tangent = route[1] - route[0];
            else if (i == route.Length - 1)
                tangent = route[i] - route[i - 1];
            else
                tangent = (route[i] - route[i - 1]).normalized + (route[i + 1] - route[i]).normalized;
            tangent.Normalize();
            Vector2 right = new Vector2(tangent.y, -tangent.x);
            polygon[i] = route[i] + right * halfWidths[i];
            polygon[polygon.Length - 1 - i] = route[i] - right * halfWidths[i];
        }
        return polygon;
    }

    private static Vector2[] OrientedEllipse(
        Vector2 center, Vector2 playDirection, float alongRadius, float acrossRadius)
    {
        const int segments = 24;
        Vector2 forward = playDirection.sqrMagnitude > 0.001f ? playDirection.normalized : Vector2.up;
        Vector2 right = new Vector2(forward.y, -forward.x);
        Vector2[] polygon = new Vector2[segments];
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            polygon[i] = center + forward * (Mathf.Cos(angle) * alongRadius) +
                         right * (Mathf.Sin(angle) * acrossRadius);
        }
        return polygon;
    }

    private static bool PointInAnyPolygon(Vector2 point, IEnumerable<Vector2[]> polygons)
    {
        foreach (Vector2[] polygon in polygons)
        {
            if (polygon != null && PointInPolygon(point, polygon))
                return true;
        }
        return false;
    }

    private static bool PointInPolygon(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        int previous = polygon.Length - 1;
        for (int current = 0; current < polygon.Length; current++)
        {
            Vector2 a = polygon[current];
            Vector2 b = polygon[previous];
            if (((a.y > point.y) != (b.y > point.y)) &&
                (point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x))
                inside = !inside;
            previous = current;
        }
        return inside;
    }

    private static Vector2[] SvgPolygon(float[] values)
    {
        Vector2[] result = new Vector2[values.Length / 2];
        for (int i = 0; i < result.Length; i++)
            result[i] = new Vector2(values[i * 2], 2048f - values[i * 2 + 1]);
        return result;
    }

    private static Vector2[] SvgPolyline(float[] values)
    {
        return SvgPolygon(values);
    }

    private static Material LoadOrCreatePlayableMaterial(string sourcePath)
    {
        string kind;
        string texturePath;
        Color color = Color.white;
        float smoothness = 0.12f;

        if (sourcePath.IndexOf("Fairway", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            kind = "Fairway";
            texturePath = FairwayTexturePath;
            color = new Color(0.40f, 0.66f, 0.35f, 1f);
        }
        else if (sourcePath.IndexOf("Green", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            kind = "Green";
            texturePath = GreenTexturePath;
            color = new Color(0.32f, 0.61f, 0.29f, 1f);
            smoothness = 0.18f;
        }
        else if (sourcePath.IndexOf("Tee", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            kind = "Tee";
            texturePath = TeeTexturePath;
            color = new Color(0.42f, 0.69f, 0.37f, 1f);
        }
        else if (sourcePath.IndexOf("Sand", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            kind = "Bunker";
            texturePath = BunkerTexturePath;
            color = new Color(0.62f, 0.50f, 0.31f, 1f);
        }
        else
        {
            kind = "Water";
            texturePath = null;
            color = new Color(0.035f, 0.19f, 0.29f, 1f);
            smoothness = 0.82f;

            // Reuse the BaseProject's lake material so the water has normal/reflection
            // detail instead of appearing as a flat blue painted polygon.
            string lakePath = GeneratedFolder + "/Stonehill Existing Lake Variant.mat";
            Material lakeMaterial = AssetDatabase.LoadAssetAtPath<Material>(lakePath);
            if (lakeMaterial != null)
                return lakeMaterial;
            Material lakeSource = AssetDatabase.LoadAssetAtPath<Material>(CubeMapWaterMaterialPath);
            if (lakeSource != null)
            {
                lakeMaterial = new Material(lakeSource);
                lakeMaterial.name = "Stonehill Existing Lake Variant";
                lakeMaterial.SetColor("_Color", new Color(0.06f, 0.15f, 0.20f, 1f));
                lakeMaterial.SetColor("_ReflectColor", new Color(0.10f, 0.19f, 0.25f, 0.30f));
                lakeMaterial.SetColor("_SpecColor", new Color(0.32f, 0.38f, 0.40f, 1f));
                lakeMaterial.SetFloat("_Shininess", 0.24f);
                AssetDatabase.CreateAsset(lakeMaterial, lakePath);
                return lakeMaterial;
            }
        }

        string assetPath = GeneratedFolder + "/Playable " + kind + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (material != null)
            return material;

        Shader shader = Shader.Find("Standard");
        if (shader == null)
            throw new InvalidOperationException("Unity Standard shader is unavailable.");
        material = new Material(shader);
        material.name = "Stonehill Playable " + kind;
        material.color = color;
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Glossiness", smoothness);
        if (!string.IsNullOrEmpty(texturePath))
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
                throw new FileNotFoundException("Missing surface texture", texturePath);
            material.mainTexture = texture;
        }
        AssetDatabase.CreateAsset(material, assetPath);
        return material;
    }

    private static PhysicMaterial LoadPhysics(string path)
    {
        PhysicMaterial material = AssetDatabase.LoadAssetAtPath<PhysicMaterial>(path);
        if (material == null)
            throw new FileNotFoundException("Missing physics material", path);
        return material;
    }

    private static GameObject NewParent(string name, Transform parent)
    {
        GameObject result = new GameObject(name);
        if (parent != null)
            result.transform.SetParent(parent, false);
        return result;
    }

    private static void EnsureAssetFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
