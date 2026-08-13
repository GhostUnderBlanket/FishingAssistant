# Pre-Milestone 7 Remediation Checklist

This document records the gaps found while auditing Milestones 1 through 6. Complete
this checklist before beginning Milestone 7 so multiplayer hardening starts from a
known, recoverable single-player and split-screen baseline.

Audit baseline:

- Reviewed on 2026-08-13 at commit `5159886` on `development`.
- Release build succeeded.
- Automated tests passed: 259 passed, 0 failed, 0 skipped.
- Passing automated tests do not replace the in-game checks recorded in
  [TESTING.md](TESTING.md).

## Completion rules

- A checkbox may be completed only when the implementation, focused automated tests,
  and relevant documentation are committed together.
- In-game checks must be recorded in [TESTING.md](TESTING.md), including game version,
  SMAPI version, player topology, and any remaining limitation.
- A deliberate change to an earlier roadmap requirement is acceptable only when the
  replacement behavior and reason are documented in [ROADMAP.md](ROADMAP.md).
- Do not restore the retired treasure-targeting hotkey or treasure-targeting HUD icon.
  Treasure targeting remains a config-menu-only setting.

## Milestone 1: Configuration and migration

- [x] Document that the Fishing Assistant 2 `JunkHighestPrice` setting is retired and
  replaced by the visual Junk List/Junk Ignore List editor.
- [x] Add a complete Fishing Assistant 2 migration fixture covering every legacy
  property, not only the representative enum and keybind subset.
- [x] Verify the fixture preserves every still-supported user choice and deliberately
  reports every retired setting.
- [x] Preserve enough information about unknown legacy properties to produce a useful,
  bounded migration report while redacting credential-like property names.
- [x] Confirm malformed and future-schema configuration files still activate safe
  defaults/read-only behavior without overwriting the original file.

Milestone 1 is cleared when a full V2 fixture has deterministic documented results and
all migration, malformed-file, unknown-value, and future-schema tests pass.

Completed on 2026-08-13. Release verification passed with 265 automated tests.

## Milestone 2: Custom configuration menu

- [x] Add deliberate confirmation before Reset Defaults changes the menu draft.
- [x] Surface actionable validation warnings inline beneath their affected controls;
  visual controls constrain values so Apply-time corrections aren't normally needed.
- [ ] Add a reusable disabled state and unavailable-reason presentation for controls
  whose behavior is unsafe or unsupported in the current context.
- [ ] Support gamepad trigger category navigation in addition to shoulder buttons.
- [ ] Rebuild layout, clickable bounds, and snappy-navigation links after locale,
  effective UI scale, and option-visibility changes.
- [ ] Add reusable section-header and description-block controls, or document the
  deliberate replacement if the final menu design no longer needs them.
- [ ] Add focused tests for Reset confirmation, Apply validation feedback, Cancel draft
  isolation, disabled controls, and navigation rebuilding.
- [ ] Verify the complete menu with mouse, keyboard, and controller in fullscreen and a
  small local split-screen viewport. Include long translated labels and non-default UI
  scale.

All 55 user-editable configuration properties are already represented in the menu;
`ConfigVersion` is internal and must not be exposed as an editable option.

## Milestone 3: Runtime context and state recovery

- [ ] Make every retained automation state meaningful and reachable. In particular,
  implement `Cooldown` and `Faulted` transitions or remove them and update the roadmap
  and HUD contract.
- [ ] Define bounded timeouts for states that can otherwise remain stuck after an
  unexpected game transition.
- [ ] Add an explicit cancellation path which stops pending automatic actions and
  returns the rod/player input state to vanilla control.
- [ ] Reset or safely transfer per-screen state on menu interruption, save unload,
  peer disconnect, local-player removal, and every existing warp/day/title/tool-change
  path.
- [ ] Add event routing for lifecycle cases not currently observed, including remote
  peer disconnection where service behavior depends on remote-player presence.
- [ ] Add recovery tests for cancellation during cast timing, unexpected menus, timeout,
  tool replacement, warp, save/load, return to title, disconnect, and screen removal.
- [ ] Verify two local split-screen players can enable, disable, interrupt, and resume
  independent automation sessions without sharing state or input.

Milestone 3 is cleared when every state has a documented entry/exit path, interrupted
sessions recover without stuck input, and the two-screen isolation smoke test passes.

## Milestone 4: Core automation parity

- [ ] Verify disabling automation or manually cancelling during every cast-to-catch
  stage stops further automatic actions immediately and safely.
- [ ] Add runtime-level integration coverage around the pure decision policies so event
  ordering and adapter mutations are tested together where practical.
- [ ] Complete repeated cast, bite, hook, minigame, popup, and treasure-loot loops with
  every automation stage independently disabled.
- [ ] Complete the compatibility cases for tutorial catches, fish ponds, legendary
  fish, supported fishing festivals, secret notes, trash, Wild/Deluxe/Challenge Bait,
  golden treasure, and every inventory-full outcome.
- [ ] Confirm manual fishing remains under vanilla control when automation is disabled.

Record all in-game results in [TESTING.md](TESTING.md). A policy unit test alone does
not complete a compatibility case.

## Milestone 5: Equipment and player safety

- [ ] Verify bait and tackle attachment changes only the owning local player's rod and
  inventory, including both Advanced Iridium Rod tackle slots.
- [ ] Verify infinite attachments restore correctly after consumption, option disable,
  unequip, warp, day end, save/reload, disconnect, and return to title.
- [ ] Verify spawned bait/tackle remains opt-in and is never added to another player's
  inventory.
- [ ] Verify automatic eating respects exclusions and never consumes another player's
  item.
- [ ] Verify late-night and low-energy stops finish the current safe boundary and leave
  only the owning player's automation disabled.
- [ ] Verify temporary enchantments are removed before persistence and recover
  predictably after save, reconnect, remote-peer disconnect, unequip, and option
  disable.
- [ ] Add missing lifecycle and ownership tests discovered by those scenarios.

Milestone 5 is cleared when save files contain no temporary attachment/enchantment
state and disconnecting or disabling the mod leaves every affected rod valid.

## Milestone 6: Catch rules, preview, and HUD

- [ ] Make the visual HUD communicate the pause reason and exceptional runtime state.
  `AutomationSession.LastReason` currently exists but is not consumed by the renderer.
- [ ] Ensure low-energy and late-night pauses have a distinct visual result instead of
  becoming indistinguishable from ordinary disabled/idle state.
- [ ] Define and test HUD behavior while menus are open and at non-default UI scales,
  zoom levels, festivals, small windows, and each split-screen viewport.
- [ ] Add Fiberglass Rod and Iridium Rod to the starter-rod picker, or document why only
  Training Rod, Bamboo Pole, and Advanced Iridium Rod are supported.
- [ ] Correct stale testing text which still calls the visual panel an
  "automation/treasure status HUD" after the treasure icon was retired.
- [ ] Verify difficulty, bite timing, treasure chances, perfect catch, quality, size,
  multi-catch, fish preview, starter rod, and auto-trash behavior in Stardew Valley
  1.6.15.
- [ ] Verify potentially destructive behavior remains opt-in and clearly explained in
  the configuration menu.

Milestone 6 is cleared when catch modifiers match documented 1.6.15 behavior and HUD,
preview, starter-rod, and auto-trash checks pass for the owning local player.

## Gate before Milestone 7

Do not begin Milestone 7 until all of the following are true:

- [ ] Every checklist item above is complete or has an approved documented replacement.
- [ ] Release build and the full automated test suite pass with no new warnings.
- [ ] The ordinary single-player cast-to-catch loop passes repeatedly with automation
  on, automation off, and each stage disabled independently.
- [ ] Festival fishing checks pass without affecting unrelated festival events.
- [ ] A two-player local split-screen isolation smoke test passes.
- [ ] Saving, reloading, returning to title, and reconnecting leave no stale automation,
  attachment, enchantment, menu, preview, or HUD state.
- [ ] [TESTING.md](TESTING.md) accurately distinguishes completed checks from deferred
  Milestone 7 multiplayer-host, farmhand, reconnect, simultaneous-fishing, and mixed-
  topology hardening work.
