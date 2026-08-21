# Fishing Assistant 3 Roadmap

Fishing Assistant 3 is a full rewrite of Fishing Assistant 2 for Stardew Valley 1.6.15
and SMAPI 4.5.2. The rewrite keeps the mod's useful core behavior, fixes unsafe or
fragile behavior, introduces a custom configuration menu, and treats single-player,
local split-screen, and multiplayer as first-class release targets.

## Product principles

- Automate repetitive fishing without taking control away from the player.
- Let every automation stage be enabled, disabled, and understood independently.
- Keep runtime state isolated per local screen and per player.
- Never persist temporary item swaps or assistant state into a save.
- Prefer supported SMAPI APIs and public game members over reflection or Harmony.
- Preserve Fishing Assistant 2 configuration where practical, but don't preserve bugs.
- Keep the default experience safe and close to vanilla progression.

## Release strategy

Development happens on `development`. Each milestone should be delivered through small,
reviewable branches and merged back into `development`. The release candidate reaches
`main` only through a pull request after the full release test matrix passes.

Planned version stages:

| Stage | Purpose |
| --- | --- |
| `3.0.0-alpha.*` | Architecture, configuration UI, and individual feature development. |
| `3.0.0-beta.*` | Feature-complete parity build; compatibility and multiplayer testing. |
| `3.0.0-rc.*` | Release candidate; bug fixes, translations, packaging, and documentation only. |
| `3.0.0` | Public release. |

The project entered `3.0.0-beta.1` on 2026-08-13 after the core loop, local safety
baseline, and the three approved Milestone 8 features were implemented. Remote
multiplayer hardening and the full release matrix remain beta requirements.

## Milestone 0: Project foundation

Goal: establish a predictable build, architecture, and release baseline before adding
gameplay behavior.

- Add repository documentation: README, contribution notes, license, changelog, and
  architecture decision records.
- Add central build settings, deterministic versioning, release ZIP generation, and a
  validation build that never deploys into the installed game.
- Add CI for restore, build, formatting, and tests without distributing game files.
- Define feature ownership: local-only, per-screen, per-player, synchronized, or
  host-authoritative.
- Introduce focused project boundaries for configuration, UI, runtime state, fishing,
  inventory, HUD, and integrations.
- Add a lightweight test project for pure decision logic.
- Define structured logging categories and avoid per-tick log noise.

Exit criteria:

- Debug and release builds pass with zero project warnings.
- CI can validate a pull request without requiring committed game assemblies.
- No build step deploys into the user's game unless explicitly enabled locally.

## Milestone 1: Configuration model and migration

Goal: make configuration safe, typed, validated, and upgradeable before connecting it
to gameplay systems.

- Define the Fishing Assistant 3 configuration schema using typed enums and
  `KeybindList` values where appropriate.
- Implement validation and normalization for numbers, enum values, item IDs, and
  conflicting settings.
- Read Fishing Assistant 2 keys and migrate them without silently losing user choices.
- Preserve unknown legacy values long enough to report useful migration warnings.
- Separate saved configuration from the mutable draft edited by the menu.
- Add tests using representative version 2 configurations, missing fields, invalid
  values, and future/unknown values.

Exit criteria:

- A version 2 configuration loads into version 3 with documented results.
- Invalid configuration cannot crash the mod or leave partially applied state.
- Defaults and migration behavior are covered by automated tests.

## Milestone 2: Custom configuration menu

Goal: ship a reusable Stardew-native menu framework without depending on Generic Mod
Config Menu.

- Build a responsive `IClickableMenu` shell with title, categories, scrolling, footer,
  tooltips, and validation messages.
- Add reusable controls: checkbox, enum selector, number slider/stepper, keybind editor,
  text/list editor, and action button. Categories intentionally replace per-page section
  headers, while tooltips and inline validation/unavailable messages replace persistent
  description blocks so narrow split-screen layouts retain space for editable controls.
- Implement Apply, Cancel, and Reset Defaults using a draft configuration.
- Support mouse, keyboard, controller, snappy navigation, scroll wheel, and gamepad
  shoulder/trigger navigation.
- Rebuild component bounds and navigation links after viewport, UI scale, language,
  category, or option-visibility changes.
- Fit small split-screen viewports and long translated labels without hiding controls.
- Show why an option is unavailable in the current context instead of silently ignoring
  it.
- Open the menu through a configurable keybind and a console command fallback.

Exit criteria:

- Every configuration value can be edited without touching `config.json` manually.
- Apply persists validated changes; Cancel leaves saved/runtime settings unchanged.
- The menu is fully usable with mouse, keyboard, and controller in full-screen and
  split-screen viewports.

## Milestone 3: Runtime context and automation state machine

Goal: create the safe execution core that all fishing features use.

- Introduce per-screen session state through SMAPI's split-screen utilities.
- Track the correct local player, rod, menu, viewport, input context, and automation
  state without assuming `Game1.player` is globally unique.
- Model the observed fishing loop explicitly: Idle, Ready, Casting, WaitingForBite,
  Hooking, Minigame, CatchResult, TreasureMenu, and Paused. Unexpected jumps are marked
  as recovered transitions instead of introducing synthetic Cooldown or Faulted states
  which cannot be derived reliably from the game context.
- Define legal transitions, cancellation rules, timeouts, and recovery behavior.
- Reset stale state on tool changes, menu changes, warps, day transitions, save unload,
  return to title, disconnect, and local-player removal.
- Add adapters around mutable `FishingRod` and `BobberBar` members.

Exit criteria:

- Two local screens can hold independent assistant state.
- Interrupted or unexpected game states recover to Idle without corrupting a rod or
  blocking player input.
- State transitions can be tested without running the game where practical.

## Milestone 4: Core automation parity

Goal: restore the primary Fishing Assistant experience with safe behavior.

- Toggle automation per local player.
- Automatic casting with configurable cast power and delay.
- Automatic hooking when a fish bites.
- Automatic minigame control with optional treasure targeting.
- Optional minigame skipping, including caught-only behavior.
- Automatic catch-popup advancement.
- Automatic fishing-treasure collection with safe inventory-full handling.
- Stop immediately on player cancellation, invalid water, menu conflicts, tool changes,
  or unsupported event/festival states.

Compatibility cases:

- Tutorial first catch, fish ponds, legendary fish, festival minigames, secret notes,
  trash catches, Wild Bait, Challenge Bait, Deluxe Bait, and golden treasure.
- Inventory outcomes: all items fit, partial stack fits, nothing fits, cursor holding an
  item, discard/drop policies, and manual takeover.

Exit criteria:

- The complete cast-to-catch loop works repeatedly without simulated stuck input.
- Each automation stage can be disabled independently.
- Manual fishing still works normally while automation is disabled.

## Milestone 5: Equipment and player safety

Goal: restore convenience features without mutating inventory or network state unsafely.

- Automatically attach bait and tackle using explicit preference rules.
- Support both tackle slots on the Advanced Iridium Rod.
- Handle infinite bait/tackle behavior without duplicating or persisting temporary
  items.
- Reassess spawned bait/tackle: default it off, label it clearly, and keep it local to
  the owning player's inventory.
- Add optional food selection and eating based on energy thresholds and exclusions.
- Add stop-time warnings and safe pausing behavior.
- Stop before a cast would exhaust stamina when configured.
- Rework optional rod enchantments so ownership, removal, saves, and multiplayer sync
  are predictable.

Exit criteria:

- Equipment and food decisions never consume another player's items.
- Saving, disconnecting, unequipping, and disabling the mod leave the rod and inventory
  in a valid state.

## Milestone 6: Catch rules, assistance, and HUD

Goal: restore advanced customization and make the assistant's decisions visible.

- Fish difficulty multiplier and additive adjustment.
- Bite timing, treasure chance, golden treasure, perfect catch, fish quality, fish size,
  and multi-catch rules.
- Fish preview with caught/uncaught and legendary visibility rules.
- Visual HUD status for automation, pause reason, and exceptional runtime state.
  Treasure targeting also has an optional unbound-by-default toggle keybind; a small
  Treasure Hunter tackle icon appears beside the rod HUD while targeting is on.
- Responsive HUD placement that respects toolbar position, UI scale, menus, festivals,
  and each split-screen viewport.
- Starter rod and auto-trash features, redesigned with explicit safeguards and item ID
  validation.

Exit criteria:

- Catch modifiers match documented behavior for Stardew Valley 1.6.15 mechanics.
- HUD information belongs to the correct local player and never renders into another
  split-screen viewport.
- Potentially destructive options are opt-in and clearly explained.

## Milestone 7: Multiplayer and split-screen hardening

The preparatory remediation and local-safety baseline are complete. The remaining
festival, special-catch, remote reconnect, and mixed-topology work is maintained in
the actionable [release-candidate verification plan](RELEASE-CANDIDATE.md), rather
than in a second historical checklist.

Goal: validate every retained feature in all supported play modes rather than treating
multiplayer as a late compatibility patch.

- Classify and document every feature as local-only, per-player, synchronized, or
  host-authoritative.
- Prevent temporary attachment swaps or local automation state from being persisted by
  the host.
- Add SMAPI multiplayer messages only where shared state truly requires them.
- Ignore or reject messages from incompatible versions and unintended senders.
- Test host, farmhand, and two simultaneous local players fishing independently.
- Test mixed topology: split-screen players on a multiplayer host or farmhand machine.
- Test join, disconnect, reconnect, day end, save, return to title, and mod disable
  during every major fishing state.

Exit criteria:

- One player's input, menu, HUD, inventory, rod, or automation never controls another
  player.
- No temporary assistant state leaks into the save or survives session teardown.
- Unsupported multiplayer behavior is visibly disabled with a reason.

## Milestone 8: New Fishing Assistant 3 features

Goal: add new value only after parity behavior is stable.

Implemented for the first beta:

- Treasure Chest Ignore List: visually select rewards that automatic treasure looting
  leaves behind, then choose whether an ignored-only chest stays open, drops its
  contents, or discards them. This is the first Milestone 8 feature.
- Automatic Bubble Steering: steer manual and automatic casts left or right toward a
  reachable fishing bubble. It must never move the player, extend the cast, or change
  its configured or manually selected power. This follows the treasure ignore list.
- Named automation profiles: Relaxed automates the whole loop, Training leaves the
  minigame to the player, Manual+ enables preview and quality-of-life assistance, and
  Custom preserves individually edited values. This follows bubble steering so the
  new automation option can be included in the profile mapping once.

Deferred candidates for a later version:

The following gameplay-assistance ideas are exploratory only. They are not committed
scope: a future version may implement all of them, selected parts, a different design
that serves the same need, or none of them after compatibility and UX review.

- Minigame assist tuning: independently adjust fish movement, catch-progress gain and
  loss, and treasure-catch speed without taking over the minigame.
- Bubble marker and steering range: show whether the current cast can reach a fishing
  bubble, and optionally offer a wider sideways steering mode without extending cast
  distance.
- Bait-aware multi-catch: offer vanilla-flavored double-catch rules tied to Wild Bait
  or an opt-in chance on other bait, alongside the existing explicit fish-count choice.
- Fish-condition assistance: explore season/location bypass and rare-fish assistance.
  This requires a multiplayer-safe design that does not temporarily mutate
  network-synchronized rod attachments.
- Context-aware pause rules for stamina, time, inventory capacity, weather, festivals,
  player movement, and nearby hazards.
- Session statistics: casts, catches, perfect catches, treasure, time spent, and stop
  reasons, stored locally and resettable.
- Accessible minigame assistance levels between fully manual and fully automatic.
- A compact on-screen control panel for changing the most common runtime toggles without
  opening the full configuration menu.
- Ordered bait and tackle preferences: replace each single-item selector with a
  reorderable multi-item list. Automatic attachment should try entries from top to
  bottom and use the first available eligible item; spawning, when enabled, should
  follow the same priority order without falling through to a lower choice first.
- Batched junk disposal was selected for the 3.1 development line. The player can keep
  the original immediate behavior or retain junk until the inventory becomes full, then
  clear all eligible Junk List stacks with one sound and one summary notification. The
  full-inventory mode uses SMAPI inventory events and requires no inventory-add patch.
- The 3.1 development line also adds an optional one-way Treasure Chest integration:
  Junk List items can be treated as ignored treasure without copying or synchronizing
  entries between the two visual editors.

After the current release-candidate verification pass is complete, the owner will
revisit these candidates for the next Milestone 8 iteration. They remain exploratory:
each may be selected, redesigned, or declined after compatibility and UX review.

## Milestone 9: Beta, release candidate, and publication

Goal: turn the feature-complete build into a supportable public release.

The actionable promotion gate is maintained in
[RELEASE-CANDIDATE.md](RELEASE-CANDIDATE.md).

- Freeze configuration keys and document migration from version 2.
- Complete English source text and review carried-forward translations; mark outdated
  translations rather than presenting them as verified.
- Write player documentation, troubleshooting, compatibility notes, and a complete
  configuration reference.
- Add release packaging, manifest validation, update-key validation, and clean-install
  testing.
- Test upgrade, downgrade warning behavior, uninstall, and reinstall against copied
  saves.
- Run the full manual release matrix and inspect SMAPI logs for every scenario.
- Publish a release candidate for opt-in testing before the final release.
- Merge the reviewed release PR from `development` into `main`, tag `v3.0.0`, and
  publish the release artifacts.

Exit criteria:

- No known save corruption, item loss/duplication, cross-player state leak, crash, or
  automation lockup.
- All release-blocking tests pass on single-player, local split-screen, multiplayer
  host, and multiplayer farmhand.
- Documentation accurately reflects shipped behavior and known limitations.

## Fishing Assistant 2 feature disposition

This is the initial planning disposition, not a promise to reproduce every legacy
implementation detail.

| Legacy area | Version 3 direction |
| --- | --- |
| Auto cast, hook, minigame, popup close | Reimplement as the core state-machine workflow. |
| Treasure targeting and auto-loot | Keep with safer menu and inventory handling. |
| Auto pause by time | Keep and expand into context-aware stop rules. |
| Auto eat | Keep with deterministic selection and per-player inventory ownership. |
| Auto attach bait/tackle | Keep with 1.6.15 rods, two tackle slots, and multiplayer safety. |
| Spawn bait/tackle | Reassess as an opt-in convenience/cheat setting. |
| Skip minigame and instant bite | Keep with explicit compatibility tests. |
| Quality, size, difficulty, perfect, multi-catch | Keep behind isolated catch-rule services. |
| Treasure/golden treasure chance | Keep while preserving vanilla mastery behavior. |
| Fish preview and status HUD | Keep and rebuild as responsive per-screen UI. |
| Starter rod | Reassess to avoid progression and multiplayer surprises. |
| Infinite bait/tackle | Keep only with safe ownership and save behavior. |
| Temporary enchantments | Rework or defer until lifecycle behavior is proven safe. |
| Auto trash | Rework as opt-in with previewable rules and recovery safeguards. |
| Generic Mod Config Menu | Remove; replace with the custom configuration menu. |

## Release test matrix

Every release candidate must cover at least:

| Mode | Required scenarios |
| --- | --- |
| Single-player | Manual, partial automation, full automation, menus, save/reload, day end. |
| Local split-screen | Each player alone, both fishing simultaneously, independent input/HUD/config. |
| Multiplayer host | Host fishing, farmhand fishing, simultaneous fishing, shared-world changes. |
| Multiplayer farmhand | Join/reconnect/disconnect, latency, host save while fishing, inventory ownership. |
| Mixed split-screen multiplayer | Multiple local players connected to a remote or local host where feasible. |

Cross-cutting variants include keyboard/mouse, controller, UI scale, zoom, supported
languages, festivals, fish ponds, tutorial fishing, full inventory, low stamina, late
night, and all supported bait/tackle mechanics.

## Definition of done for a feature PR

- Behavior and ownership scope are documented.
- Configuration is typed, validated, translated, and available in the custom menu.
- Relevant pure logic has automated tests.
- Cancellation, teardown, and unexpected-state recovery are implemented.
- Single-player behavior is manually verified.
- Split-screen and multiplayer impact is tested or explicitly deferred with the feature
  disabled in those contexts.
- Build passes with no new warnings, generated/reference files are excluded, and the PR
  explains user-visible behavior and migration impact.
