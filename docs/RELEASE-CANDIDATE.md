# Fishing Assistant 3 Release-Candidate Gate

This checklist defines the work required to promote `3.0.0-beta.1` to
`3.0.0-rc.1`. Version 3.0 is feature-frozen: new gameplay ideas move to 3.1 unless a
change is required to prevent crashes, item loss or duplication, cross-player state
leaks, stuck automation, or a misleading release experience.

## 1. Resolve release-blocking known issues

- [ ] Fix mouse and controller keybind capture so the activation input is ignored until
  released and the next keyboard, mouse, or controller input persists for the correct
  local player.
- [ ] Fix Fish Preview placement in horizontal and vertical local split-screen across
  representative UI-scale and zoom settings.
- [ ] Re-test both fixes with keyboard/mouse and controller and update
  [KNOWN-ISSUES.md](KNOWN-ISSUES.md).

## 2. Multiplayer safety gate

Test with one host and one remote farmhand. Add mixed local/remote split-screen only
where the available hardware permits it.

- [ ] Run manual, partial, and Relaxed automation for host and farmhand separately and
  simultaneously. Input, HUD, preview, config, rod, and inventory must remain owned by
  the correct player.
- [ ] Verify player-specific config survives reconnect and neither player's Apply
  action changes the other player.
- [ ] Verify bait/tackle attachment, treasure collection, automatic eating, junk
  disposal, starter rod, and catch modifiers never change another player's inventory
  or result.
- [ ] Verify temporary enchantments disable safely while a remote player is connected
  and are never written into a save.
- [ ] Test join, disconnect, reconnect, host save, day end, warp, tool swap, and return
  to title during representative idle, cast, minigame, catch popup, and treasure states.
- [ ] Inspect the SMAPI logs for errors, repeated recovery, duplicated input, or stale
  per-screen state.

## 3. High-risk gameplay matrix

The exhaustive combinations in [TESTING.md](TESTING.md) remain useful regression
coverage, but only these risk-based checks block the first RC:

- [ ] Supported fishing festival automation completes a full cast-to-score loop;
  unrelated festivals never start automation.
- [ ] Treasure targeting, normal/golden chance overrides, instant treasure, ignore
  list, partial-stack collection, and full-inventory Stop/Drop/Discard outcomes behave
  without item loss or duplication.
- [ ] Skip-minigame and catch modifiers preserve Vanilla rules for first catch,
  legendary fish, Wild Bait, Challenge Bait, fish ponds, and festivals.
- [ ] Infinite attachments and temporary enchantments remain safe across catch,
  escape, save/reload, day end, tool swap, warp, disconnect, and return to title.
- [ ] Automatic Bubble Steering works for manual and automatic casts without extending
  cast range or moving the player.
- [ ] The complete cast-to-catch loop runs repeatedly without stuck input, audio, rod
  state, or uncontrolled recasting.

## 4. Release engineering and documentation

- [ ] Freeze and document the configuration schema and Fishing Assistant 2 migration.
- [ ] Review all English menu text, inline warnings, command help, README, configuration
  reference, troubleshooting, compatibility notes, and known limitations.
- [ ] Enable deterministic release ZIP packaging containing only the DLL, resolved
  manifest, translations, and required assets.
- [ ] Validate the manifest version, unique ID, minimum SMAPI version, Nexus update key,
  and clean-install load behavior.
- [ ] Test upgrade from Fishing Assistant 2 and the latest beta, future-schema
  read-only protection, uninstall, and reinstall using copied saves.
- [ ] Add automated CI validation for restore, tests, Release build, and package
  inspection without requiring proprietary game files in the repository.

## RC promotion

- [ ] All automated tests and the Release build pass with zero warnings.
- [ ] No open issue can cause save corruption, item loss or duplication,
  cross-player control, a crash, or an unrecoverable automation state.
- [ ] Update the version to `3.0.0-rc.1`, move the changelog entry, build the release
  artifact, and publish it for opt-in testing.
- [ ] Record the exact game, SMAPI, mod version, commit, topology, and test results in
  [TESTING.md](TESTING.md).
