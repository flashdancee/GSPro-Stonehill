# Version control

This repository stores the Stonehill-owned course overlay and its reproducible
source data. The complete OPCD BaseProject and installed authoring applications
remain local because they are large and may have separate redistribution terms.

## Tracked source

- Stonehill Unity scripts, terrain, scenes, and generated course-shape assets
- Unity `Packages` and `ProjectSettings`
- QGIS, Inkscape, routing, reference, and automation sources
- Plain-text GSPro/GreenKeeper metadata and preview artwork
- The user-annotated front-nine panorama (stored with Git LFS)

## Deliberately untracked

- Unity `Library`, `Temp`, `Logs`, IDE files, and transferred cache backups
- OPCD packages, the full BaseProject asset library, GreenKeeper, and installers
- Screenshots, encrypted `.gspcrse` builds, and other generated outputs

Git LFS stores large terrain, GIS, model, and binary Unity source files. A fresh
working copy requires the matching OPCD V4 BaseProject before opening the Unity
project. Copy the tracked `Assets/StonehillCourse` overlay into that BaseProject,
then open it with Unity 2018.2.8f1.
