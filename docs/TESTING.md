# Test Record

This file records in-game behavior that has actually been observed. A successful build
or automated test does not count as manual verification. Remaining release-candidate
work is maintained in [RELEASE-CANDIDATE.md](RELEASE-CANDIDATE.md).

## Current baseline

- **Game:** Stardew Valley 1.6.15
- **SMAPI:** 4.5.2
- **Current release-candidate build:** 3.0.0-rc.1
- **Current post-RC development build:** 3.1.0-alpha.1; its new Junk List integrations
  have completed automated and in-game verification.
- **RC automated verification:** 478 tests passed for the published RC baseline.
- **Post-RC automated verification:** 489 tests pass in both Debug and Release for the
  3.1.0-alpha.1 development build, including the new policy and per-screen cache cases.
- **RC gameplay pass:** 2026-08-21, commit `f5ab2d7`, using the single-player,
  local split-screen, remote host, and remote farmhand test instances. The SMAPI logs
  were inspected without finding errors, repeated recovery, duplicated input, stale
  per-screen state, item loss, or duplication. All 13 gameplay rows in sections 1 and 2
  of the release-candidate plan are complete.
- **RC readiness review:** Configuration schema 13 and its migration paths, English and
  Thai text, manifest metadata, clean installation, copied-save uninstall/reinstall,
  the automated suite, and the zero-warning Release build have passed review. No open
  issue is known to cross the RC safety gate.
- **RC publication:** The inspected `3.0.0-rc.1` artifact was published for opt-in
  testing after every release-candidate checklist row passed.

## Verified locally

- The normal single-player and two-player local split-screen cast-to-catch loop works
  with every core automation stage independently enabled or disabled.
- Disabling automation during a cast lets the Vanilla cast finish while preventing later
  assistant actions; it no longer leaves reel audio or pending automation stuck.
- Each local player has independent automation state, HUD, Fish Preview, and persisted
  configuration. Applying one player's draft does not change the other player's profile.
- The custom configuration UI has been exercised with mouse, keyboard, and controller,
  including split-screen navigation, keybind capture, item pickers, and localized text.
- Fish Preview placement is verified in local co-op after the world-coordinate fix; the
  Sonar-style preview also scales with zoom and avoids Challenge Bait overlap.
- Bait/tackle attachment and spawning, auto-eat, auto-trash, starter rod, late-night,
  low-energy protection, infinite attachments, and temporary enchantment cleanup are
  verified for the owning local player in single-player and local split-screen.
- Fish difficulty, bite timing, treasure chance, perfect catch, quality, size,
  multi-catch, Fish Preview, starter rod, and auto-trash have received local in-game
  coverage. The corrected maximum-fish-size boundary passed its targeted RC check.

## Verified during the release-candidate pass

- Remote host and farmhand play passed with independent Manual+, Training, Custom, and
  Relaxed profiles. Simultaneous fishing, reconnect, save reload, lifecycle changes,
  inventory ownership, HUD, preview, input, and per-player configuration remained
  isolated.
- Bait/tackle attachment, treasure collection, automatic eating, junk disposal,
  starter rods, catch modifiers, infinite attachments, and temporary enchantments did
  not affect the other player or leak temporary state into the save.
- The complete Ice Fishing Festival and Stardew Valley Fair fishing-minigame automation
  loops passed, including HUD, automatic casting and hooking, scoring, and cleanup.
  Unrelated festivals did not start fishing automation. Festival and fish-pond result
  rules remained Vanilla where required.
- Treasure targeting, instant normal and golden treasure, the treasure ignore list,
  partial stacks, and the Stop/Drop/Discard full-inventory behaviors passed without
  item loss or duplication.
- Skip-minigame modes and catch modifiers passed separately and in combination across
  first catches, legendary fish, fish ponds, trash, escape, Wild Bait, Deluxe Bait,
  Challenge Bait, treasure targeting, and festival fishing. Fish quality, corrected
  maximum and reduced size, and catch counts 1-3 produced the expected results.
- Bubble Steering passed for both manual and automatic casts at multiple cast powers.
  It did not move the player, extend cast range, or select a different cast power.
- Repeated full cast-to-catch loops passed with every automation stage toggled
  independently, without stuck input, reel audio, rod state, or uncontrolled recasting.

## Verified release readiness

- Configuration schema 13 is frozen. Migration from Fishing Assistant 2 and the latest
  beta passed review, and future-schema profiles remained protected by read-only
  handling.
- English and Thai menu text, inline warnings, command help, README, configuration
  reference, compatibility notes, and documented limitations were reviewed.
- The manifest version behavior, preserved unique ID, minimum SMAPI version, Nexus
  update key, and clean-install behavior were validated.
- Deterministic release packaging produced the same SHA-256 hash across repeated runs.
  The inspected ZIP contains only `FishingAssistant.dll`, the resolved manifest, and
  the English and Thai translations; every entry has a fixed timestamp.
- Copied-save uninstall and reinstall testing passed without save damage or stale mod
  state.
- The complete automated suite and Release build passed with zero warnings. The RC
  safety review found no open issue that can cause a crash, save corruption, item loss
  or duplication, cross-player control, or an unrecoverable automation state.

## Release-candidate promotion

- No gameplay verification row remains open in sections 1 and 2 of the
  release-candidate plan.
- All release-readiness and promotion rows are complete, and the inspected
  `3.0.0-rc.1` artifact is available for opt-in testing.

## Test utilities

- **Create Test Fishing Bubble:** Debug page or `fa_bubble`. Creates a reachable bubble
  beside the selected cast landing point for Bubble Steering tests.
- **Prepare Ice Fishing Festival:** Debug page or `fa_ice_festival`. Host-only setup
  using Stardew Valley's debug command. It changes the date, time, season, and location;
  use a disposable save and do not save afterward.
- **Prepare Stardew Valley Fair:** Debug page or `fa_stardew_valley_fair`. Host-only
  setup under the same disposable-save rule, for the FishingGame festival minigame.

## Verified 3.1 development features

- [x] With `Ignore Junk List items in treasure chests` enabled, verify ordinary and
  golden treasure leave Junk List items in the chest while collecting other rewards.
- [x] Verify disabling that option restores the explicit Treasure Chest Ignore List as
  the only source, and that edits remain isolated between local split-screen profiles.
- [x] Verify Junk Disposal Off retains all junk and Immediately removes only newly
  acquired quantities with the established behavior.
- [x] Verify When Inventory Is Full retains junk while space remains, then removes all
  eligible Junk List stacks together after the last slot is filled.
- [x] Verify one batch produces one sound and one summary message, applies Vanilla
  trash-can reclamation to every discarded stack, and leaves non-trashable items and
  protected fish untouched.
- [x] Verify applying the full-inventory mode or enabling automation while already full
  performs one batch cleanup, with split-screen and remote players remaining isolated.

## Recording rule

When a release-candidate row passes, record the game, SMAPI, mod version, commit,
topology, configuration, and SMAPI-log result in the matching entry of
[RELEASE-CANDIDATE.md](RELEASE-CANDIDATE.md). Do not mark a row complete based only on
automated tests or a related scenario.
