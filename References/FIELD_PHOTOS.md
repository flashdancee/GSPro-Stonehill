# September 27, 2026 field photographs

The user supplied 45 named JPGs in `Source-Pictures/`. The [photo register](field-photo-register.csv) records every filename, SHA-256, EXIF time, GPS position and terrain-relative coordinate. Coordinates were converted from WGS84 EXIF to UTM zone 17N, then to the 2,048 m terrain: `x = easting - 497209.046529`, `image_y = 5142237.372739 - northing`. Handheld camera positions are approximate and are **not** green centres, tee markers or surveyed boundaries. The [photo point map](../Reviews/field-photo-map.jpg) overlays camera locations on the registered 2021 orthophoto.

The on-course markers in the photographs are **Black and Red together for men, Yellow for women**. There are two distinct playing positions. The 2025 scorecard's Blue/Red names and longer distances do not reflect the installed markers. Where readable, the photographed hole signs now control displayed yardage. Hole 4 has no sign photograph, so its scorecard distances remain provisional.

| Hole | Men yd | Women yd | Field control and course appearance |
|---|---:|---:|---|
| 1 | 446 | 388 | Men near `(755,705)`, Yellow near `(700,675)`, green camera near `(357,786)`. Left water, broad tree line and exposed rock on the fairway side; a gravel track enters near the tee. |
| 2 | 308 | 272 | Men near `(490,763)`, Yellow near `(518,776)`, green cameras near `(765,841)`. Tee looks uphill over a gravel crossing; green has a close tree backdrop. |
| 3 | 271 | 232 | Men near `(755,854)`, Yellow near `(723,844)`. The sign, tee photos and orthophoto put the green near `(510,854)`; there is no green photograph. Narrow tree-lined corridor and gravel to the tee's right. |
| 4 | 318* | 293* | Green camera near `(763,875)`. The camera looks west along a narrow fairway with a gravel track on its north side; tee positions near `(470,900)` and `(495,897)` are inferred. |
| 5 | 144 | 111 | Green camera near `(900,933)`; the photographed tee/sign camera is near `(778,857)`. Short downhill hole over a gravel crossing with surrounding shrubs. The Yellow marker is inferred farther forward from the sign distance. |
| 6 | 132 | 95 | Men camera near `(936,952)`; yellow area inferred near `(917,989)`. Green near `(838,1025)` is inferred from 2021 turf, tee view and sign distance. Pond and exposed rock sit between tee and green. |
| 7 | 227 | 176 | Men/sign near `(732,1086)`, Yellow camera near `(801,1101)`, green camera near `(931,1087)`. Play goes east from the pond, around rock and a dogleg; gravel borders the pond. |
| 8 | 128 | 96 | Men/sign near `(1018,1130)`, Yellow camera near `(995,1104)` and tee inferred 5 m forward, green camera near `(1015,1010)`. Short uphill hole with a gravel track to the right and trees around the green. |
| 9 | 300 | 288 | Men/sign near `(976,950)`; the two green-labelled cameras are near `(865,666)`. The playable target near `(907,688)` is provisional: it reconciles the sign length, 2021 turf and nearby camera positions. The women's marker is inferred close to the men's. |

`*` Hole 4 uses the scorecard because no current sign is photographed. All other figures are transcribed from the pictured signs; the signs may themselves be approximate. Route and turf centres inferred from the photos remain subject to an on-course GSPro playtest, especially Holes 3, 6 and 9. The previous geometry and release remain in Git history.

Visual refinements use the imagery as follows: the camera points reset the route, tee and green anchors; the orthophoto traces the cart track and rock/woodland regions; the ground views control the gravel, dense deciduous margins and open playable corridors. Cart tracks are visual terrain material only and retain the underlying GSPro ball physics. The photographs include people and their original EXIF GPS; preserve the source JPGs in Git LFS for review.
