using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// A deliberately small file-based bridge for repeatable Stonehill front-nine editor work.
/// It accepts only the commands listed in ExecuteCommand; it cannot execute
/// arbitrary shell commands or arbitrary C#.
/// </summary>
[InitializeOnLoad]
public static class StonehillUnityAutomation
{
    private const string StonehillScenePath =
        "Assets/StonehillCourse/Scenes/Stonehill_Holes_01_02.unity";

    private static readonly string ProjectRoot =
        Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    private static readonly string RequestPath =
        Path.Combine(ProjectRoot, "StonehillAutomation.request");
    private static readonly string ArtifactDirectory =
        Path.Combine(ProjectRoot, "StonehillArtifacts");
    private static readonly string ResponsePath =
        Path.Combine(ArtifactDirectory, "last-response.json");

    private static double nextPollTime;

    [Serializable]
    private class AutomationResponse
    {
        public string command;
        public bool succeeded;
        public string message;
        public string utcTime;
        public string scenePath;
        public bool sceneDirty;
        public bool terrainFound;
        public string terrainName;
        public int heightmapResolution;
        public int alphamapLayers;
        public float positionX;
        public float positionY;
        public float positionZ;
        public float sizeX;
        public float sizeY;
        public float sizeZ;
        public string topImage;
        public string perspectiveImage;
        public string heightImage;
        public string holesTopImage;
        public string holesPerspectiveImage;
    }

    static StonehillUnityAutomation()
    {
        EditorApplication.update += PollForRequest;
        Debug.Log("Stonehill automation bridge ready. Request file: " + RequestPath);
    }

    private static void PollForRequest()
    {
        if (EditorApplication.timeSinceStartup < nextPollTime)
            return;

        nextPollTime = EditorApplication.timeSinceStartup + 0.75d;
        if (!File.Exists(RequestPath))
            return;

        string command = string.Empty;
        try
        {
            command = File.ReadAllText(RequestPath).Trim().ToLowerInvariant();
            File.Delete(RequestPath);
            ExecuteCommand(command);
        }
        catch (Exception exception)
        {
            WriteResponse(BuildResponse(
                command,
                false,
                exception.GetType().Name + ": " + exception.Message));
            Debug.LogException(exception);
        }
    }

    private static void ExecuteCommand(string command)
    {
        switch (command)
        {
            case "validate":
                WriteResponse(BuildResponse(command, true, "Scene validation completed."));
                break;

            case "capture":
            case "capture-validation":
                CaptureValidationImages();
                CaptureHoleImages();
                WriteResponse(BuildResponse(
                    command,
                    true,
                    "Validation images and terrain report created."));
                break;

            case "save-scene":
                bool saved = EditorSceneManager.SaveOpenScenes();
                WriteResponse(BuildResponse(
                    command,
                    saved,
                    saved ? "Open scenes saved." : "Unity did not save every open scene."));
                break;

            case "open-stonehill-scene":
                EditorSceneManager.OpenScene(StonehillScenePath, OpenSceneMode.Single);
                WriteResponse(BuildResponse(command, true, "Stonehill scene opened."));
                break;

            case "apply-aerial":
                StonehillTerrainReferenceOverlay.ApplyAerialReferenceOverlay();
                WriteResponse(BuildResponse(command, true, "Aerial reference overlay applied."));
                break;

            case "remove-aerial":
                StonehillTerrainReferenceOverlay.RemoveAerialReferenceOverlay();
                WriteResponse(BuildResponse(command, true, "Aerial reference overlay removed."));
                break;

            case "build-two-hole":
            case "build-front-nine":
                StonehillTwoHoleCourseBuilder.BuildPlayableCourse();
                CaptureValidationImages();
                CaptureHoleImages();
                WriteResponse(BuildResponse(
                    command,
                    true,
                    "Front-nine playable scene generated, saved and captured."));
                break;

            case "package-course":
            case "package-front-nine":
                StonehillTwoHoleCourseBuilder.PackageCourseBundle();
                WriteResponse(BuildResponse(
                    command,
                    true,
                    "Encrypted Stonehill front-nine course bundle packaged."));
                break;

            case "capture-hole1-red-tee":
                CaptureHoleOneRedTeeImage();
                WriteResponse(BuildResponse(
                    command,
                    true,
                    "Hole 1 red-tee view rendered."));
                break;

            default:
                WriteResponse(BuildResponse(
                    command,
                    false,
                    "Unknown command. Allowed: validate, capture-validation, save-scene, " +
                    "open-stonehill-scene, apply-aerial, remove-aerial, build-front-nine, package-front-nine, " +
                    "capture-hole1-red-tee."));
                break;
        }
    }

    private static AutomationResponse BuildResponse(
        string command,
        bool succeeded,
        string message)
    {
        AutomationResponse response = new AutomationResponse
        {
            command = command,
            succeeded = succeeded,
            message = message,
            utcTime = DateTime.UtcNow.ToString("o"),
            scenePath = SceneManager.GetActiveScene().path,
            sceneDirty = SceneManager.GetActiveScene().isDirty
        };

        Terrain terrain = UnityEngine.Object.FindObjectOfType<Terrain>();
        if (terrain != null && terrain.terrainData != null)
        {
            TerrainData data = terrain.terrainData;
            Vector3 position = terrain.transform.position;
            response.terrainFound = true;
            response.terrainName = terrain.name;
            response.heightmapResolution = data.heightmapResolution;
            response.alphamapLayers = data.alphamapLayers;
            response.positionX = position.x;
            response.positionY = position.y;
            response.positionZ = position.z;
            response.sizeX = data.size.x;
            response.sizeY = data.size.y;
            response.sizeZ = data.size.z;
            response.topImage = Path.Combine(ArtifactDirectory, "terrain-top.png");
            response.perspectiveImage = Path.Combine(ArtifactDirectory, "terrain-perspective.png");
            response.heightImage = Path.Combine(ArtifactDirectory, "terrain-height.png");
            response.holesTopImage = Path.Combine(ArtifactDirectory, "holes-01-02-top.png");
            response.holesPerspectiveImage = Path.Combine(ArtifactDirectory, "holes-01-02-perspective.png");
        }

        return response;
    }

    private static void CaptureValidationImages()
    {
        Terrain terrain = UnityEngine.Object.FindObjectOfType<Terrain>();
        if (terrain == null || terrain.terrainData == null)
            throw new InvalidOperationException("No Unity Terrain with TerrainData is open.");

        Directory.CreateDirectory(ArtifactDirectory);

        TerrainData data = terrain.terrainData;
        Vector3 origin = terrain.transform.position;
        Vector3 size = data.size;
        Vector3 center = origin + new Vector3(size.x * 0.5f, size.y * 0.35f, size.z * 0.5f);

        RenderCamera(
            origin + new Vector3(size.x * 0.5f, size.y + 500f, size.z * 0.5f),
            Quaternion.Euler(90f, 0f, 0f),
            true,
            Mathf.Max(size.x, size.z) * 0.5f,
            1536,
            1536,
            Path.Combine(ArtifactDirectory, "terrain-top.png"));

        Vector3 perspectivePosition = center + new Vector3(-size.x * 0.62f, size.y + 330f, -size.z * 0.62f);
        Quaternion perspectiveRotation = Quaternion.LookRotation(center - perspectivePosition, Vector3.up);
        RenderCamera(
            perspectivePosition,
            perspectiveRotation,
            false,
            0f,
            1920,
            1080,
            Path.Combine(ArtifactDirectory, "terrain-perspective.png"));

        RenderHeightImage(data, Path.Combine(ArtifactDirectory, "terrain-height.png"));
        Debug.Log("Stonehill validation artifacts written to " + ArtifactDirectory);
    }

    private static void CaptureHoleImages()
    {
        Terrain terrain = UnityEngine.Object.FindObjectOfType<Terrain>();
        if (terrain == null || terrain.terrainData == null)
            throw new InvalidOperationException("No Unity Terrain with TerrainData is open.");

        Directory.CreateDirectory(ArtifactDirectory);
        Vector3 center = new Vector3(680f, 35f, 1190f);
        RenderCamera(
            new Vector3(center.x, 620f, center.z),
            Quaternion.Euler(90f, 0f, 0f),
            true,
            470f,
            1800,
            1200,
            Path.Combine(ArtifactDirectory, "holes-01-02-top.png"));

        Vector3 perspectivePosition = center + new Vector3(-480f, 340f, -520f);
        RenderCamera(
            perspectivePosition,
            Quaternion.LookRotation(center - perspectivePosition, Vector3.up),
            false,
            0f,
            1920,
            1080,
            Path.Combine(ArtifactDirectory, "holes-01-02-perspective.png"));
    }

    private static void CaptureHoleOneRedTeeImage()
    {
        Terrain terrain = UnityEngine.Object.FindObjectOfType<Terrain>();
        if (terrain == null || terrain.terrainData == null)
            throw new InvalidOperationException("No Unity Terrain with TerrainData is open.");

        Directory.CreateDirectory(ArtifactDirectory);

        // Eye-level camera at the exact Hole 1 red tee, looking along the first
        // landing corridor toward the second aim point and the distant green.
        Vector3 cameraPosition = new Vector3(688.7938f, 40.91844f, 1368.8126f);
        Vector3 target = new Vector3(485.232f, 45.0f, 1361.849f);
        RenderCamera(
            cameraPosition,
            Quaternion.LookRotation(target - cameraPosition, Vector3.up),
            false,
            0f,
            1920,
            1080,
            Path.Combine(ArtifactDirectory, "hole-01-red-tee.png"));
    }

    private static void RenderCamera(
        Vector3 position,
        Quaternion rotation,
        bool orthographic,
        float orthographicSize,
        int width,
        int height,
        string outputPath)
    {
        GameObject cameraObject = new GameObject("Stonehill Automation Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        RenderTexture renderTexture = null;
        Texture2D image = null;
        RenderTexture previousActive = RenderTexture.active;

        try
        {
            camera.transform.position = position;
            camera.transform.rotation = rotation;
            camera.orthographic = orthographic;
            camera.orthographicSize = orthographicSize;
            camera.nearClipPlane = 0.5f;
            camera.farClipPlane = 6000f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.58f, 0.76f, 0.86f, 1f);
            camera.allowHDR = false;
            camera.allowMSAA = true;

            renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = renderTexture;
            camera.Render();

            RenderTexture.active = renderTexture;
            image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes(outputPath, image.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previousActive;
            if (camera != null)
                camera.targetTexture = null;
            if (image != null)
                UnityEngine.Object.DestroyImmediate(image);
            if (renderTexture != null)
                UnityEngine.Object.DestroyImmediate(renderTexture);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }

    private static void RenderHeightImage(TerrainData data, string outputPath)
    {
        const int outputSize = 1024;
        Texture2D image = new Texture2D(outputSize, outputSize, TextureFormat.RGB24, false);
        Color[] pixels = new Color[outputSize * outputSize];
        float denominator = outputSize - 1f;

        for (int y = 0; y < outputSize; y++)
        {
            float normalizedY = y / denominator;
            for (int x = 0; x < outputSize; x++)
            {
                float height = data.GetInterpolatedHeight(x / denominator, normalizedY) / data.size.y;
                pixels[y * outputSize + x] = new Color(height, height, height, 1f);
            }
        }

        image.SetPixels(pixels);
        image.Apply();
        File.WriteAllBytes(outputPath, image.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(image);
    }

    private static void WriteResponse(AutomationResponse response)
    {
        Directory.CreateDirectory(ArtifactDirectory);
        File.WriteAllText(ResponsePath, JsonUtility.ToJson(response, true));
        Debug.Log("Stonehill automation: " + response.command + " - " + response.message);
    }
}
