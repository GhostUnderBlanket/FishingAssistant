# Known Issues

These issues are intentionally deferred so development can continue. Re-test them before
the version 3 release candidate.

They remain open in `3.0.0-beta.1`; the alpha build references below record where they
were originally reproduced.

## Local co-op Fish Preview offset

- **Status:** Open; reproduced in local co-op on `3.0.0-alpha.1` (`c337268`).
- **Symptom:** Fish Preview is rendered with a larger-than-normal offset from the fishing
  minigame bar in split-screen. The offset differs from the expected single-player
  placement even after converting between the game and UI viewport coordinate spaces.
- **Follow-up:** Inspect the render-target/UI-scale transform active during
  `RenderedActiveMenu`, then test horizontal and vertical split-screen layouts at several
  UI scale and zoom values.

## Mouse and controller keybind capture does not persist

- **Status:** Open; reproduced with a controller on `3.0.0-alpha.1` (`c337268`) and
  subsequently confirmed with mouse input during beta testing.
- **Symptom:** Activating a keybind control with controller A or a mouse click briefly
  shows `Press a key or button...`, but the control immediately returns to the previous
  binding (for example `F5`) instead of waiting for or saving the next input.
- **Follow-up:** Separate every activation input from the capture frame, require its
  release before accepting a binding, and verify SMAPI `ButtonsChanged`, mouse events,
  and Vanilla menu input order for each local split-screen instance.
