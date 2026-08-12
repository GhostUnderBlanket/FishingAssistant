# Changelog

All notable changes to Fishing Assistant 3 will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the
project uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Initial SMAPI project scaffold for Fishing Assistant 3.
- Startup log confirming that the mod loaded.
- Development roadmap and project documentation.
- A typed version 3 configuration schema with legacy-compatible keys and keybinds.
- Configuration normalization, migration reporting, editable drafts, and unit tests.
- Protection against overwriting configuration files created by a newer schema.
- Game-registry validation for bait, tackle, starter rod, and junk-ignore item IDs.
- Warnings for dependent settings that are inactive or overridden.
- Safe field-level recovery for unknown enum names and non-destructive fallback for
  unreadable configuration files.
- Initial responsive custom configuration menu with draft-based automation checkboxes,
  Apply, Cancel, Reset Defaults, mouse input, and controller navigation.
- Configuration-menu opening through a configurable keybind or the `fa_config` console
  command.
- Responsive category navigation and scrolling for all boolean configuration settings.
- Reusable enum selectors and numeric steppers with mouse, keyboard, and controller
  input for all enum and numeric configuration settings.
- Item selectors populated from Stardew Valley's loaded bait, tackle, and fishing-rod
  data, including items registered by other mods.
- A keybind editor which captures keyboard, mouse, controller, and multi-button input
  through SMAPI while preserving draft/apply/cancel behavior.
- A combined visual junk-list and junk-ignore editor with item icons, localized names,
  search, scrolling, and mouse, keyboard, and controller selection.
- Per-screen automation sessions, a tested fishing-state observer, lifecycle recovery,
  runtime toggle keybind, and a compact state HUD. This slice observes state only and
  does not automate gameplay input yet.
- Per-screen automatic casting with configurable power and recast delay, stamina and
  player-state safeguards, festival blocking, and validation of the predicted fishing
  target before casting.
- Per-screen automatic hooking which responds once per real nibble, respects the
  vanilla Auto-Hook enchantment, and avoids festival or menu-conflicted input.
- Automatic casting and hooking inside Stardew Valley's fishing festival minigames,
  while unrelated festival contexts remain blocked.
- Per-screen automatic fishing-minigame control which steers the vanilla fishing bar
  toward the fish without directly changing catch progress or results.
- Per-screen treasure targeting toggled with the configured treasure key, including
  catch-progress hysteresis and HUD status.
- Functional normal and golden treasure chance overrides applied once per catch, with
  `Default`, `Always`, and `Never` behavior while festival rules remain vanilla.
- A per-screen Debug action which warps the local player to the ocean-fishing spot by
  Willy's Fish Shop, while refusing to interrupt active events or minigames.

### Changed

- Replaced price-threshold junk classification with explicit visual item selection.
- Replaced bait, tackle, and starter-rod cycling controls with searchable visual item
  pickers, and increased spacing in the remaining value selectors.
- New configurations and Reset Defaults now classify the five standard fishing-trash
  items as junk: Trash, Driftwood, Broken Glasses, Broken CD, and Soggy Newspaper.

### Fixed

- Category and scrolling arrows now use game sprites instead of font glyphs, avoiding
  the left arrow rendering as a heart in Stardew Valley's dialogue font.
- The configuration-menu title now has a game-style backing panel for contrast against
  dark menu backgrounds.
- Action controls now match selector height, and category, scrollbar, and picker arrows
  share one consistent visual scale.
