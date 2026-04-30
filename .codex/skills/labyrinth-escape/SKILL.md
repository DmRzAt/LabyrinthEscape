---
name: labyrinth-escape
description: Use when working on the Labyrinth Escape Unity project in C:\Users\Admin\LabyrinthEscape: Unity scripts, scenes, gameplay, UI, AI, puzzles, VR/FPS mechanics, project rules, or documentation alignment.
---

# Labyrinth Escape

## Communication Rules

- Write short answers.
- Be concise to save tokens.
- Do not add comments in code.
- Do not run tests unless the user explicitly asks. The user tests manually and sends errors.
- Do not scan the whole project without permission.
- Do not use Git in this project. The user handles Git.
- Do the exact requested task.
- Describe changes briefly, without extra reasoning.
- Be confident, but do not invent facts. Say when something is unknown.

## Project Context

- Project: `Labyrinth Escape`.
- Engine: Unity `6000.3.12f1`.
- Render pipeline: URP `17.3.0`.
- Genre: 3D corridor exploration / puzzle adventure.
- Main flow: `MainMenuScene` -> `GameScene` -> `EndScene`.
- Goal: explore labyrinth, solve puzzles, collect keys, open doors, avoid or fight enemies, reach final reward.
- Required systems: FPS/VR movement, levels/labyrinth, enemy AI, puzzle interactions, UI/HUD, win/lose flow.
- Optional systems: LAN co-op, moving platforms, traps, hidden passages, save system.

## Documentation Notes

- Authors listed in docs: Ivan Kasyniuk, Dmytro Zatserkivnyi, Bohdan Tsybulenko.
- Responsibilities:
  - Ivan: movement, UI, Unity/Git setup.
  - Dmytro: enemy AI, level design, GameScene.
  - Bohdan: puzzles, interactions, testing.
- Planned technologies:
  - Unity 6 LTS / `6000.x`.
  - XR Interaction Toolkit `3.0+`.
  - URP `17.x`.
  - TextMeshPro.
  - Newtonsoft.Json for save data.
- Current `Packages/manifest.json` includes URP, Input System, AI Navigation, TextMeshPro via Unity packages.
- XR Interaction Toolkit and Newtonsoft.Json were not visible in the checked manifest.

## Current Structure

- Scenes: `Assets/Scenes/MainMenuScene.unity`, `Assets/Scenes/GameScene.unity`, `Assets/Scenes/EndScene.unity`.
- Core scripts: `Assets/Scripts/Core`.
- Player scripts: `Assets/Scripts/Player`.
- Enemy scripts: `Assets/Scripts/Enemy`.
- Puzzle scripts: `Assets/Scripts/Puzzle`.
- UI scripts: `Assets/Scripts/UI`.
- Editor tools: `Assets/Scripts/Editor`.
- Models: `Assets/Models`.

## Existing Systems

- `GameManager`: singleton, keys, win/lose, scene loading.
- `PlayerController`: Rigidbody FPS movement and mouse look.
- `PlayerHealth`: health and damage flow.
- `EnemyAI`: NavMesh patrol/chase/attack states.
- `KeyItem`: pickup key behavior.
- `LockedDoor`: requires keys and opens by rotation.
- `HUD`: HP, keys, win/lose messages.
- UI scripts: main menu and end scene flow.

## Work Rules

- Prefer small, focused edits.
- Match existing Unity/C# style.
- Avoid broad refactors unless requested.
- Avoid changing generated Unity metadata unless needed.
- Ask before adding packages, large assets, or project-wide rewrites.
- If verification is needed, inspect only the relevant files unless the user permits broader search.
