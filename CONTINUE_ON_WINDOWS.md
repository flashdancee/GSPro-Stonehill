# Continue the Stonehill project on Windows

## Prompt for a new Codex chat

Open the transferred `GSPro-Stonehill` folder as the local workspace, then send:

> Continue the Stonehill GSPro two-hole prototype. Read README.md,
> WINDOWS_HANDOFF.md, References/SOURCES.md, and
> Unity/Stonehill_Holes_01_02/Assets/StonehillCourse/README.md before making
> changes. I am new to Unity, so guide me one checkpoint at a time, inspect
> screenshots and Console errors, and do not assume I know where Unity controls
> are. First verify Unity 2018.2.8f1, then open the prepared scene and create the
> terrain using Stonehill > Create Terrain From Prepared RAW. Stop if Unity has
> red compilation errors.

## Current state

- Scope: Stonehill Golf Club, Sudbury, Ontario; holes 1 and 2 only.
- Unity BaseProject: OPCD V4 dated 2025-12-02.
- Required Unity version: `2018.2.8f1`.
- GreenKeeper: `4.1.0`.
- Prepared terrain: 2048 × 2048 m with a 2049 × 2049 RAW heightmap.
- Prepared Unity scene:
  `Assets/StonehillCourse/Scenes/Stonehill_Holes_01_02.unity`.
- Prepared terrain command: `Stonehill > Create Terrain From Prepared RAW`.
- The imagery-backed SVG is a tracing workspace, not an OPCD submission.
- There is no playable `.course` build yet.

## First checkpoint

The first Windows goal is only to open the project cleanly and create the
prepared terrain. Do not import processed golf meshes, configure GreenKeeper,
or attempt a GSPro build until that checkpoint has been visually verified.
