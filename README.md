# BudE Vision Studio

A personal, beginner-friendly AI game creation toolkit that lives inside Unity.
Describe what you want in plain English, Bigfoot builds it: maps, characters,
props, and starter kits. Built for BudE404, powered by the Bigfoot404 agent.

No accounts, no subscriptions, no payments, no cloud services beyond your own
Bigfoot agent.

## Install (one click)

1. Open your Unity project (Unity 6.6 / 6000.6).
2. `Window > Package Manager`.
3. Click the **+** button (top-left) > **Add package from git URL...**
4. Paste: `https://github.com/bude404-ops/BudE-Vision-Studio.git`
5. Click **Add**. Done.

Then open the toolkit: menu **Bude > Bude Studio**.

## What's inside (MVP, stage 1)

- **Bude Studio window** with big-button tabs:
  - **PLAN** — describe your game; keep a living game plan with tasks.
  - **BIBLE** — your asset library: import art, track what exists, no duplicates.
  - **FACTORY** — one-click objects: sprites, platforms, cubes, lights. All undoable.
  - **CREATE** — generate a playable starter scene (2D platformer or 3D arena),
    with player controls, collectibles, score, and a goal flag. Press Play.
  - **SETTINGS** — Bigfoot chat hookup (agent URL + key) and project backup.
- **Bigfoot chat** — talk to your Bigfoot agent from inside Unity. Paste your
  agent's API details in SETTINGS. Without a key it falls back to opening your
  agent's chat in the browser.
- **Safe art import** — pick a file, choose a category, get sprite import
  settings applied automatically. Duplicate files are caught by content hash.
- **Project backups** — one button, copies Assets + ProjectSettings with a
  timestamp. Roll back by copying back.

## Honest status (read this)

This is stage 1. It is written against Unity 6000.6 APIs but **compilation has
not been verified in the editor yet** — if anything throws an error, screenshot
the console and send it to Bigfoot; it gets fixed same day. Not built yet
(coming stages, see `Documentation~/ROADMAP.md`): one-click rigging and
animation generation, AI art generation in-editor (needs the agent API key from
MC), terrain sculpting brushes, and the Lil Foots 2.5D template wiring.

## Workflow

Tell Bigfoot what you want → approve the plan → generate/import art →
edit visually in the scene → press Play.

## For Bigfoot (maintenance)

- Editor code: `Editor/`. Runtime components: `Runtime/`. Tests: `Tests/`.
- Docs: `Documentation~/SPEC.md` (full vision) and `Documentation~/ROADMAP.md`
  (stage status).
- Never commit API keys. Keys live in `Assets/BudeStudio/Settings.asset`, which
  is gitignored inside projects.

## Notes for Unity settings

The starter player scripts use the classic Input class (arrow keys, WASD,
space). In Project Settings > Player > Active Input Handling, make sure
"Input Manager (Old)" or "Both" is selected (that's Unity's default).
