# Bude Studio — ROADMAP

Build order per the brief: smallest working version first, then expand in
stages. After each stage: compile, test, commit, report honestly.

## Stage 1 — Foundation (THIS RELEASE, v0.1.0)

- [x] UPM package (com.bude404.visionstudio) installable from git URL
- [x] Bude Studio window (PLAN / BIBLE / FACTORY / CREATE / SETTINGS)
- [x] Game Plan asset (title, pitch, tasks, regions)
- [x] Asset Bible (entries, categories, duplicate hash check, upsert)
- [x] Safe art import (file picker, category, sprite settings, approval dialogs)
- [x] Object factory (sprites, platforms, primitives, lights, text; undo-safe;
      no fake primitives; sprite tinting via renderer color)
- [x] 2D platformer starter scene (player, platforms, collectibles, score, goal)
- [x] 3D arena starter scene (player, blocks, pickups, goal, camera)
- [x] Runtime starter kit (PlayerController2D/3D, Collectible, ScoreTracker,
      GoalFlag, CameraFollow)
- [x] Project backups (timestamped, pruned, one click)
- [x] Bigfoot chat transport (agent API + browser fallback, defensive parsing)
- [x] Sanity tests (state roundtrip, bible upsert, reply extraction)
- [ ] Verified compile in Unity 6000.6 ← NEXT: install via git URL and report
- [ ] In-window chat live (needs agent API key from MC)

## Stage 2 — Repository & prefab placement

- [ ] Asset categories search/filter in BIBLE
- [ ] Prefab workflow: save selection as prefab from FACTORY
- [ ] Dependency notes on bible entries (what scene uses what)

## Stage 3 — Visual building mode

- [ ] Grid-snap palette for placement
- [ ] Bulk placement brush
- [ ] Layer manager shortcuts (Background / Middle / Ground / Props / Player)

## Stage 4 — Lil Foots 2.5D template

- [ ] Region scaffold generator (Mossveil-style terrain strips, painted-ground
      doctrine, sorted layers)
- [ ] Frame-animation import (sprite sheets → animator states: idle/run/jump)
- [ ] Per-map timer + medals wiring

## Stage 5 — Rigging & animation

- [ ] 2D bone rig generation on imported character art
- [ ] One-click walk/run/jump/idle/attack clips
- [ ] Visual correction tools (retime, mirror fix, facing lock)

## Stage 6 — Autopilot & validation

- [ ] Scene validation report (missing colliders, unsorted layers, dead refs)
- [ ] Safe auto-repair pass with approval
- [ ] Playtest capture + console error extraction back to Bigfoot
