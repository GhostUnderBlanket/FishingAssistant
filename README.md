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
- Apply late-night warnings, low-energy protection, automatic eating, and configurable
  delays without forcing a shared multiplayer pause.

### Fishing and minigame assistance

- Adjust fish speed, catch progress gain and loss, treasure progress, and fishing-bar
  size independently.
- Preview the hooked fish with Classic or vanilla-inspired Sonar presentation.
- Hide uncaught fish, reveal legendary fish separately, and show treasure status.
- Steer manual or automatic casts toward reachable fishing bubbles without moving the
  player or increasing the rod's normal forward range.
- Choose steering effort, show a reachability marker, and optionally adjust cast power
  for automatic casts, manual casts, both, or neither.

### Catch and treasure control

- Configure perfect catches, fish size, quality, and supported multi-catch results.
- Target fishing treasure and optionally toggle targeting with a keybind and visual HUD
  indicator.
- Collect fully available treasure during the minigame.
- Maintain a visual Treasure Chest Ignore List and choose what happens when only ignored
  rewards remain.
- Treat items from the Junk List as ignored treasure without duplicating the list.

### Inventory, bait, and tackle tools

- Maintain visual Junk and Treasure Ignore lists using item pickers instead of item IDs.
- Dispose of junk immediately, only when the inventory is full, or not at all.
- Automatically eat suitable food when energy is low.
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
- `F6`: open the Fishing Assistant configuration menu.
- Treasure-targeting toggle: unbound by default and available as an optional keybind.

All controls can be changed from the in-game configuration menu. Controller and mouse
input are supported.

## Configuration

Open the menu with `F6`, change the draft settings, then select **Apply**. **Cancel**
discards the draft, while **Defaults** restores default values within the draft until it
is applied. Configuration is stored per player so local co-op players can use different
profiles and assistance settings.

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
