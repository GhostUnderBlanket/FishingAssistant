# Configuration

Fishing Assistant 3 stores its settings in SMAPI's standard `config.json`. The current
schema version is `3`. Saved configuration, the active validated configuration, and an
editable menu draft are treated as separate objects.

## Compatibility and migration

The version 3 schema keeps the Fishing Assistant 2 property names where their meaning
is still supported. Legacy enum names remain JSON strings, while single-button legacy
keybinds load into SMAPI `KeybindList` values. A version 2 file without `ConfigVersion`
is identified as legacy, normalized, assigned version 3, and written back through
SMAPI.

Unknown top-level properties are reported in the SMAPI log before a migrated file is
written. They are not silently treated as supported options. A file whose
`ConfigVersion` is newer than 3 is loaded read-only: its schema version is not
downgraded, the file is not automatically rewritten, and applying a draft is blocked
to avoid discarding future data.

## Validation

Numeric settings are clamped to supported ranges. Missing keybinds, invalid enum
values, blank item preferences, whitespace, and duplicate ignore-list entries are
normalized to deterministic safe values. Every correction and unsupported property is
reported through SMAPI logging.

Unknown enum names are converted into an invalid sentinel and corrected field by field,
so one obsolete value doesn't prevent the remaining user choices from loading. If the
JSON document itself can't be read, the mod uses safe defaults for that session and
leaves the original file untouched for manual recovery.

After SMAPI raises `GameLaunched`, item preferences and the junk ignore list are
resolved through Stardew Valley's item registry. Existing IDs are converted to their
qualified form. Missing IDs or items in an incompatible category fall back to `Any` or
`None`, while missing junk-ignore IDs are removed. Delaying this pass until
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
Catch Assistance, Fish Preview, and Rod Enchantments. It provides Apply, Cancel, and
Reset Defaults actions, recalculates its bounds when the viewport changes, fits narrow
split-screen viewports, supports mouse and controller/snappy navigation, and displays
descriptions as tooltips. Category shoulder navigation and scrolling work without
requiring a mouse. Reset changes only the draft until Apply is selected.

Each open menu draft carries the configuration revision it was created from. If a
second local split-screen player applies a newer draft first, the stale menu is asked to
close and reopen instead of silently overwriting the other player's changes.

The menu opens through `OpenConfigMenuButton` or the `fa_config` SMAPI console command.
The console command remains available while the default menu keybind is unbound. More
enum, number, item, and keybind controls will be added incrementally; the menu is not
yet a complete replacement for editing every setting in `config.json`.
