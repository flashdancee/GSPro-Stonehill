#!/usr/bin/env python3
"""Create true-scale reference vectors for Stonehill holes 1 and 2.

Input geometry comes from an Overpass JSON response in WGS84.  Coordinates are
projected to NAD83 / UTM zone 17N and then placed on the same 2048 m square as
the Unity heightmap.  The SVG is deliberately a reference drawing: OPCD's
exact production colours must be applied from the registered V4 base package.
"""

from __future__ import annotations

import json
import math
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "QGIS/vectors/osm_holes_01_02_raw.json"
GEOJSON_OUT = ROOT / "QGIS/vectors/stonehill_holes_01_02.geojson"
SVG_OUT = ROOT / "Inkscape/stonehill_holes_01_02_reference.svg"
WORKING_SVG_OUT = ROOT / "Inkscape/stonehill_holes_01_02_working.svg"
REPORT_OUT = ROOT / "References/hole_geometry_report.json"

# True-metre terrain extent, NAD83 / UTM zone 17N (EPSG:26917).
X_MIN = 497209.04652911134
Y_MIN = 5140189.3727385933
X_MAX = 499257.04652911134
Y_MAX = 5142237.3727385933
TERRAIN_SIZE = 2048.0

# Local WGS84-to-NAD83 adjustment selected by the Esri projection service at
# the terrain centre.  Across this 2 km plot it is effectively constant and
# keeps OSM vectors aligned with the EPSG:26917 raster to about a metre.
NAD83_X_SHIFT = 0.3212405911
NAD83_Y_SHIFT = -0.9969364319

# OSM ways that belong to the two-hole prototype.
FEATURES = {
    1306285792: ("tee", 1),
    1306285793: ("fairway", 1),
    1306285794: ("green", 1),
    1306285795: ("hole", 1),
    1306285796: ("water", 1),
    1306285797: ("tee", 2),
    1306285798: ("fairway", 2),
    1306285799: ("green", 2),
    1306285800: ("bunker", 2),
    1306285801: ("hole", 2),
}

STYLES = {
    # Official OPCD V4 palette colours from the registered download.
    "water": ("#0000c0", "#0000c0"),
    "fairway": ("#43e561", "#43e561"),
    "green": ("#bce5a4", "#bce5a4"),
    "tee": ("#a0e5b8", "#a0e5b8"),
    "bunker": ("#e5e5aa", "#e5e5aa"),
}


def utm17(lon_deg: float, lat_deg: float) -> tuple[float, float]:
    """Project lon/lat to EPSG:26917 using the GRS80 ellipsoid."""
    a = 6378137.0
    inv_f = 298.257222101
    f = 1.0 / inv_f
    e2 = f * (2.0 - f)
    ep2 = e2 / (1.0 - e2)
    k0 = 0.9996
    lon0 = math.radians(-81.0)
    lat = math.radians(lat_deg)
    lon = math.radians(lon_deg)
    sin_lat = math.sin(lat)
    cos_lat = math.cos(lat)
    tan_lat = math.tan(lat)
    n = a / math.sqrt(1.0 - e2 * sin_lat * sin_lat)
    t = tan_lat * tan_lat
    c = ep2 * cos_lat * cos_lat
    aa = cos_lat * (lon - lon0)
    m = a * (
        (1 - e2 / 4 - 3 * e2**2 / 64 - 5 * e2**3 / 256) * lat
        - (3 * e2 / 8 + 3 * e2**2 / 32 + 45 * e2**3 / 1024) * math.sin(2 * lat)
        + (15 * e2**2 / 256 + 45 * e2**3 / 1024) * math.sin(4 * lat)
        - (35 * e2**3 / 3072) * math.sin(6 * lat)
    )
    x = 500000.0 + k0 * n * (
        aa
        + (1 - t + c) * aa**3 / 6
        + (5 - 18 * t + t**2 + 72 * c - 58 * ep2) * aa**5 / 120
    )
    y = k0 * (
        m
        + n
        * tan_lat
        * (
            aa**2 / 2
            + (5 - t + 9 * c + 4 * c**2) * aa**4 / 24
            + (61 - 58 * t + t**2 + 600 * c - 330 * ep2) * aa**6 / 720
        )
    )
    return x + NAD83_X_SHIFT, y + NAD83_Y_SHIFT


def local_xy(point: dict[str, float]) -> tuple[float, float]:
    east, north = utm17(point["lon"], point["lat"])
    return east - X_MIN, Y_MAX - north


def path_data(points: list[dict[str, float]], close: bool) -> str:
    xy = [local_xy(point) for point in points]
    commands = [f"M {xy[0][0]:.3f},{xy[0][1]:.3f}"]
    commands.extend(f"L {x:.3f},{y:.3f}" for x, y in xy[1:])
    if close:
        commands.append("Z")
    return " ".join(commands)


def line_length_m(points: list[dict[str, float]]) -> float:
    projected = [utm17(point["lon"], point["lat"]) for point in points]
    return sum(
        math.hypot(b[0] - a[0], b[1] - a[1])
        for a, b in zip(projected, projected[1:])
    )


def main() -> None:
    data = json.loads(SOURCE.read_text(encoding="utf-8"))
    selected = [element for element in data["elements"] if element.get("id") in FEATURES]
    if len(selected) != len(FEATURES):
        found = {element["id"] for element in selected}
        raise SystemExit(f"Missing expected OSM ways: {sorted(set(FEATURES) - found)}")

    geo_features = []
    for element in selected:
        kind, hole = FEATURES[element["id"]]
        coordinates = [[point["lon"], point["lat"]] for point in element["geometry"]]
        closed = kind != "hole"
        geometry_type = "Polygon" if closed else "LineString"
        geometry_coordinates = [coordinates] if closed else coordinates
        geo_features.append(
            {
                "type": "Feature",
                "id": f"way/{element['id']}",
                "properties": {
                    **element.get("tags", {}),
                    "prototype_hole": hole,
                    "prototype_feature": kind,
                    "source": "OpenStreetMap contributors",
                },
                "geometry": {"type": geometry_type, "coordinates": geometry_coordinates},
            }
        )

    geojson = {
        "type": "FeatureCollection",
        "name": "Stonehill holes 1 and 2 reference geometry",
        "crs": {"type": "name", "properties": {"name": "urn:ogc:def:crs:OGC:1.3:CRS84"}},
        "features": geo_features,
    }
    GEOJSON_OUT.write_text(json.dumps(geojson, indent=2) + "\n", encoding="utf-8")

    layers: dict[str, list[str]] = {key: [] for key in STYLES}
    centerlines: list[str] = []
    labels: list[str] = []
    report: dict[str, object] = {
        "terrain": {
            "crs": "EPSG:26917",
            "width_m": TERRAIN_SIZE,
            "length_m": TERRAIN_SIZE,
            "extent": [X_MIN, Y_MIN, X_MAX, Y_MAX],
        },
        "holes": {},
    }

    published_yards = {1: 516, 2: 344}
    for element in selected:
        kind, hole = FEATURES[element["id"]]
        if kind == "hole":
            centerlines.append(
                f'<path id="hole-{hole}-centerline" d="{path_data(element["geometry"], False)}"/>'
            )
            start = local_xy(element["geometry"][0])
            labels.append(
                f'<text x="{start[0] + 10:.3f}" y="{start[1] - 10:.3f}">Hole {hole}</text>'
            )
            mapped_m = line_length_m(element["geometry"])
            report["holes"][str(hole)] = {
                "par": int(element.get("tags", {}).get("par", 0)),
                "published_blue_yards": published_yards[hole],
                "published_blue_metres": round(published_yards[hole] * 0.9144, 2),
                "osm_centerline_metres": round(mapped_m, 2),
                "note": "OSM center-line is a routing reference, not a surveyed tee-to-pin measurement.",
            }
        else:
            fill, stroke = STYLES[kind]
            layers[kind].append(
                f'<path id="osm-way-{element["id"]}" d="{path_data(element["geometry"], True)}" '
                f'fill="{fill}" stroke="{stroke}" stroke-width="1.5"/>'
            )

    layer_svg = []
    for kind in ("water", "fairway", "green", "tee", "bunker"):
        content = "\n    ".join(layers[kind])
        layer_svg.append(
            f'<g inkscape:groupmode="layer" inkscape:label="{kind.title()} reference" id="layer-{kind}">\n'
            f'    {content}\n  </g>'
        )
    layer_svg.append(
        '<g inkscape:groupmode="layer" inkscape:label="Hole center-lines reference" '
        'id="layer-centerlines" fill="none" stroke="#ba2d2d" stroke-width="2" '
        'stroke-dasharray="8,6">\n    '
        + "\n    ".join(centerlines)
        + "\n  </g>"
    )
    layer_svg.append(
        '<g inkscape:groupmode="layer" inkscape:label="Labels reference" id="layer-labels" '
        'font-family="sans-serif" font-size="18" font-weight="bold" fill="#202020">\n    '
        + "\n    ".join(labels)
        + "\n  </g>"
    )

    svg = f'''<?xml version="1.0" encoding="UTF-8" standalone="no"?>
<svg xmlns="http://www.w3.org/2000/svg"
     xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape"
     width="2048" height="2048" viewBox="0 0 2048 2048">
  <title>Stonehill Golf Club — holes 1 and 2 true-scale reference</title>
  <desc>One SVG unit equals one metre. EPSG:26917 terrain extent. Feature fills use the official OPCD V4 palette; labels, center-lines, and the incomplete perimeter make this a tracing reference rather than a Clender submission.</desc>
  <g inkscape:groupmode="layer" inkscape:label="Terrain extent reference" id="layer-terrain">
    <rect x="0" y="0" width="2048" height="2048" fill="#eef1e8" stroke="#202020" stroke-width="2"/>
  </g>
  {chr(10).join(layer_svg)}
</svg>
'''
    SVG_OUT.write_text(svg, encoding="utf-8")
    working_svg = svg.replace(
        "Stonehill Golf Club — holes 1 and 2 true-scale reference",
        "Stonehill Golf Club — holes 1 and 2 imagery tracing workspace",
    ).replace(
        "One SVG unit equals one metre. EPSG:26917 terrain extent. Feature fills use the official OPCD V4 palette; labels, center-lines, and the incomplete perimeter make this a tracing reference rather than a Clender submission.",
        "One SVG unit equals one metre. The linked Ontario COOP 2021 orthophoto is an alignment and tracing aid and must be removed before OPCD submission.",
    ).replace(
        '''<g inkscape:groupmode="layer" inkscape:label="Terrain extent reference" id="layer-terrain">
    <rect x="0" y="0" width="2048" height="2048" fill="#eef1e8" stroke="#202020" stroke-width="2"/>
  </g>''',
        '''<g inkscape:groupmode="layer" inkscape:label="Ontario COOP 2021 imagery — REMOVE BEFORE SUBMISSION" id="layer-imagery" inkscape:locked="true">
    <image href="../QGIS/imagery_raw/stonehill_coop2021_2048.jpg" x="0" y="0" width="2048" height="2048" preserveAspectRatio="none"/>
  </g>''',
    )
    WORKING_SVG_OUT.write_text(working_svg, encoding="utf-8")
    REPORT_OUT.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
