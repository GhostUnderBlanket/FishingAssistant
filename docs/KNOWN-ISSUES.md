# Known Issues

Open issues must be re-tested before the version 3 release candidate. Resolved issues
remain recorded below as regression history.

## Local co-op Fish Preview offset

- **Status:** Resolved; verified in-game in local co-op on `3.0.0-beta.2`
  (`72eca69`) on 2026-08-16.
- **Symptom:** Fish Preview is rendered with a larger-than-normal offset from the fishing
  minigame bar in split-screen. The offset differs from the expected single-player
  placement even after converting between the game and UI viewport coordinate spaces.
- **Fix:** Fish Preview now renders in the same world-draw coordinate space as Vanilla's
  `BobberBar` instead of converting only its anchor into UI coordinates. The game now
  applies zoom, UI scale, and the active split-screen viewport consistently to both.
- **Regression coverage:** Repeat horizontal and vertical split-screen checks at several
  UI scale and zoom values before each release candidate.

## Mouse and controller keybind capture does not persist

- **Status:** Resolved and verified in-game with mouse and controller on
  `3.0.0-beta.1` (`ec87262`) on 2026-08-14.
- **Symptom:** Activating a keybind control with controller A or a mouse click briefly
  shows `Press a key or button...`, but the control immediately returns to the previous
  binding (for example `F5`) instead of waiting for or saving the next input.
- **Fix:** Mouse, keyboard, and controller inputs now share one SMAPI capture path. The
  capture gate ignores the activation input until every button is released, then accepts
  the next input or chord for the current local screen. The menu also persists its
  rebuilt layout signature so the next update tick does not cancel capture.
- **Regression coverage:** Retain automated activation-release tests and repeat capture
  checks in the release-candidate input matrix.
