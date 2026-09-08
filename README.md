# Stonehill GSPro course

> **Current checkpoint (September 2026):** The front-nine beta has been rebuilt
> from the annotated Stonehill panorama and installed in GSPro. It includes nine
> enabled holes, official 2025 par and yardages, correctly spaced white/red tees,
> four pins per hole, aim points, seven mapped penalty-water areas, and physically
> smoothed greens and tees. The routing is substantially improved but remains a
> beta pending on-course photographs and playtesting.

The currently installed course release is `0.3.0-beta.1` (updated 2026-09-04).
Release identity is stored in `CourseRelease.json`; release notes are in
`CHANGELOG.md`. The packaging script embeds that manifest in its output, and
`Scripts/sync-gspro-course-metadata.ps1` populates the Version and Date Updated
fields that GSPro leaves blank for sideloaded courses.

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
- The native 15,000 × 15,000 COOP 2021 orthomosaic, world file, and projection
  definition are retained as the authoritative 20 cm visual reference. They make
  maintained turf, shorelines, paths, tree lines, and exposed bedrock traceable.
- `highlighted-stone-hill-panorama-full-transparent.png` identifies the front-nine
  routing, hole numbers, tee areas, and water features supplied by the course user.
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

## Current validation checkpoint

See `WINDOWS_HANDOFF.md` for the exact files and first-opening procedure. A new
Codex session should begin with the prompt in `CONTINUE_ON_WINDOWS.md`.

1. Play all nine holes from both white and red tees in GSPro.
2. Verify the seven mapped water penalties, aiming, putting, and gimme behavior.
3. Photograph each hole from its tees, landing area, approach, green, and hazards.
4. Refine fairway/rough edges, bunker outlines, cart paths, vegetation, and rock
   placement from the native orthomosaic, then validate their appearance against
   on-course photographs.
5. Extend the same annotated, deterministic workflow to holes 10–18.

## Accuracy status

The terrain source is authoritative LiDAR-derived elevation. Front-nine routing
now follows the supplied annotated panorama, while feature edges remain visual
interpretations rather than survey data. Before release, verify green edges,
tee decks, bunker outlines, water levels, cart paths, vegetation, and pin areas
using on-site photographs and playtesting.
