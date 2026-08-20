# Fishing Assistant 3 Release-Candidate Verification Plan

Complete every unchecked row before promoting the current beta line to `3.0.0-rc.1`.
The local baseline and previous verified behavior are recorded in
[TESTING.md](TESTING.md). Every completed row must record the game, SMAPI, mod version,
commit, player topology, relevant configuration, and SMAPI-log result.

## 1. Remote multiplayer safety

Run these with one host and one remote farmhand. Where hardware permits, repeat the
critical rows with split-screen on either host or farmhand.

- [ ] Run Manual+, Training, Custom, and Relaxed automation independently and
  simultaneously. Confirm input, HUD, preview, config, rod, inventory, and result stay
  with the owning player.
- [ ] Apply visibly different profiles, reconnect both players, reload the save, and
  confirm neither player's Apply action or profile changes the other.
- [ ] Verify bait/tackle attachment, treasure collection, automatic eating, junk
  disposal, starter rod, and catch modifiers never change another player's inventory or
  result.
- [ ] Connect a remote player while temporary enchantments are active. Confirm they are
  removed, remain unavailable while connected, and are never written into the save.
- [ ] Cover join, disconnect, reconnect, host save, day end, warp, tool swap, and return
  to title during idle, cast, minigame, catch-popup, and treasure-menu states.
- [ ] Inspect the SMAPI log after each topology for errors, repeated recovery, duplicated
  input, stale per-screen state, item loss, or duplication.

## 2. Festival and high-risk gameplay

- [ ] On a disposable host test save, use Debug > Prepare Ice Fishing Festival or
  `fa_ice_festival`. Complete a full automated cast-to-score loop, then confirm an
  unrelated festival never starts fishing automation. Do not save the date-jumped test
  game.
- [ ] With normal and golden treasure chance set to `Always`, test treasure targeting,
  instant treasure, the ignore list, partial-stack collection, and each full-inventory
  Stop/Drop/Discard action without item loss or duplication.
- [ ] Test `SkipAll` and `SkipOnlyCaught` with first catches, legendary fish, fish ponds,
  Wild/Deluxe/Challenge Bait, treasure targeting on/off, and the fishing festival.
  Confirm festival and fish-pond result rules remain Vanilla.
- [ ] Test catch modifiers separately and together: difficulty, instant bite, perfect
  catch, every quality, corrected maximum and reduced fish size, and counts 1–3. Cover
  trash, escape, first catch, fish pond, legendary fish, Wild Bait, Challenge Bait, and
  festival fishing.
- [ ] Test Infinite Bait/Tackle and all temporary enchantments across catch, escape,
  save/reload, day end, warp, tool swap, disconnect, and return to title. Confirm no
  temporary attachment or enchantment leaks into a save.
- [ ] Use `fa_bubble` to verify Bubble Steering for manual and automatic casts at several
  cast powers. It must not move the player, extend cast range, or select a different
  power.
- [ ] Run the complete cast-to-catch loop repeatedly with every automation stage toggled
  independently. Confirm no stuck input, reel audio, rod state, or uncontrolled recast.

## 3. Release readiness

- [ ] Freeze the schema and review the Fishing Assistant 2 migration, future-schema
  read-only handling, and upgrade from both Fishing Assistant 2 and the latest beta.
- [ ] Review English menu text, inline warnings, command help, README, configuration
  reference, compatibility notes, and limitations. Review Thai coverage separately.
- [ ] Validate manifest version, unique ID, minimum SMAPI version, Nexus update key, and
  clean-install behavior.
- [ ] Enable deterministic release ZIP packaging containing only the DLL, resolved
  manifest, translations, and required assets.
- [ ] Test copied-save uninstall and reinstall, then add CI for restore, tests, Release
  build, and package inspection without proprietary game files.

## RC promotion

- [ ] The full automated suite and a Release build pass with zero warnings.
- [ ] No open issue can cause a crash, save corruption, item loss/duplication,
  cross-player control, or unrecoverable automation state.
- [ ] Update the version to `3.0.0-rc.1`, build the release artifact, and publish it for
  opt-in testing.
