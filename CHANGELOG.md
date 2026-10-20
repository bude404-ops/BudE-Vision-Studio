# Changelog

## 0.2.0 - Oct 20, 2026

### Added
- **STUDIO LINK** - the headline feature: Bigfoot builds INSIDE your Unity with zero setup.
  No relay, no tunnels, no extra windows. The editor polls the Bigfoot conversation over
  normal outbound HTTPS; when Bigfoot replies with a `bude-exec` command block, you review
  it and click EXECUTE (or enable auto-run). Supported ops: primitives, sprites, cameras,
  art import by URL (async, auto sprite settings), delete, build 2D/3D starter maps,
  save scene. Results post back into the conversation so Bigfoot verifies and iterates.
- SETTINGS: Studio Link enable + auto-run toggles (default off, approval required).
- MiniJson (dependency-free JSON) added under Runtime/Scripts.
- 7 new EditMode tests for Studio Link payload extraction (fence parsing, newest-wins,
  hidden-message skip, malformed-fence tolerance).

## 0.1.0 - Oct 20, 2026

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
