# Fishing Assistant 3

Fishing Assistant 3 is a configurable fishing automation and accessibility mod for
Stardew Valley. It can automate the complete fishing loop, assist only with selected
steps, or leave fishing under manual control while providing previews and quality-of-life
tools.

The mod includes its own in-game configuration menu.

## Features

### Flexible fishing automation

- Automatically cast, hook fish, play or skip the fishing minigame, close catch popups,
  and collect treasure.
- Use Relaxed, Training, Manual+, or Custom automation profiles as a starting point.
- Toggle every automation stage independently.
- Use Slow, Normal, Fast, or Custom timing for automatic recasting, catch popups, and
  treasure looting and food consumption.
- Apply late-night warnings, low-energy protection, automatic eating, and optional
  inventory opening after a safety stop without forcing a shared multiplayer pause.

### Fishing and minigame assistance

- Adjust fish speed, catch progress gain and loss, treasure progress, and fishing-bar
  size independently.
- Skip every fishing minigame, or skip a species only after the configured number of
  normal or perfect catches.
- Preview the hooked fish with Classic or vanilla-inspired Sonar presentation.
- Control the visibility of uncaught and legendary fish, and show treasure status.
- Steer manual or automatic casts toward reachable fishing bubbles without moving the
  player or increasing the rod's normal forward range.
- Choose steering effort, show a reachability marker, and optionally adjust cast power
  for automatic casts, manual casts, both, or neither.

### Catch and treasure control

- Keep vanilla fish quality and amount, enforce a minimum, or set a fixed result for
  supported catches. Perfect-catch and maximum-size controls remain independent.
- Target fishing treasure and optionally toggle targeting with a keybind and visual HUD
  indicator.
- Scale normal bite waiting time and fishing or golden treasure chances while retaining
  the applicable vanilla bonuses.
- Collect fully available treasure during the minigame.
- Maintain a visual Treasure Chest Ignore List and choose what happens when only ignored
  rewards remain.
- Treat items from the Junk List as ignored treasure without duplicating the list.

### Inventory, bait, and tackle tools

- Maintain visual Junk and Treasure Ignore lists using item pickers instead of item IDs.
- Dispose of junk immediately, only when the inventory is full, or not at all.
- Automatically eat ordered preferred food at an energy target or only when another cast
  is unaffordable. Choose whether fallback selection uses the best value, most energy,
  or no food, with a configurable delay that allows cancellation.
- Attach bait and tackle from ordered preference lists, including both slots on the
  Advanced Iridium Rod.
- Refill missing attachments, preserve infinite bait or tackle, and optionally provide a
  selected starter rod.
- Apply optional session-only fishing-rod enchantments without permanently modifying the
  player's equipment.

### Multiplayer-aware design

Configuration, input, HUD state, automation state, inventory handling, and temporary
runtime data are isolated for each local player. Fishing Assistant supports single-player,
local split-screen co-op, remote multiplayer, and supported fishing-festival minigames.
Settings that would be unsafe while remote players are connected are disabled instead of
writing temporary state into the shared save.

## Requirements

- Stardew Valley 1.6.15 or a compatible later 1.6 release.
- SMAPI 4.5.2 or later.

## Installation

1. Install [SMAPI](https://smapi.io/).
2. Download and extract Fishing Assistant into the game's `Mods` folder.
3. Start Stardew Valley through SMAPI.


## Controls

- `F5`: enable or disable fishing automation for the current local player.
- `F6`: main shortcut for opening the Fishing Assistant configuration menu.
- Controller `Back / View`: optional shortcut for opening the configuration menu.
- Treasure-targeting toggle: unbound by default and available as an optional keybind.

All controls can be changed from the in-game configuration menu. Controller and mouse
input are supported. On macOS, hold `Fn` when using a configured F1-F24 shortcut if the
operating system assigns that function key to a system action.

## Configuration

Open the menu with `F6`, change the draft settings, then select **Apply**. **Cancel**
discards the draft, while **Defaults** restores default values within the draft until it
is applied. The menu warns before discarding unapplied changes and supports sliders,
direct numeric entry, keyboard, mouse, and controller input. Configuration is stored per
player so local co-op players can use different profiles and assistance settings. Pages
follow the fishing workflow and use named group dividers; their height and visible rows
are recalculated when the game window or UI scale changes.

## Building from source

Install a .NET SDK capable of targeting .NET 6 and ensure Stardew Valley with SMAPI is
installed in a location recognized by
[`Pathoschild.Stardew.ModBuildConfig`](https://www.nuget.org/packages/Pathoschild.Stardew.ModBuildConfig).
Then run:

```powershell
dotnet build FishingAssistant.slnx --configuration Release
```

Builds do not automatically deploy into the game's `Mods` folder.

## License

Fishing Assistant is available under the [MIT License](LICENSE).
