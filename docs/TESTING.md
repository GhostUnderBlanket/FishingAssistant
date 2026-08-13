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
- Instant fishing-treasure capture has not been tested in-game yet.
- Automatic catch-popup closing has not been tested in-game yet.
- Instant fish bites have not been tested in-game yet.
- Automatic fishing-treasure collection has not been tested in-game yet.
- Fishing-minigame skipping has not been tested in-game yet.
- Automatic bait attachment and refill has not been tested in-game yet.
- Automatic tackle attachment has not been tested in-game yet.
- Infinite bait and tackle preservation has not been tested in-game yet.
- Automatic food selection and eating has not been tested in-game yet.
- Late-night warnings and safe automation pausing have not been tested in-game yet.
- Low-energy cast protection has not been tested in-game yet.
- Temporary rod enchantments have not been tested in-game yet.
- Fish difficulty multiplier and additive adjustment have not been tested in-game yet.
- Perfect catch, preferred base quality, maximum fish size, and preferred fish count
  modifiers have not been tested in-game yet.
- Fish preview visibility, responsive placement, and reveal options have not been
  tested in-game yet.
- Automatic junk disposal and its stack-delta safeguards have not been tested in-game
  yet.
- The visual automation status HUD and toolbar-relative placement have not
  been tested in-game yet.
- Local split-screen/co-op has not been tested yet.
- Multiplayer host, farmhand, reconnect, and simultaneous-fishing scenarios have not
  been tested yet.
- Mixed split-screen multiplayer has not been tested yet.

## Pending configuration-menu checks

The custom configuration menu's automated tests cover draft isolation, input mapping,
inline warnings, unavailable controls, and layout-context detection. The following
checks still need to be observed in-game before Milestone 2 can be marked complete:

- In a normal fullscreen single-player session, open the menu with F6 and verify every
  category, scrolling, Apply, Cancel, Reset confirmation, item picker, junk editor,
  inline warning, and disabled-control explanation with mouse and keyboard.
- Repeat with a controller: verify A/B, D-pad/left stick navigation, Left/Right
  Shoulder and Left/Right Trigger category navigation, selector adjustment, and
  keybind capture/cancel.
- Known regressions currently deferred: local co-op Fish Preview placement and controller
  keybind capture. See [KNOWN-ISSUES.md](KNOWN-ISSUES.md).
- While the menu is open, change UI scale, zoom, window size, and game language. Check
  that no option or snappy-navigation focus is left at stale bounds after the menu
  rebuilds.
- In a small local split-screen viewport, verify long localized labels, scrolling,
  tooltips, footer buttons, picker dialogs, and Reset confirmation remain reachable
  and inside that player's viewport.
- With a remote player connected, open Rod Enchantments and confirm every temporary
  enchantment option is visibly disabled, explains why, ignores edits, and becomes
  editable again after the remote player leaves.
- In local split-screen, confirm a D-pad press advances exactly one config option,
  Controller Back opens the menu for the controller-owning player, and Fish Preview
  stays beside that same player's BobberBar rather than another screen's viewport.

The festival support and multiplayer-safe architecture currently present in the code
must therefore be treated as implemented but manually unverified until the matching
release-matrix scenarios are completed.

## Deferred manual checks

### Automation cancellation and timeout recovery

- Disable automation while an assistant-started cast is charging or in flight. Confirm
  the cast is cancelled, the player can move, and no later hook/recast occurs.
- Open a blocking menu during an assistant-owned cast, hook attempt, or catch-popup
  close attempt. Confirm pending assistant work is cleared without cancelling a fishing
  action that was started manually.
- Leave an assistant-owned cast, hook, or catch-popup close in an unexpectedly stuck
  state. Confirm automation disables after its bounded timeout and vanilla input remains
  available.
- During automatic fishing, test tool replacement, warp, saving, return to title, and a
  remote peer disconnect. Confirm pending work does not resume from stale per-screen
  flags afterward.
- Verify automatic casting and hooking in each Stardew Valley fishing festival
  minigame, including countdown, active play, timeout, results, and exit behavior.
- Confirm unrelated festival events never start fishing automation.
- With `TreasureChance` set to `Always`, verify the saved treasure-targeting checkbox,
  start/abandon progress thresholds, completed treasure capture, and fallback to fish
  tracking when no treasure is available. Confirm there is no targeting hotkey or HUD
  icon. Also verify `Default` and `Never`, plus all three golden-treasure modes.
- With treasure chance set to `Always`, verify instant treasure captures normal and
  golden chests only after they fully appear, works with manual/automatic minigames and
  both skip modes, still requires successfully catching the fish to receive rewards,
  and does not alter festival results. Repeat with config targeting both on and off,
  then as each split-screen player, multiplayer host, and farmhand.
- Verify automatic catch-popup closing for normal fish, trash, first catches, records,
  fish ponds, treasure catches, secret notes, and a full inventory. Confirm disabling
  either automation or `AutoClosePopup` leaves the popup under manual control.
- Verify `InstantFishBite` in normal fishing with F5 automation both enabled and
  disabled. Confirm the game still chooses the catch normally and that supported
  festival fishing minigames trigger bites without affecting unrelated festivals.
- Verify automatic treasure collection with free slots, mergeable partial stacks,
  special no-slot items, and multiple rewards. For a full inventory, separately verify
  Stop leaves rewards in the menu, Drop places them on the ground, and Discard removes
  them only when explicitly selected; all three should disable automation.
- Verify `SkipAll` and `SkipOnlyCaught` with uncaught and previously caught fish,
  Challenge Bait, legendary fish, fish ponds, treasure targeting on/off, and supported
  festival minigames. Confirm `Off` preserves the normal minigame.
- Verify automatic bait attachment with `Any` and a specific preference, refill across
  multiple inventory stacks, rods without bait slots, manual bait already attached,
  and the opt-in spawn fallback. Repeat for a multiplayer farmhand and local
  split-screen player to confirm only the owning inventory is changed.
- Verify tackle attachment with `Any` and specific preferences on Iridium and Advanced
  Iridium Rods, including independent first/second slots, preserved durability, manual
  tackle already attached, and the opt-in spawn fallback. Repeat ownership checks for
  multiplayer farmhand and split-screen inventories.
- Verify infinite bait at stack size one and larger stacks, and infinite tackle at 19
  uses and lower durability. Cover successful catches, escaped fish, treasure, tool
  switching, warps, day end, saving/reloading, farmhand play, and split-screen; confirm
  no duplicate or temporary attachment is written into the save.
- Verify automatic eating at, below, and above the configured energy threshold with
  plain food, buff food, fish allowed/disallowed, a one-item stack, food/drink fullness
  buffs, and no eligible food. Repeat for multiplayer farmhand and split-screen
  inventories and confirm only the owning player's stack is consumed.
- Verify `Off`, `WarnOnly`, and `WarnAndPause` at 24:00 with warning counts 1 and 3.
  Let the threshold pass during a cast, minigame, catch popup, and treasure menu; the
  current catch must finish before only that screen's automation disables. Repeat as
  host, farmhand, and split-screen, confirming shared world time never pauses.
- Verify automatic casting at energy just above, equal to, and below the vanilla cast
  cost, both with and without automatic eating. Repeat with an Efficient rod and in a
  fishing festival; only the owning player's automation should stop, and no valid
  zero-cost cast should be blocked.
- Verify each temporary rod enchantment alone and all four together, with an already
  enchanted rod, option changes, rod switching, `RemoveWhenUnequipped` on/off, saving,
  reloading, and return to title. Repeat in local split-screen. Connect a remote
  farmhand while enchantments are active and confirm they are removed, a warning is
  shown, and no assistant-added enchantment appears in the saved rod afterward.
- Verify difficulty settings `1.0x/+0`, `0.0x/+0`, `0.5x/+10`, and `2.0x/-20` with
  manual and automatic fishing, the first-catch tutorial, Blessing of Waters, legendary
  fish, fish ponds, festival minigames, split-screen, host, and farmhand. Confirm each
  local minigame is adjusted once and another player's fish behavior is unaffected.
- Verify catch-result settings separately and together with manual fishing, automatic
  play, and skipped minigames. Cover every quality choice (including vanilla quality
  promotion on a perfect catch), maximum and reduced fish sizes, escaped fish, trash,
  ordinary counts 1-3, Wild Bait, Challenge Bait losing zero/one/two fish, legendary
  fish, first catches, fish ponds, and festival fishing. Confirm festival/fish-pond
  results remain vanilla and only the owning local player's result changes. Repeat as
  split-screen players, multiplayer host, and farmhand.
- Verify the fish preview with caught and uncaught fish, trash, legendary fish, normal
  and golden treasure, every preview sub-option, UI scales, zoom levels, window sizes,
  both sides of the screen, and long localized item names. Confirm it disappears during
  fade-out, never grants Sonar Bobber behavior, stays within each split-screen viewport,
  and shows only the owning player's catch history and treasure state. Repeat in a
  fishing festival as host and farmhand.
- Verify automatic junk disposal with automation on/off, the option on/off, each five
  default trash item, a newly created stack, an existing partial stack, multiple items
  gained in one tick, treasure rewards, and inventory-full reward handling. Confirm the
  Junk Ignore List always protects an item, untrashable/quest items remain untouched,
  fish require `AllowTrashFish`, only the newly gained quantity leaves an old stack,
  and trash-can reclamation money matches vanilla. Repeat for both split-screen players
  and as multiplayer host/farmhand, confirming only the event's owning inventory is
  changed.
- Verify the visual status HUD with F5 on/off, every automation state, and each badge:
  active work, ordinary disabled, menu pause, late night, low energy, action timeout,
  and recovery. Confirm enabled idle has no emote.
  Verify the background-free rod sprite, drop shadow, and animated Vanilla emotes
  remain readable over light and dark terrain. Test toolbar pinned and
  automatic top/bottom movement,
  configured left/right placement, UI scale and zoom settings, menus, festivals, and
  small window sizes. Repeat for both split-screen players and confirm each panel uses
  only that screen's session and remains inside its viewport.
- Repeat the normal cast-and-hook loop in local split-screen with each player alone and
  both players fishing simultaneously.
- During an automatic cast, interrupt with a menu, tool swap, warp, save, return to
  title, and local-player removal. Confirm the rod returns to Vanilla control, the next
  session starts cleanly, and removing either split-screen player leaves no stale HUD or
  automated input on the remaining screen.
- Repeat the normal and festival fishing checks as multiplayer host and farmhand.
- Inspect the SMAPI log after each scenario for errors, recoveries, duplicated input,
  or cross-player state leakage.

Update this file only after the corresponding behavior has been observed in-game.
