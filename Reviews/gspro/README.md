# Actual GSPro inspection — 0.4.0-beta.1

Installed source: `97f8ad3c9c9fd356f61d23043fd125c14b25e1dc`. See [installation receipt](../installation.json) for package, metadata and rollback hashes.

- [Hole 01 — White tee — actual GSPro screenshot](hole-01-white-tee-gspro.png)
- [Hole 01 — approach/green flyover — actual GSPro screenshot](hole-01-approach-flyover-gspro.png)

GSPro 3.1.6.20 loaded the installed course successfully. The log reported 7.7 seconds to load the scene and 0.5 seconds to process it. Rounded lake, turf and woodland rendered without missing-material pink surfaces in the inspected views. Foreground grass clumps remain coarse; distant terrain silhouette and provisional playing outlines need visual refinement. Existing post-processing shader warnings remain in the runtime log. This inspection covers Hole 1; other holes have Unity review captures and await GSPro inspection/playtesting.

Memory at the static Hole 1 tee: working set 3,188,092,928 bytes (2.97 GiB); private bytes 5,327,298,560 (4.96 GiB). This is whole-process memory, including GSPro itself, not incremental course memory. See hole-01-memory.json. Captures are 1440 × 1080 including letterboxing; internal render resolution and quality preset were not independently confirmed.

Frame timing is **pending**. Intel PresentMon 2.5.1 CLI was downloaded from the official GameTechDev/PresentMon release and SHA-256 verified (`9bec3083069f58f911e6a512f4806db51a27bd096103087bc1d05ef54c80a191`). A 20-second capture restricted to GSPro's PID failed because Windows denied permission to start the performance trace. No FPS or frame-time result is claimed. A future authorized administrator-run capture should record median/p95 frame time at tee and tree-heavy views, resolution and graphics settings. Do not infer gameplay performance from Unity capture timings.

No tee shots, hazard/drop behaviour or putting were tested. All holes remain awaiting user playtest. No 60 FPS acceptance requirement applies.
