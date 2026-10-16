# TestPlayableAds

Technical test of a Unity playable ad: a simple sliding puzzle (taquin) exported with the **Unity Playworks (Luna) plugin**.

**Live preview:** [Luna Playground](https://playground.lunalabs.io/preview/532306/740883/bb3372790558fa1e13944dc60c9bbba148debc2d1669a3979b17ed7a6247a5fe)

- Unity **2022.3.62f3**, uGUI only, no external assets loaded at runtime
- Touch and mouse input, portrait and landscape layouts, safe-area aware
- Grid size **2x2 / 3x3 / 4x4** (3x3 by default), shuffle difficulty and all colors tweakable from the Luna Playground
- Store redirect and end-of-game handled by the Luna SDK (MRAID per network is configured in the Playground)

## Project layout

| Path | Content |
|---|---|
| `Assets/Scenes/SampleScene.unity` | The only scene: a camera and the `PuzzleGame` object |
| `Assets/Scripts/PuzzleGame.cs` | Game logic and UI, built in code at startup |
| `Assets/Scripts/PlayableCta.cs` | Luna SDK calls: store redirect, game end, analytics events |
| `Assets/Sprites/` | White rounded sprites with a vertical gradient, tinted at runtime |
| `Assets/Fonts/` | Roboto Condensed Bold (Apache 2.0, license included) |
| `luna.json` | Luna plugin configuration (font alphabet, runtime analysis, Playground app) |

## Setup

1. Put the Luna SDK next to the project: the package is referenced in `Packages/manifest.json` as `file:../../7.2.0/scripts`, so the folder layout must be:
   ```
   PlayableAdTest/
   ├── 7.2.0/            ← Unity Playworks SDK (not versioned)
   └── PlayableAdTest/   ← this repository
   ```
2. Open the project with Unity 2022.3.62f3.
3. In the Unity Playworks window, set **MSBuild Tools** to an MSBuild 15+ executable (for example Visual Studio 2022 Build Tools).

The `LUNA_IS_PRESENT` define is set in both the Player Settings and `luna.json`; Luna calls are compiled out without it.

## Testing

- **Editor:** open `SampleScene` and press Play. Use a portrait Game view resolution (e.g. 1080x1920) to see the mobile layout. Luna calls only log in the editor.
- **Luna:** build from the Unity Playworks window, then use the local Playground (`http://127.0.0.1:8000/`). If a new Playground field does not show up, delete `LunaTemp/` and rebuild.

## Text rendering

Luna rasterizes fonts into a bitmap atlas. To keep text sharp without a large atlas, `luna.json` uses 160 px glyphs in a 1024×1024 atlas and an alphabet restricted to the characters the game displays. **When changing an in-game text, add any new character to the font alphabet** (Unity Playworks window → Fonts), otherwise it will not render. Text size relative to its container is set by `TextScale` in `PuzzleGame.cs`.

## Playground fields

| Section | Field | Default |
|---|---|---|
| Gameplay | Shuffle moves | 20 (random valid moves from the solved grid) |
| Gameplay | Grid size | Grid3x3 |
| Appearance | Background, Board, Tiles, Tile numbers, Title, Download button, Button text, Replay button, End overlay | see `PuzzleGame.cs` |

Store URLs (iOS / Android) are set in the Luna Playground, not in code.

## Analytics events

Standard: `TutorialComplete` (first move), `LevelWon`, `EndCardShown`, `LevelRetry`.

Custom:

| Event | When | Value |
|---|---|---|
| `first_move` | first move | 0 |
| `invalid_tap` | first tap on a tile not aligned with the gap | moves |
| `multi_tile_slide` | first slide moving several tiles | tiles moved |
| `row_1_complete` … `row_3_complete` | a row is fully in place (once per game) | moves |
| `moves_10`, `moves_20`, `moves_50` | move milestones | moves |
| `puzzle_solved_moves` | puzzle solved | moves |
| `cta_click_ingame`, `cta_click_endcard` | DOWNLOAD tapped during the game / on the end card | moves |
| `replay` | Replay tapped | replay count |

Event names are string literals in `PlayableCta.cs` so that Luna's build analysis can count them.
