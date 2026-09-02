using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Creates the true-scale Stonehill terrain from the prepared Ontario DTM RAW.
/// Intended for the OPCD BaseProject under Unity 2018.2.8f1.
/// </summary>
public static class StonehillTerrainBuilder
{
    private const int Resolution = 2049;
    private const float TerrainWidthMetres = 2048f;
    private const float TerrainHeightMetres = 80f;
    private const string RawAssetPath =
        "Assets/StonehillCourse/SourceData/stonehill_heightmap_2049_le.raw";
    private const string TerrainAssetPath =
        "Assets/StonehillCourse/StonehillTerrainData.asset";

    [MenuItem("Stonehill/Create Terrain From Prepared RAW")]
    public static void CreateTerrain()
    {
        if (AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainAssetPath) != null)
        {
            EditorUtility.DisplayDialog(
                "Stonehill terrain already exists",
                "StonehillTerrainData.asset already exists. No files were changed.",
                "OK");
            return;
        }

        Terrain existingTerrain = Object.FindObjectOfType<Terrain>();
        if (existingTerrain != null)
        {
            EditorUtility.DisplayDialog(
                "Terrain already in scene",
                "Remove or rename the existing terrain before creating Stonehill terrain.",
                "OK");
            return;
        }

        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string rawFile = Path.Combine(projectRoot, RawAssetPath);
        if (!File.Exists(rawFile))
        {
            EditorUtility.DisplayDialog("Missing Stonehill RAW", rawFile, "OK");
            return;
        }

        byte[] bytes = File.ReadAllBytes(rawFile);
        int expectedLength = Resolution * Resolution * 2;
        if (bytes.Length != expectedLength)
        {
            EditorUtility.DisplayDialog(
                "Unexpected heightmap size",
                "Expected " + expectedLength + " bytes but found " + bytes.Length + ".",
                "OK");
            return;
        }

        float[,] heights = new float[Resolution, Resolution];
        for (int unityRow = 0; unityRow < Resolution; unityRow++)
        {
            // Ontario GeoTIFF/RAW rows run north-to-south. Unity terrain rows
            // run south-to-north, so reverse the rows during import.
            int sourceRow = Resolution - 1 - unityRow;
            for (int column = 0; column < Resolution; column++)
            {
                int offset = (sourceRow * Resolution + column) * 2;
                ushort value = (ushort)(bytes[offset] | (bytes[offset + 1] << 8));
                heights[unityRow, column] = value / 65535f;
            }
        }

        TerrainData terrainData = new TerrainData();
        terrainData.heightmapResolution = Resolution;
        terrainData.size = new Vector3(
            TerrainWidthMetres,
            TerrainHeightMetres,
            TerrainWidthMetres);
        terrainData.SetHeights(0, 0, heights);
        AssetDatabase.CreateAsset(terrainData, TerrainAssetPath);

        GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
        terrainObject.name = "Stonehill Terrain (DTM 2023-24)";
        terrainObject.transform.position = Vector3.zero;
        Selection.activeGameObject = terrainObject;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log(
            "Stonehill terrain created: 2048m x 2048m, 80m vertical range, " +
            "RAW zero represents 240m above sea level.");
    }
}
