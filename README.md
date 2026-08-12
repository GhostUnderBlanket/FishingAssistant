# Fishing Assistant 3

Fishing Assistant 3 is a complete rewrite of Fishing Assistant 2, a configurable
fishing automation and assistance mod for Stardew Valley.

The project is currently in early alpha development. It isn't ready for normal gameplay
or migration from Fishing Assistant 2 yet.

## Goals

- Rebuild the useful Fishing Assistant 2 systems on a maintainable architecture.
- Provide a custom in-game configuration menu without requiring Generic Mod Config
  Menu.
- Support single-player, local split-screen, and multiplayer as release requirements.
- Preserve existing configuration where practical while fixing unsafe legacy behavior.
- Add new assistance options without taking control away from the player.

See the [development roadmap](docs/ROADMAP.md) for the planned milestones and release
criteria, and [testing status](docs/TESTING.md) for scenarios which have actually been
verified in-game.

## Current status

The mod currently contains the SMAPI project scaffold, a typed and validated
configuration layer, a complete first pass of its custom configuration menu, and the
first runtime automation slices. The menu can edit every setting through a safe draft.
Per-screen runtime sessions can be toggled independently, observe the current fishing
phase, display it in a small HUD, automatically cast toward validated fishing water
after a configurable delay, hook a fish when a real nibble occurs, and steer the vanilla
fishing bar toward the fish. Other gameplay automation stages are not connected yet.
The assistant also closes each normal catch popup after a short viewing delay when
that automation stage is enabled.
Treasure targeting is a per-screen runtime toggle and defaults to off.
Normal and golden treasure chance overrides are functional outside festival minigames.
The optional instant-bite rule is functional in normal fishing and supported fishing
festival minigames.
Fishing treasure is collected into the correct local player's inventory when enabled;
full-inventory handling follows the configured stop, drop, or discard policy.
The fishing minigame can optionally be skipped for all fish or only species previously
caught by the current local player.
Bait can be attached and refilled from the current local player's inventory while the
rod is idle, with optional spawned bait clearly treated as a cheat setting.
Tackle can be attached under the same ownership safeguards, including independent
preferences for both slots on the Advanced Iridium Rod.
Infinite bait and tackle preserve the actual equipped attachment across each fishing
cycle without creating temporary replacement items, and snapshots are cleared before
saving or leaving the session.
Automatic eating can restore low energy from the current local player's inventory. It
prefers efficient plain food, avoids quest and progression items, and only consumes a
stack after Stardew Valley accepts the eating action.
Late-night behavior can warn a configured number of times and then disable only the
current screen's fishing automation once the active catch is safely complete. It does
not pause shared world time or force-open a game menu.
Before an energy-consuming cast, the assistant gives automatic eating the first chance
to recover energy and otherwise disables only that player's automation before the cast
can cause exhaustion. Efficient and event-controlled casts remain available.
Optional rod enchantments are session-only: the assistant remembers the exact instances
it added, removes them before every save, and never removes a player's existing
enchantments. They support single-player and local split-screen, but are disabled while
remote players are connected to prevent synchronized temporary state entering a save.
Fish difficulty can be multiplied and then adjusted by a fixed amount for each local
fishing minigame. The modifier preserves vanilla setup rules, supports manual and
automatic fishing, and never edits shared fish data.
Catch-result preferences now support perfect catches, maximum fish size, base fish
quality, and ordinary multi-catches during manual or automatic fishing. Festival and
fish-pond results stay fully vanilla; legendary fish, Wild Bait, and Challenge Bait
retain their vanilla catch-count rules. A perfect catch can still promote the selected
base quality using Stardew Valley's normal quality upgrade.
The fish preview now renders beside each local player's fishing bar. It can conceal
uncaught targets, reveal legendary targets independently, show localized fish names,
and indicate normal or golden treasure without changing the player's equipped tackle.
Automatic junk disposal now uses the visual junk and protected-item lists while that
player's automation is enabled. It removes only the quantity received by the latest
inventory change, never an older matching stack, and refuses to discard fish unless
the separate fish safeguard is enabled.
Development happens incrementally on the `development` branch.

## Requirements

- Stardew Valley 1.6.15 or later in the supported 1.6 line.
- SMAPI 4.5.2 or later.
- .NET 6 SDK or a newer SDK capable of targeting .NET 6, for development.

## Build

The project uses
[`Pathoschild.Stardew.ModBuildConfig`](https://www.nuget.org/packages/Pathoschild.Stardew.ModBuildConfig)
to find the local Stardew Valley installation and reference the game and SMAPI
assemblies.

```powershell
dotnet build FishingAssistant.slnx --configuration Debug
```

Normal development builds do not deploy into the game's `Mods` directory and do not
create release ZIP files.

## Branch workflow

- `main` stays release-ready and is updated only through reviewed pull requests.
- `development` is the integration branch for active development.
- Pull requests should be small enough to review and must pass relevant validation
  before merging.

## Reference material

Development may use locally available game code and third-party open-source mods for
behavioral research. Proprietary decompiled game code and repository-local references
must never be committed or redistributed. Any substantial reuse from an open-source
project must follow its license and be attributed.

## License

Fishing Assistant is available under the [MIT License](LICENSE).
