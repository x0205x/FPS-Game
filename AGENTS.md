# FPS Game — Agent Guide

## Speed: use parallel subagents

For any task with **2+ independent steps**, launch multiple **Task** subagents in **one** turn.

### Examples

**Compile errors**
- Agent A (`explore`): scan `Assets/Scripts` for API mismatches
- Agent B (`shell`): tail `%LOCALAPPDATA%\Unity\Editor\Editor.log` for `error CS`

**Ship a feature** (e.g. vehicle, ambience, menu)
- Agent A: editor import / prefab under `Assets/Scripts/Editor/`
- Agent B: runtime scripts under `Assets/Scripts/`
- Agent C: wire `BuildTestScene.cs` / `MainMenuBootstrap.cs`

**GitHub**
- Agent A: `git status`, `git diff`, `git log`
- Agent B: stage, commit, `git push https://x0205x@github.com/x0205x/FPS-Game.git main`

### After subagents finish

1. Merge their outputs — resolve duplicate edits.
2. One verification pass (compile log or targeted test).
3. Single commit if the user asked to push.

## Constraints

- **WebGL / `docs/`** — do not modify unless explicitly requested.
- **Scenes**: `MainMenu`, `TestPlayground`
- **Menu**: Web Version button → `MainMenuController.Options()`; Credits → `https://github.com/x0205x`
- **Gameplay ambient**: `SpaceAmbienceController` — ElevenLabs clip plays **once** after Start Prologue
