# Changelog

All notable changes to Fishing Assistant 3 will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the
project uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [3.0.0-beta.1] - 2026-08-13

### Added

- Controller Back can open the configuration menu from the world as a fallback for
  local controller players, while the configurable F6 default remains unchanged.
- Automatic configuration-menu layout and navigation rebuilding when viewport size, UI
  scale, zoom, language, or visible option set changes while the menu is open.
- Left and right trigger support for configuration-menu category navigation.
- A reusable disabled configuration-control state with inline unavailable reasons;
  temporary rod enchantment controls are disabled while remote players are connected.
- Small colored validation descriptions beneath settings whose dependencies make them
  ineffective or overridden, updated immediately while editing the configuration draft.
- Inline safety notices for active item-discard, automatic consumption, starter-rod,
  and bait/tackle spawn choices, with automated coverage for their opt-in defaults.
- Player-scoped configuration profiles keyed by stable multiplayer ID, preventing one
  local co-op player's Apply action from changing another player's runtime or saved
  settings while retaining `config.json` as the legacy-compatible template.
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
- Complete Fishing Assistant 2 migration coverage, explicit reports for retired
  `CatchTreasureButton` and `JunkHighestPrice` settings, bounded unknown-value reports,
  and regression tests protecting malformed and future-schema files from overwrite.
- Initial responsive custom configuration menu with draft-based automation checkboxes,
  Apply, Cancel, Reset Defaults, mouse input, and controller navigation.
- A deliberate confirmation dialog for Reset Defaults which leaves the editable draft
  unchanged when canceled.
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
- Config-driven treasure targeting for automatic minigame play, including safe
  catch-progress hysteresis.
- Functional normal and golden treasure chance overrides applied once per catch, with
  `Default`, `Always`, and `Never` behavior while festival rules remain vanilla.
- A per-screen Debug action which warps the local player to the ocean-fishing spot by
  Willy's Fish Shop, while refusing to interrupt active events or minigames.
- Per-screen automatic catch-popup closing after a short viewing delay, without
  simulating shared keyboard or controller input.
- Per-screen instant bites which remove the post-cast wait while preserving the game's
  normal nibble, hook, fish selection, and festival-minigame flow.
- Per-screen automatic fishing-treasure collection with partial-stack support and
  explicit stop, ground-drop, or discard handling when the inventory is full.
- Optional per-screen fishing-minigame skipping for every catch or only previously
  caught species, while vanilla resolves rewards, Challenge Bait, and festival scores.
- Local-player automatic bait attachment and refill using inventory preference rules,
  with an explicit opt-in fallback for spawning bait when none is available.
- Local-player automatic tackle attachment with independent preferences for both
  Advanced Iridium Rod slots and an explicit opt-in spawn fallback.
- Per-screen infinite bait and tackle preservation which snapshots real attachments
  during a cast and restores only consumed stack or durability before saving.
- Per-screen automatic food consumption at the configured energy threshold, with
  deterministic local-inventory selection and safeguards for fish, buffs, fullness,
  quest items, and progression items.
- Per-screen late-night warnings which honor the configured warning count and defer
  disabling automation until the active fishing cycle and reward menus have ended.
- Low-energy cast protection which lets automatic eating run first, then disables only
  the current screen's automation before a cast can cause exhaustion.
- Session-only Auto-Hook, Efficient, Master, and Preserving rod enchantments which
  track exact assistant-owned instances, suspend during saving, preserve existing
  enchantments, support local split-screen, and disable when remote players connect.
- Per-screen fish difficulty multiplier and additive adjustment applied once to each
  fishing minigame after vanilla tutorial and blessing modifiers.
- A per-screen fish preview beside the fishing bar with responsive viewport placement,
  caught/uncaught concealment, optional names, legendary reveal rules, and normal or
  golden treasure indicators without simulating an equipped Sonar Bobber.
- Local-player automatic junk disposal driven by the visual junk and protected-item
  lists. It removes only newly acquired stack quantities, preserves existing stack
  contents, requires explicit permission for fish, and honors vanilla trashability.
- Instant fishing-treasure capture once a chest has fully appeared, including skipped
  minigames while preserving festival treasure rules.
- A visual Treasure Chest Ignore List with configurable keep-open, drop, and discard
  behavior when only ignored rewards remain.
- Automatic Bubble Steering for reachable fishing bubbles during manual and automatic
  casts, without moving the player or extending cast range.
- Relaxed, Training, Manual+, and Custom Automation Profiles.
- Session cast-power learning for subsequent automatic casts and a debug command for
  creating a reachable fishing bubble.

### Changed

- Replaced the text automation-status HUD with a compact Fishing Assistant 2-style
  full-color fishing-rod icon and animated Vanilla emotes for active, disabled, paused,
  stopped, warning, and recovered states.
- The automation HUD now hides behind ordinary menus, non-festival events, and
  unrelated minigames while remaining available during the fishing bar and supported
  fishing festivals; layout stays within each local logical UI viewport.
- Replaced the temporary treasure-targeting hotkey and HUD icon with one persistent
  checkbox in the custom configuration menu.
- Changed the default configuration-menu keybind from unbound to F6 after retiring the
  treasure-targeting hotkey. Existing explicit keybind choices remain unchanged.
- Replaced price-threshold junk classification with explicit visual item selection.
- Replaced bait, tackle, and starter-rod cycling controls with searchable visual item
  pickers, and increased spacing in the remaining value selectors.
- New configurations and Reset Defaults now classify the five standard fishing-trash
  items as junk: Trash, Driftwood, Broken Glasses, Broken CD, and Soggy Newspaper.

### Fixed

- Keybind capture now waits for the mouse, keyboard, or controller activation input to
  be released before accepting the next input, preventing an immediate return to the
  previous binding.
- Configuration-menu rebuilds now retain their current layout signature instead of
  rebuilding every tick and cancelling active keybind capture.
- Added Vanilla-style backing panels to the Junk List Editor title and selection
  counts, separated its search and mode controls from the upper frame, and replaced
  tiny-font item states with scaled small-font labels.
- Refined picker spacing and header padding, added the missing item-picker title panel,
  and tightened two-line item-card labels.
- Added clear gutters between configuration options, pagination, and the scrollbar.
- Added top spacing above the category selector so its label and navigation buttons no
  longer touch the menu frame.
- Tightened the vertical spacing between item names and Junk/Ignored state labels.
- Junk, protected-item, and treasure-ignore editors now group the active mode's selected
  items first, followed by a separator and all unassigned items.
- Replaced the oversized Vanilla partition sprite between editor groups with a thin
  separator confined to the row gap, and improved selected-card label contrast.
- Disabling Automation with F5 no longer cancels a cast already in progress. Vanilla
  finishes that cast normally while every later automatic action remains disabled,
  avoiding stuck rod state or shared fishing audio in local co-op.
- Prevented snappy controller navigation from processing a directional press twice and
  skipping an option.
- Positioned Fish Preview against the active local game's viewport, matching the
  BobberBar coordinate system in split-screen.

- Reset Defaults confirmation now wraps to the current UI viewport instead of creating
  an oversized, off-screen vanilla dialog.
- Category and scrolling arrows now use game sprites instead of font glyphs, avoiding
  the left arrow rendering as a heart in Stardew Valley's dialogue font.
- The configuration-menu title now has a game-style backing panel for contrast against
  dark menu backgrounds.
- Action controls now match selector height, and category, scrollbar, and picker arrows
  share one consistent visual scale.
- Automatic casting now recognizes that the Efficient enchantment and event-controlled
  fishing casts do not consume player stamina.
- Bubble steering now targets the nearest point inside the bubble tile, matching the
  full reach available through Vanilla sideways steering.
