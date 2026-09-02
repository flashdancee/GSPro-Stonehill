using UnityEditor;
using UnityEngine;

/// <summary>
/// Applies or removes the georeferenced Stonehill orthophoto as a single
/// terrain layer. This is an alignment aid only, not a production texture.
/// </summary>
public static class StonehillTerrainReferenceOverlay
{
    private const string AerialTexturePath =
        "Assets/StonehillCourse/SourceData/stonehill_coop2021_2048.jpg";

    [MenuItem("Stonehill/Apply Aerial Reference Overlay")]
    public static void ApplyAerialReferenceOverlay()
    {
        Terrain terrain = Object.FindObjectOfType<Terrain>();
        if (terrain == null || terrain.terrainData == null)
        {
            EditorUtility.DisplayDialog(
                "Stonehill terrain not found",
                "Create the prepared Stonehill terrain before applying the reference overlay.",
                "OK");
            return;
        }

        Texture2D aerial = AssetDatabase.LoadAssetAtPath<Texture2D>(AerialTexturePath);
        if (aerial == null)
        {
            EditorUtility.DisplayDialog(
                "Stonehill aerial image not found",
                AerialTexturePath,
                "OK");
            return;
        }

        TerrainData terrainData = terrain.terrainData;
        SplatPrototype referenceLayer = new SplatPrototype
        {
            texture = aerial,
            tileSize = new Vector2(terrainData.size.x, terrainData.size.z),
            tileOffset = Vector2.zero,
            metallic = 0f,
            smoothness = 0f
        };

        terrainData.splatPrototypes = new[] { referenceLayer };

        float[,,] weights = new float[
            terrainData.alphamapHeight,
            terrainData.alphamapWidth,
            1];
        for (int y = 0; y < terrainData.alphamapHeight; y++)
        {
            for (int x = 0; x < terrainData.alphamapWidth; x++)
            {
                weights[y, x, 0] = 1f;
            }
        }

        terrainData.SetAlphamaps(0, 0, weights);
        EditorUtility.SetDirty(terrainData);
        AssetDatabase.SaveAssets();

        Debug.Log(
            "Stonehill aerial reference overlay applied across the 2048m terrain. " +
            "Use Stonehill > Remove Aerial Reference Overlay before production texturing.");
    }

    [MenuItem("Stonehill/Remove Aerial Reference Overlay")]
    public static void RemoveAerialReferenceOverlay()
    {
        Terrain terrain = Object.FindObjectOfType<Terrain>();
        if (terrain == null || terrain.terrainData == null)
        {
            EditorUtility.DisplayDialog(
                "Stonehill terrain not found",
                "No terrain reference overlay was changed.",
                "OK");
            return;
        }

        terrain.terrainData.splatPrototypes = new SplatPrototype[0];
        EditorUtility.SetDirty(terrain.terrainData);
        AssetDatabase.SaveAssets();
        Debug.Log("Stonehill aerial reference overlay removed.");
    }
}
