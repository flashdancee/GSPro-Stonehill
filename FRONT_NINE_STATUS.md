# Front-nine status

Photo GPS and orthophoto now anchor the front-nine geometry; Holes 3, 6 and 9 retain inferred green targets. Every hole awaits user GSPro shot and putting tests.

| Hole | Turf / trees | Water | Images | GitHub | Playtest |
|---|---|---|---|---|---|
| 1 | Technical pass | Smoothed; metadata aligned | Captured | codex/hole-01-baseline | Awaiting user |
| 2 | Technical pass | Smoothed; metadata aligned | Captured | codex/hole-02-refinement | Awaiting user |
| 3 | Technical pass | None mapped | Captured | codex/hole-03-refinement | Awaiting user |
| 4 | Technical pass | Smoothed; metadata aligned | Captured | codex/hole-04-refinement | Awaiting user |
| 5 | Technical pass | Smoothed; metadata aligned | Captured | codex/hole-05-refinement | Awaiting user |
| 6 | Technical pass | Smoothed; metadata aligned | Captured | codex/hole-06-refinement | Awaiting user |
| 7 | Technical pass | Smoothed; metadata aligned | Captured | codex/hole-07-refinement | Awaiting user |
| 8 | Technical pass | None mapped | Captured | codex/hole-08-refinement | Awaiting user |
| 9 | Technical pass | Smoothed; metadata aligned | Captured | codex/hole-09-refinement | Awaiting user |

## Integration checkpoint

Complete review: [PR #11](https://github.com/flashdancee/GSPro-Stonehill/pull/11), left unmerged for user review. Per-hole branches preserve incremental history; the Hole 6 PR is a historical draft and must not be deployed independently of subsequent fixes.

- Two full source rebuilds and repeated refinement passed; final terrain/collider hashes also match the incremental Hole 9 checkpoint.
- Nine enabled holes, two selectable Red/Yellow tees, four pins per hole, aiming data and seven exact Unity/GKD water boundaries verified.
- Review build 0.4.0-beta.1 installed with matching SHA-256 and paired rollback backup.
- Hole 1 inspected in actual GSPro; other holes await installed-runtime inspection and all holes await user shots/hazards/putting.
- Runtime memory recorded. Frame-time capture pending Windows performance-tracing privileges.
- [Source evidence and scorecard mapping](References/SOURCES.md); [labelled images by hole](Reviews/README.md).
- Photo follow-ups: precise green/fairway outlines (especially holes 3–9), bunkers, paths, distinctive rocks/trees, steep water banks and the adjusted Hole 6 upper/Hole 7 pond traces.

2026-09-12: Blue/Red naming approved by the user; metadata-only update 0.4.0-beta.2. [All 72 routed yardage combinations pass 5% tolerance](Reviews/yardage-audit.md).

2026-09-27: Field-photo reconstruction on `codex/field-photo-reconstruction`; see [photo evidence](References/FIELD_PHOTOS.md) and [current yardage audit](Reviews/field-yardage-audit.md). The two physical tee positions now show colocated Black/Red men's and Yellow women's markers. Eight photographed hole signs supersede scorecard distances; Hole 4 still uses card values. The 2026-09-12 Blue/Red note above is historical.

2026-09-30: GSPro selector reduced to exactly Red and Yellow; Black remains a photographed marker at the shared men's position, not a separate selectable tee. Field-photo bundle installed for user testing as 0.5.0-beta.2.
