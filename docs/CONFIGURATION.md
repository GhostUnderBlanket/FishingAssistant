# Configuration

Fishing Assistant 3 loads SMAPI's standard `config.json` as the migration-compatible
base template. Once a player is loaded, that player's settings are stored separately in
`config.players/player-<UniqueMultiplayerID>.json`. The current schema version is `10`.
Saved configuration, the active validated player profile, and an editable menu draft
are treated as separate objects.

Using Stardew's stable multiplayer player ID instead of split-screen order means each
single-player, host, farmhand, and local co-op player retains their own settings when
screens join or leave. A player without a profile starts from a deep copy of the base
template, preserving existing Fishing Assistant 2/3 choices without linking later
edits back to another player. Unreadable and future-schema profile files are activated
read-only for the session and never overwritten automatically.

## Compatibility and migration

The current schema keeps the Fishing Assistant 2 property names where their meaning
is still supported. Legacy enum names remain JSON strings, while single-button legacy
keybinds load into SMAPI `KeybindList` values. A version 2 file without `ConfigVersion`
is identified as legacy, normalized, assigned the current version, and written back through
SMAPI.

Unknown top-level properties are reported in the SMAPI log before a migrated file is
written. Reports include a bounded representation of the original JSON value so the
setting can be identified; values with credential-like property names are redacted.
They are not silently treated as supported options. A file whose
`ConfigVersion` is newer than 10 is loaded read-only: its schema version is not
downgraded, the file is not automatically rewritten, and applying a draft is blocked
to avoid discarding future data.

Schema version 6 retires two Fishing Assistant 2 settings and reports both during
migration:

- `CatchTreasureButton` is omitted when the normalized file is rewritten. Its
  replacement, `TreasureTargeting`, is edited only through the custom configuration
  menu and defaults to off.
- `JunkHighestPrice` is omitted because price-based junk classification was replaced
  by the visual Junk List/Junk Ignore List editor. A price threshold cannot be mapped
  safely to a stable set of item IDs, so the editor starts from the documented default
  trash list and preserves the legacy `JunkIgnoreList`.

Schema version 7 adds a visual `TreasureChestIgnoreList` and the
`ActionIfOnlyIgnoredTreasureRemains` policy. The safe default keeps the fishing chest
open for manual handling; players may instead opt into dropping ignored items into the
world or permanently discarding them. Inventory-full handling remains separate and
never classifies ignored treasure as an item that failed to fit.

Schema version 8 adds opt-in `AutomaticBubbleSteering`. It assists both manual and
automatic casts by applying vanilla-speed sideways steering toward a reachable,
fishable bubble. It doesn't move the player, extend the chosen cast distance, or
change the configured or manually selected cast power.

Schema version 9 adds per-player automation profiles. Relaxed automates the core loop;
Training leaves the fishing minigame to the player; Manual+ leaves casting, hooking,
and the minigame manual while enabling preview, bubble steering, popup closing, and
treasure looting. Editing any profile-controlled option marks the draft as Custom.
Migration labels existing configurations Custom so their individual choices are never
silently replaced by a preset. Profiles do not change cheat, catch-result, difficulty,
inventory, food, equipment, or keybind settings.

Schema version 10 extends bubble steering to automatic casts. Relaxed and Training
profiles enable it; Custom profiles retain the player's explicit choice.

The migration test fixture contains every public Fishing Assistant 2 configuration
property. It verifies that every still-supported non-default choice survives and that
both retired choices are reported deliberately.

## Validation

Numeric settings are clamped to supported ranges. Missing keybinds, invalid enum
values, blank item preferences, whitespace, and duplicate ignore-list entries are
normalized to deterministic safe values. Every correction and unsupported property is
reported through SMAPI logging.

Unknown enum names are converted into an invalid sentinel and corrected field by field,
so one obsolete value doesn't prevent the remaining user choices from loading. If the
JSON document itself can't be read, the mod uses safe defaults for that session and
leaves the original file untouched for manual recovery.

After SMAPI raises `GameLaunched`, item preferences, both junk lists, and the treasure
ignore list are
resolved through Stardew Valley's item registry. Existing IDs are converted to their
qualified form. Missing IDs or items in an incompatible category fall back to `Any` or
`None`, while missing junk-list IDs are removed. Delaying this pass until
`GameLaunched` lets content packs from other mods register their item data first.

Dependent settings are preserved but reported when inactive. For example, spawning
bait has no effect while automatic bait attachment is disabled. Minigame skipping also
reports that it takes priority over automatic minigame play when both are enabled. The
custom menu can use these warnings to explain unavailable or overridden options without
discarding the player's choices.

## Draft behavior

`ConfigManager.CreateEditSession` creates an independent deep copy of mutable
configuration members. The custom menu edits only this draft. Applying validates and
persists a copy before replacing the active configuration; canceling can discard the
draft without changing runtime or saved settings.

## Custom menu status

The menu can edit boolean settings across Automation, Inventory & Food, Bait & Tackle,
Catch Assistance, Fish Preview, and Rod Enchantments. Boolean, enum, and numeric
settings use reusable controls. Numeric steppers clamp at the same ranges enforced by
configuration validation. The menu provides Apply, Cancel, and Reset Defaults actions,
recalculates its bounds when the viewport, UI scale, zoom, language, or visible option
set changes, fits narrow split-screen viewports, supports mouse and controller/snappy
navigation, and displays descriptions as tooltips. Category shoulder/trigger navigation
and scrolling work without requiring a mouse. Reset
opens a confirmation dialog before replacing the draft with defaults. Canceling that
dialog leaves the draft untouched; confirming still changes only the draft until Apply
is selected.

Each category is a single focused settings page, so the category title deliberately
replaces additional section headers. Persistent description blocks are also deliberately
omitted: option descriptions are available as hover/controller tooltips, while warnings
and unavailable reasons appear inline beneath only the controls they affect. This keeps
the editable control visible in small and split-screen viewports.

Destructive and free-item choices also receive an immediate inline notice whenever they
are selected. This covers permanently discarding full-inventory treasure, automatic junk
removal, automatic food/fish consumption, starter rods, and bait/tackle spawning. Their
safe defaults remain `Stop`, disabled, or `No starter rod`, so each behavior requires an
explicit player choice.

Each open menu draft carries both the owning player-profile key and the revision it was
created from. Applying one local split-screen player's draft updates only that profile
and doesn't invalidate another player's draft. A draft is rejected if its own profile
changed after opening or the active local player no longer matches its owner.

The menu opens through `OpenConfigMenuButton`, which defaults to F6, or the `fa_config`
SMAPI console command. Controller Back is also available as a local controller fallback
while no menu is active, so a keyboard-only default does not block a split-screen
player from reaching the keybind editor. The console command remains available if the
menu keybind is manually cleared. Item pickers are populated from the game registry after
`GameLaunched`, so loaded
content-pack bait and tackle can appear by localized name. The controls category can
capture keyboard, mouse, controller, and multi-button keybinds through SMAPI. Escape or
controller B cancels capture; Backspace or Delete clears a binding.

The combined junk/junk-ignore editor, treasure-ignore editor, and the bait, tackle, and starter-rod pickers show
localized item names and sprites, support search and scrolling, and preserve the same
Apply/Cancel draft behavior. Every current setting can be edited without manually
changing `config.json`. Starter rod defaults to `No starter rod`; selecting a rod is an
explicit opt-in and never replaces a rod the local player already owns.
