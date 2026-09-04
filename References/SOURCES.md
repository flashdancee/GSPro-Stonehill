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

## Scorecard reference

- Stonehill hole 1: par 5, published blue distance 516 yd
- Stonehill hole 2: par 4, published blue distance 344 yd
- Reference: Golfify / 18Birdies; confirm against a current physical club
  scorecard before GreenKeeper configuration.

## Workflow guide

- The Perfect Lie, “Building GSPro Courses — Complete Guide,” updated 2026
- Between Two Biomes GSPro Golf Course Design tutorials
- OPCD V4 workflow: LiDAR → Inkscape → Blender → Unity → GreenKeeper
