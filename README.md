# Stonehill GSPro course

> **Current checkpoint (September 2026):** A playable front-nine beta is built
> and installed in GSPro. It includes nine enabled holes, official 2025 par and
> yardages, white/red tees, four pins per hole, aim points, initial penalty-water
> areas, and a first terrain/vegetation/material pass. Holes 3–9 remain GIS-first
> approximations pending on-course photographs and playtesting.

This project is a true-scale reconstruction of Stonehill Golf Club in Sudbury,
Ontario. The terrain extent covers the entire course, and the current playable
checkpoint contains the front nine.

## Current state

- Ontario 2023–24 LiDAR-derived DTM downloaded and cropped to a 2,048 m square.
- Terrain is in NAD83 / UTM zone 17N (`EPSG:26917`), where units are real metres.
- A Unity-compatible 2,049 × 2,049, little-endian 16-bit RAW heightmap exists.
- OSM reference geometry for holes 1 and 2 has been extracted to GeoJSON.
- A true-scale, layered Inkscape SVG reference has been generated.
- An openly licensed Ontario COOP 2021 orthophoto has been aligned at exactly
  one image pixel per terrain metre for spline tracing.
- OPCD V4 BaseProject dated 2025-12-02 is unpacked as the Stonehill Unity project.
- GreenKeeper 4.1.0 and the current Blender extension installer are available.
- Blender 4.2.2 LTS is installed locally with OPCD Tools 3.5.8, Edit Mesh
  Tools, and LoopTools enabled in a project-local configuration.
- Inkscape 1.4.2 is installed locally with the official OPCD V4 palette.

## Required application versions

Use the versions embedded in the downloaded OPCD packages. They supersede the
older public guide where the two disagree:

- Unity `2018.2.8f1` (declared by `ProjectSettings/ProjectVersion.txt`)
- Blender `4.2.2+` (enforced by `opcd_installer_1_0_2.py`)
- Inkscape `1.2+`
- QGIS 3.x
- GreenKeeper `4.1.0`

## Terrain import settings

Use `QGIS/exports/stonehill_heightmap_2049_le.raw` in Unity with:

- Depth: 16 bit
- Byte order: Windows / little-endian
- Resolution: 2049 × 2049
- Terrain width: 2048 m
- Terrain length: 2048 m
- Terrain height: 80 m
- Elevation base represented by RAW zero: 240 m
- Flip vertically: verify against the reference overlay before committing

The source DTM ranges from approximately 247.53 m to 315.95 m. The 240–320 m
encoding leaves safe headroom while preserving centimetre-scale quantization.

## Important SVG note

`Inkscape/stonehill_holes_01_02_working.svg` is the imagery-backed tracing
workspace. `Inkscape/stonehill_holes_01_02_reference.svg` is its vector-only
baseline. Neither file is yet a Clender-ready production spline. The feature
fills use the exact registered OPCD V4 palette, but reference labels and
center-lines remain and the required rough/perimeter/cart-path work is not
complete. Remove reference-only content and the imagery layer before submission.

## Regenerate reference files

From this directory:

```bash
python3 Scripts/build_reference.py
```

This creates the processed GeoJSON, vector reference SVG, imagery-backed working
SVG, and geometry report from the saved Overpass response.

## Launch Blender with OPCD tools

On this Linux computer, run:

```bash
./Tools/run-blender-opcd.sh
```

This launcher uses the exact Blender 4.2.2 LTS installation and the isolated
OPCD configuration under `Tools/blender-config`. Do not start this copy of
Blender directly if you want the OPCD extensions to be available.

## Launch Inkscape with the OPCD palette

On this Linux computer, run:

```bash
./Tools/run-inkscape-opcd.sh Inkscape/stonehill_holes_01_02_working.svg
```

The OPCD V4 palette is stored in the isolated Inkscape profile used by this
launcher. The AppImage uses extract-and-run mode because FUSE is unavailable
on this system; the first window can therefore take a little longer to appear.

## Next build checkpoint

See `WINDOWS_HANDOFF.md` for the exact files and first-opening procedure. A new
Codex session should begin with the prompt in `CONTINUE_ON_WINDOWS.md`.

1. Install and activate Unity 2018.2.8f1 on the Windows authoring computer.
2. Finish and validate the two-hole SVG spline in the local Inkscape setup.
3. Transfer the prepared Unity project to the Windows authoring computer.
4. Open `Unity/Stonehill_Holes_01_02` and create the prepared RAW terrain using
   the included `Stonehill` editor menu.
5. Verify the SVG against licensed aerial imagery, and
   complete rough, perimeter, cart-path, and other missing spline layers.
6. Submit the two-hole SVG through the OPCD Web App, conform the result in Blender, and import
   the FBX into the OPCD Unity project.
7. Configure holes 1 and 2 in GreenKeeper and build the first `.course` file.

## Accuracy status

The terrain source is authoritative LiDAR-derived elevation. The golf shapes
are community-mapped reference data, not survey data. Before release, verify
green edges, tee decks, bunker outlines, water levels, cart paths, vegetation,
and pin areas using current licensed imagery and on-site photographs.
