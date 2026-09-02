# Stonehill Unity assets

Open this project only with Unity 2018.2.8f1.

Initial terrain setup:

1. Open `Assets/StonehillCourse/Scenes/Stonehill_Holes_01_02.unity`.
2. Wait until Unity finishes importing/compiling the BaseProject.
3. Choose `Stonehill > Create Terrain From Prepared RAW`.
4. Save the scene.
5. Compare the terrain against the two reference PNGs in `SourceData`.

The importer creates a 2048 × 2048 m terrain with a 2049-sample heightmap and
an 80 m Unity vertical range. RAW zero corresponds to geodetic elevation 240 m,
but the Unity terrain is intentionally positioned at scene Y=0.

Do not submit `stonehill_holes_01_02_map.png` as an OPCD spline. It contains
reference labels and center-lines and is provided only for alignment checking.
