# Testing Status

This file records manual verification which has actually been completed. Automated
tests and successful builds do not count as in-game verification.

## Current manual-test baseline

Status recorded on 2026-08-12:

- All in-game testing completed so far has been single-player only.
- Fishing behavior inside festivals and festival fishing minigames has not been tested
  in-game yet.
- Treasure targeting and the treasure chance overrides have not been tested in-game
  yet. `TreasureChance` is now connected to gameplay, so the deferred repeatable test
  can use `Always` to produce a treasure target.
- Local split-screen/co-op has not been tested yet.
- Multiplayer host, farmhand, reconnect, and simultaneous-fishing scenarios have not
  been tested yet.
- Mixed split-screen multiplayer has not been tested yet.

The festival support and multiplayer-safe architecture currently present in the code
must therefore be treated as implemented but manually unverified until the matching
release-matrix scenarios are completed.

## Deferred manual checks

- Verify automatic casting and hooking in each Stardew Valley fishing festival
  minigame, including countdown, active play, timeout, results, and exit behavior.
- Confirm unrelated festival events never start fishing automation.
- With `TreasureChance` set to `Always`, verify the F6 treasure-targeting toggle, HUD
  status, start/abandon progress thresholds, completed treasure capture, and fallback
  to fish tracking when no treasure is available. Also verify `Default` and `Never`,
  plus all three golden-treasure modes.
- Repeat the normal cast-and-hook loop in local split-screen with each player alone and
  both players fishing simultaneously.
- Repeat the normal and festival fishing checks as multiplayer host and farmhand.
- Inspect the SMAPI log after each scenario for errors, recoveries, duplicated input,
  or cross-player state leakage.

Update this file only after the corresponding behavior has been observed in-game.
