# Test Record

This file records in-game behavior that has actually been observed. A successful build
or automated test does not count as manual verification. Remaining release-candidate
work is maintained in [RELEASE-CANDIDATE.md](RELEASE-CANDIDATE.md).

## Current baseline

- **Game:** Stardew Valley 1.6.15
- **SMAPI:** 4.5.2
- **Current beta line:** 3.0.0-beta.2
- **Automated verification:** 478 tests passed after the current performance and
  maximum-fish-size fixes.

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
  coverage. The corrected maximum-fish-size boundary still needs its targeted RC check.

## Still unverified in-game

- Remote multiplayer: host, farmhand, reconnect, simultaneous fishing, and mixed
  split-screen/remote topology.
- The complete Ice Fishing Festival automation loop and the guarantee that unrelated
  festivals do not start fishing automation.
- The exhaustive special-catch and inventory matrix: tutorial, fish pond, legendary,
  first catch, record, secret note, Wild/Deluxe/Challenge Bait, golden treasure,
  partial stacks, and every full-inventory action.
- Targeted re-checks for the latest maximum-fish-size fix, treasure targeting and
  golden/instant treasure combinations, skip-minigame combinations, bubble steering,
  and temporary-enchantment behavior with a remote player connected.

## Test utilities

- **Create Test Fishing Bubble:** Debug page or `fa_bubble`. Creates a reachable bubble
  beside the selected cast landing point for Bubble Steering tests.
- **Prepare Ice Fishing Festival:** Debug page or `fa_ice_festival`. Host-only setup
  using Stardew Valley's debug command. It changes the date, time, season, and location;
  use a disposable save and do not save afterward.
- **Prepare Stardew Valley Fair:** Debug page or `fa_stardew_valley_fair`. Host-only
  setup under the same disposable-save rule, for the FishingGame festival minigame.

## Recording rule

When a release-candidate row passes, record the game, SMAPI, mod version, commit,
topology, configuration, and SMAPI-log result in the matching entry of
[RELEASE-CANDIDATE.md](RELEASE-CANDIDATE.md). Do not mark a row complete based only on
automated tests or a related scenario.
