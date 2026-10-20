# Changelog

## 0.1.0 — 2026-10-20

First release: Bude Studio stage 1.

- Bude Studio editor window with PLAN / BIBLE / FACTORY / CREATE / SETTINGS tabs.
- Game Plan and Asset Bible data assets.
- Safe art import with duplicate (MD5) detection and sprite import settings.
- Object factory with undo support (sprites, platforms, primitives, lights, text).
- Playable starter scenes: 2D platformer and 3D arena.
- Runtime starter kit: player controllers (2D/3D), collectibles, score, goal
  flag, camera follow.
- Project backups (timestamped, auto-pruned).
- Bigfoot chat transport with agent API + browser fallback and defensive
  reply parsing.
- Edit-mode sanity tests.

Notes: written against Unity 6000.6; compilation not yet verified in-editor.
In-window chat requires the agent API key (pending from MC); browser fallback
is active until then.
