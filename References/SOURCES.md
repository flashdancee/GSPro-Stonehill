# Data and workflow sources

## Terrain

- Ontario Digital Terrain Model (LiDAR-Derived), Sudbury 2023–24 project
- Service: `Elevation/Ontario_DTM_LidarDerived/ImageServer`
- Licence/source authority: Geospatial Ontario, Government of Ontario
- Crop CRS: NAD83 / UTM zone 17N (`EPSG:26917`)
- Crop extent: `497209.046529, 5140189.372739, 499257.046529, 5142237.372739`
- Raster size: 2049 × 2049 pixels; 1 m sample spacing across 2048 m
- Retrieved: 2026-09-01
- Vector projection uses a local WGS84-to-NAD83 adjustment resolved at the
  terrain centre through Esri's Geometry Service.

## Course vectors

- OpenStreetMap contributors, ODbL 1.0
- Retrieved through Overpass API: 2026-09-01
- Saved response: `QGIS/vectors/osm_holes_01_02_raw.json`
- Prototype source ways: `1306285792` through `1306285801`

OpenStreetMap vectors must retain appropriate attribution if redistributed.

## Orthophoto

- Central Ontario Orthophotography Project (COOP) 2021 through the Ontario
  Imagery Data Service (`GEO_Imagery_Data_Service_2018to2022`)
- Licence: Open Government Licence – Ontario
- Capture period: 2021-04-02 through 2021-06-01; leaf-off/low-leaf
- Native product resolution: 20 cm; service export used here: 1 m/pixel
- Aligned export: `QGIS/imagery_raw/stonehill_coop2021_2048.jpg`
- Native-detail holes 1–2 crop:
  `QGIS/imagery_raw/stonehill_holes_01_02_coop2021_20cm.jpg`
- Export CRS and extent match the terrain: `EPSG:26917`, 2048 × 2048 m
- Retrieved: 2026-09-01

### Native-resolution reference supplied September 2026

- Image: `stone-hill-COOP2021-orthomosaic.jpg`
- World file: `stone-hill-COOP2021-orthomosaic.jgw`
- Projection: `stone-hill-COOP2021-orthomosaic.prj`
- Raster size: 15,000 × 15,000 pixels
- Native ground sampling: 0.20 m/pixel; 3,000 × 3,000 m coverage
- Extent from the world file pixel edges: `497000, 5140000, 500000, 5143000`
- Declared CRS: NAD83(CSRS) / UTM zone 17N (`EPSG:2958`)
- The existing 2,048 m Unity terrain occupies pixel columns approximately
  `1045–11285` and rows `3813–14053` in this raster.
- Use this native raster to trace fairway/green boundaries, shorelines, cart
  paths, tree lines, and exposed Canadian Shield shelves. Use the LiDAR DTM for
  elevation and slope; imagery alone cannot establish rock height or collision.
- The CSRS/non-CSRS UTM distinction is sub-metre at this site. The image aligns
  visually with the existing `EPSG:26917` terrain crop, but preserve both source
  CRS declarations rather than silently relabelling either dataset.

## Supplied scorecard reference

- [Front-nine yardages and course map](Stonehill-Golf-Card_2025-back.jpg)
- [Back-nine yardages and clubhouse information](Stonehill-Golf-Card_2025-front.jpg)
- Original supplied filenames include 2025; the map artwork is printed 2024. Edition is unconfirmed.
- The card's Blue row supplies the current GSPro White distances: 471, 344, 287, 318, 156, 151, 290, 161, 310 yards (2,488 total).
- Red: 446, 308, 271, 293, 144, 132, 227, 128, 300 yards (2,249 total).
- Front-nine pars: 5, 4, 4, 4, 3, 3, 4, 3, 4 (34 total).
- Blue-to-White is an existing simulator label mapping, not a claim that the card shows White tees. Preserve distances; review tee naming separately with the user.
- These supplied images supersede the older third-party Hole 1/2 scorecard notes. Do not substitute previously quoted Hole 1 distance 516 yd.

## Source usage and conflict resolution

| Source | Controls | Limits |
|---|---|---|
| Native COOP orthomosaic with JGW/PRJ | Turf edges, shorelines, woodland, paths and exposed rock | Leaf-off 2021 imagery; small/distinctive features need photos |
| GIS LiDAR DTM and derived RAW | Ground elevation, scale and slope | Sampling may not resolve banks or green micro-contours |
| [User annotated panorama](../highlighted-stone-hill-panorama-full-transparent.png) | Hole numbers, routing, tee regions, rough/fairway intent and water identification | Annotations guide tracing; they are not surveyed geometry |
| Supplied scorecard images above | Par and tee yardages; schematic routing cross-check | Map is schematic; tee colour mapping and edition recorded above |
| Future dated user photographs | Current vegetation, precise feature appearance, bunkers, paths and distinctive trees | Record hole, viewpoint and direction; reconcile spatial placement against GIS |

For each hole compare the annotated routing with the georeferenced orthomosaic and terrain before tracing. Check scorecard distances along the playing route, not solely straight-line tee-to-pin distance. Preserve original sources and record any correction, evidence and affected holes in the review notes. Conflicts remain visible; do not silently move tees or change yardages to force a fit. Existing approximate fairways/greens on holes 3–9 remain provisional.

## Workflow guide

- The Perfect Lie, “Building GSPro Courses — Complete Guide,” updated 2026
- Between Two Biomes GSPro Golf Course Design tutorials
- OPCD V4 workflow: LiDAR → Inkscape → Blender → Unity → GreenKeeper

## Tee naming decision — 2026-09-12
The user confirmed Blue/Red labels. Version 0.4.0-beta.2 exports Blue instead of White with identical positions and distances. Earlier White-labelled captures remain historical. See [yardage audit](../Reviews/yardage-audit.md).
