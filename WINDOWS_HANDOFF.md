# Windows handoff for the Stonehill two-hole prototype

The Unity editor and final GSPro build/test steps must be completed on Windows.
The GIS, SVG, and Blender preparation can remain on this Linux computer.

## What to copy

Copy this folder to the Windows computer:

`Unity/Stonehill_Holes_01_02`

The only required top-level Unity project folders are:

- `Assets`
- `Packages`
- `ProjectSettings`

Do not copy `Library`, `Temp`, `Logs`, `obj`, `.vs`, generated `.csproj` files,
or generated `.sln` files. Unity recreates those caches. Omitting `Library` alone
saves approximately 3.9 GB from this checkout.

Also copy `OPCD/GreenKeeper_4.1.0` if GreenKeeper is not already installed on
the Windows computer.

## First Windows opening

1. Install the exact Unity editor version `2018.2.8f1`.
2. Add/open `Stonehill_Holes_01_02` as an existing Unity project.
3. Allow the initial asset import and script compilation to finish. This may
   take a while on the first opening.
4. Open
   `Assets/StonehillCourse/Scenes/Stonehill_Holes_01_02.unity`.
5. Choose `Stonehill > Create Terrain From Prepared RAW`.
6. Save the scene.
7. Do not continue if Unity reports red compilation errors; save the Console
   output or take a screenshot so the project can be corrected first.

## Not yet expected

There is no playable `.course` file yet. The terrain is prepared, but the
production SVG must still be completed and processed through the OPCD Web App,
then conformed in Blender, imported into Unity, configured in GreenKeeper, and
built on Windows.
