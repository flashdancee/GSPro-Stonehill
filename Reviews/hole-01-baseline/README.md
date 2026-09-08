# Hole 1 lake repair

Replaced the one-metre grid lake with a triangulated, rounded shoreline (136 boundary vertices). The water remains at its existing height of 39.00205 Unity metres. Its overall location is retained; sharp tracing corners are rounded into continuous curves.

Lowered the terrain inside the basin and blended the adjoining bank over at most four metres. The pre-lake terrain is retained as `Assets/StonehillCourse/HoleOneDetail/PreLakeTerrain.asset`, so repeated applications use the original baseline rather than progressively reshaping the terrain. Removed grass within the basin and overlapping fairway collision triangles. The water renderer and collider reference the same new mesh. Existing GKD penalty metadata is retained.

Validation checks water planarity, polygon/triangle area agreement, and terrain clearance on a 0.5-metre sampling grid more than 0.8 metres inside the shoreline. Unity renders were reviewed from the tee, shoreline and an elevated viewpoint. In-game shot testing has not been performed.

Source: `../../staging/hole1-lake/StonehillLakeRefinement.cs`, also installed in the Unity project's StonehillCourse/Editor directory.

Rebuild using `StonehillTwoHoleCourseBuilder.BuildSmoothLakePackage`, or the **Stonehill → Hole 1 → Smooth Lake and Capture** menu followed by packaging. After a full terrain/routing rebuild, refresh the pre-lake baseline from that terrain before applying this repair.

Backups of the preceding scene, terrain and installed bundle are in `../backups/lake-20260907`. Review images in this directory are Unity renders.
