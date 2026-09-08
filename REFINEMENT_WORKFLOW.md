# Stonehill front-nine refinement

## Authoritative files and prerequisites

Work in `C:/GSPro-Courses/GSPro-Stonehill`, using Unity 2018.2.8f1 and the matching OPCD V4 BaseProject. Other staging copies are historical references. Do not upgrade Unity. Restore Git LFS objects before opening a fresh checkout. The licensed BaseProject stays local; overlay the tracked StonehillCourse assets, Packages and ProjectSettings onto it.

`Assets/StonehillCourse/Refinement/RefinementSettings.json` records enabled holes, deterministic tree seeds, spacing, height range, woodland radius and camera eye height. Woodland is limited to existing orthomosaic zones, with playing-surface, tee, water and rock exclusions. Placement is provisional pending photos.

## Build and review

1. Start from the last reviewed branch, preserving any unrelated changes. Create `codex/hole-NN-refinement`. During initial rollout these are stacked branches; each PR targets its preceding checkpoint, and merges wait for review.
2. In Unity choose **Stonehill > Refinement > Hole N**. The command enables that hole, reapplies the enabled union, validates, saves and captures. Existing Hole 1 menu commands remain available.
3. **Stonehill > Build Front-Nine Playable Course** rebuilds the base routing from RAW, reapplies Hole 1, refreshes the pre-water baseline, then restores every enabled refinement. Never copy a stale baseline onto the whole live terrain.
4. Run `StonehillTwoHoleCourseBuilder.ValidateRepeatability` through Unity `-executeMethod`. Use the complete rebuild regression check before the final front-nine checkpoint.
5. Inspect `Reviews/hole-NN/after-tee.png`, `after-approach.png`, green and hazard views. Show at least two images directly in chat after each hole update. Label these **Unity renders**; label actual GSPro captures separately.
6. Commit source, settings, `.meta` files, required generated assets, selected images and validation records. Push and open a PR with known limitations. Do not merge before user review.
7. Package using **Stonehill > Package Front-Nine Course Bundle**. Synchronize hazard polygons from `Reviews/water-boundaries.json` using `Scripts/update-refined-hazards.ps1`; do not retain older traced penalty outlines after smoothing.
8. Before installation, back up the existing package and GKD together. Record source commit, package/GKD SHA-256, version, timestamp and backup paths. Update CourseRelease.json and CHANGELOG.md. Install the pair and verify both hashes.

Unity batch commands require the editor to be closed. Use `-batchmode -quit -projectPath <project> -executeMethod <method> -logFile <log>` with rendering enabled. A second editor must never open the same project concurrently.

## Acceptance

Technical validation covers water planarity and triangulation, half-metre interior terrain clearance, bounded terrain changes, visual-pass physics preservation, marker counts and deterministic reapplication. Review shore edges visually as well as checking interior samples. Validate GKD has nine enabled holes, two enabled tees per hole and four pins, and retains existing aiming data.

Playtest status stays **awaiting user playtest** until the user confirms tee shots, hazard entry/drop behaviour and putting. Prioritize visual quality; record actual GSPro frame time, resolution/settings and memory without claiming Unity capture timing is gameplay performance.

## Recovery

For an installed regression restore the saved bundle and GKD pair and reload the course in GSPro. For authoring recovery use the reviewed Git branch with LFS assets restored, then rebuild with its matching settings. Preserve the September 8 pre-refinement backup until the front nine is accepted. Avoid deleting caches or resetting uncommitted files as a routine repair.

## Photos requested later

For each hole: red and white tee views toward the landing area; landing-area view forward and back; approach; green from the front and looking back; hazards from both sides; distinctive trees, rock faces, bunkers, paths and elevation transitions. Label by hole, viewpoint and date. Note approximate camera location/direction when possible. Record uncertain shapes as provisional rather than inventing survey precision.

## Scope

Front nine first. Existing yardages/routing remain the baseline; evidence-backed geometric corrections receive separate notes. Back-nine construction follows front-nine acceptance.

## Validated release commands

Run `StonehillTwoHoleCourseBuilder.ValidateFullRebuild` to compare two complete source rebuilds and then repeated refinement. Run `python Scripts/validate-metadata.py <repository>` after synchronizing water boundaries. Legacy enabled flags on empty tee placeholders are preserved; only White and Red have playable positions and distances.

After committing the source used to build, run `Scripts/install-refined-course.ps1 -BundlePath <built bundle> -BackupDirectory <new unique directory>`. The script saves the installed bundle/GKD/release manifest together, verifies installation hashes and writes `Reviews/installation.json`. Keep encrypted packages and rollback packages outside Git. Back up the GSPro database before using the existing metadata-sync script.

The source traces for Hole 6 upper pond and Hole 7 pond overlap existing playing-surface envelopes. Refinement trims those boundaries to preserve original tees and greens, and exports the same adjusted boundaries to GKD. These traces and remaining steep banks require photo review before visual acceptance.
