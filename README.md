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

The whole studio loads as a normal Unity package (no relay scripts, no tunnels,
nothing to run outside Unity). Updating it later = Package Manager > update.

## What's inside (stage 2)

- **Bude Studio window** with big-button tabs:
  - **PLAN** — describe your game; keep a living game plan with tasks.
  - **BIBLE** — your asset library: import art, track what exists, no duplicates.
  - **FACTORY** — one-click objects: primitives, cameras, sprites, collectibles,
    score keepers. All undoable.
  - **CREATE** — generate a playable starter scene (2D platformer or 3D arena),
    with player controls, collectibles, score, and a goal flag. Press Play.
  - **CHAT** — talk to your Bigfoot agent from inside Unity. Without a key it
    falls back to opening your agent's chat in the browser.
  - **SETTINGS** — agent hookup, import defaults, backups, Studio Link.
- **STUDIO LINK (v0.2.0)** — Bigfoot builds inside your Unity with **zero setup**:
  no relay, no tunnels, no extra windows. See the section below.
- **Safe art import** — pick a file, choose a category, get sprite import
  settings applied automatically. Duplicate files are caught by content hash.
- **Project backups** — one button, zips Assets + ProjectSettings with a
  timestamp. Roll back by copying back.

## Studio Link: Bigfoot builds in Unity, no setup

This replaces the relay/tunnel dance from the Lil Foots days. Unity only makes
normal outbound HTTPS calls, so there are no ports to open, no firewall prompts,
and nothing to start before using it.

1. SETTINGS tab: paste your agent API key, click CONNECT, tick **Enable Studio Link**.
2. Ask Bigfoot for something in plain English ("build me a hill with a cave entrance").
3. When his reply contains a build plan, the CHAT tab shows it as a pending
   command. Click **EXECUTE** (or turn on auto-run in SETTINGS).
4. The objects appear in your scene, fully undoable. The outcome is posted back
   into the conversation automatically, so Bigfoot sees success or console errors
   and can fix and re-send himself.
5. Edit anything afterwards with the studio tools.

Supported build ops: primitives, sprites, cameras, art import from URL (async,
auto sprite settings), delete object, build 2D/3D starter map, save scene.
Commands are approval-gated by default.

## Honest status (read this)

Written against Unity 6000.6 APIs but **compilation has not been verified in a
real editor yet** — if anything throws an error, screenshot the console and send
it to Bigfoot; it gets fixed same day. Still to come (see
`Documentation~/ROADMAP.md`): AI art generation in-editor, image-to-3D models,
one-click rigging and animation, terrain sculpting brushes, Lil Foots 2.5D
template wiring.

## Workflow

Tell Bigfoot what you want → he builds it live (Studio Link) → you edit visually
in the scene → press Play → tell him what to fix.

## For Bigfoot (maintenance)

- Editor code: `Editor/`. Runtime components and data: `Runtime/`. Tests: `Tests/`.
- Docs: `Documentation~/SPEC.md` (full vision) and `Documentation~/ROADMAP.md`
  (stage status).
- Never commit API keys. Keys live in `Assets/BudeStudio/BudeStudioSettings.asset`,
  which is gitignored inside projects.
- Studio Link contract: assistant messages carry ```bude-exec fenced JSON
  (`{"ops":[{"op":"primitive","type":"Cube","name":"Block","pos":[0,1,0]}]}`).
  The editor polls the conversation, executes on approval, and posts
  `STUDIO LINK RESULT: ...` back as a user message.

## Notes for Unity settings

The starter player scripts use the classic Input class (arrow keys, WASD,
space). In Project Settings > Player > Active Input Handling, make sure
"Input Manager (Old)" or "Both" is selected (that's Unity's default).
