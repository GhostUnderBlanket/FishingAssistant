# Architecture

This document defines the initial architecture boundaries for Fishing Assistant 3. It
will evolve through small architecture decision records as implementation reveals new
constraints.

## Dependency direction

The SMAPI entry point is the composition root. Gameplay and UI systems must not reach
back into `ModEntry` to find shared mutable state.

```text
ModEntry / SMAPI events
        |
        v
Application coordination and per-screen session
        |
        +----> Fishing services ----> Game adapters
        |
        +----> Configuration -------> Persistence
        |
        +----> Menu and HUD --------> Stardew UI primitives
```

Decision logic should depend on small project-owned models and interfaces. Code that
reads or mutates Stardew Valley types belongs near the outside of the dependency graph.

## Planned areas

| Area | Responsibility |
| --- | --- |
| Entry | Compose services and route SMAPI lifecycle events. |
| Runtime | Hold per-screen session state and drive legal automation transitions. |
| Fishing | Decide casting, hooking, minigame, catch, and stop behavior. |
| Game adapters | Isolate direct access to `FishingRod`, `BobberBar`, menus, and player state. |
| Configuration | Defaults, schema, validation, migration, draft editing, and persistence. |
| UI | Reusable custom-menu controls, layout, navigation, tooltips, and validation display. |
| Inventory | Bait, tackle, food, treasure, trash, and ownership-safe item operations. |
| HUD | Render per-screen status and compact runtime controls. |
| Multiplayer | Define and enforce message contracts and host authority where required. |

These are responsibility boundaries, not a requirement to create one assembly per row.
The first implementation should stay in one mod assembly until a separate assembly
provides a concrete testing or dependency benefit.

## Runtime ownership

All runtime behavior must declare one of these scopes:

- **Local screen:** input, active menu, viewport, HUD, and local automation session.
- **Player:** inventory, equipped rod, preferences, and player-specific progression.
- **Shared world:** time, location state, and other host-synchronized game data.
- **Process:** immutable metadata and services proven safe to share between screens.

Mutable local-screen state will use SMAPI's split-screen utilities. Static mutable state
is prohibited. A feature may touch shared-world state only after its authority and
multiplayer behavior are documented.

The runtime stores an `AutomationSession` and action timers in `PerScreen<T>`. It
observes and controls only the current local player's equipped rod, and renders only
into that local screen's HUD. Automatic casting is a local-player action guarded by a
pure policy and a narrow rod adapter; it does not send multiplayer messages or mutate
shared-world state directly. Automatic hooking uses the same boundary and a per-screen
latch so one nibble can trigger at most one automated hook attempt.

Festival automation is denied by default. The current exception is the game's active
`FishingGame` minigame while it is still running; casting applies an additional startup
buffer so automation cannot act during the minigame countdown.

The initial minigame controller reads the current local screen's `BobberBar` through a
narrow adapter. A pure proportional controller writes its vertical bar speed while
vanilla code retains ownership of fish movement and catch progress. Live perfect and
maximum-size preferences are reapplied to the local bar so vanilla can render the
matching result feedback.

Final catch preferences cross one deliberately narrow Harmony boundary at
`FishingRod.pullFishFromWater`, immediately before vanilla serializes its result net
event. A pure catch-result policy decides all replacements; the patch only replaces
method arguments and falls back to the untouched vanilla result if compatibility code
fails. This boundary is necessary for multi-catch because vanilla computes its count in
a local variable which has no supported API or public mutable hook. Festival and fish
pond results remain untouched, as do legendary, Wild Bait, and Challenge Bait count
rules. Each call runs in the current local screen context and never edits shared fish
data.

Treasure targeting is an unsaved per-screen runtime preference. Its pure policy starts
targeting only after fish progress is high, uses hysteresis while pursuing the chest,
and returns to the fish before catch progress reaches the failure boundary.

Treasure chance overrides are decided once when a new `BobberBar` is observed. The
adapter changes only the bar's treasure flags, `Default` preserves the vanilla roll,
and festival fishing always keeps its vanilla result.

Instant treasure capture uses a pure eligibility policy and changes only the current
local bar's caught/progress fields after the chest reaches full scale. It is independent
of automatic minigame steering; skipped minigames may also preserve an existing chest
when the option is enabled. Both paths explicitly leave festival treasure untouched.

Fish preview rendering is local-screen UI only. The `BobberBar` adapter produces an
immutable snapshot containing the current item, reveal inputs, treasure state, and bar
bounds. A pure policy controls concealment and optional details, while a separate
layout policy places the panel inside the current `uiViewport`. The renderer caches
only display items per screen and never adds a Sonar Bobber or changes tackle state.

Automatic eating is a local-screen action over the owning player's inventory. A pure
policy selects food deterministically, while the game adapter starts the vanilla eat
animation and decrements the selected stack only after the game accepts consumption.

Automatic junk disposal consumes SMAPI's local-player `InventoryChanged` delta instead
of rescanning or removing matching inventory stacks. A pure policy requires automation,
the opt-in setting, explicit junk membership, vanilla trashability, and the fish safety
setting. The service removes only the added stack or positive quantity delta from that
exact item instance; the protected-item list always wins. Vanilla trash reclamation is
then applied to a copy containing only the discarded quantity.

Late-night warnings and their pending stop request are per-screen runtime state. The
assistant disables automation only after the current rod, minigame, and reward menu are
idle; it never pauses shared world time, which keeps host and farmhand behavior aligned.

Low-energy protection runs after automatic eating and before any automatic cast. It
uses the owning player's fishing level and rod enchantments, and disables only that
screen's automation if the next vanilla stamina charge would cause exhaustion.

Assistant-added rod enchantments are tracked by object identity per local screen and
never replace or remove enchantments already owned by the player. They are removed from
all tracked rods before saving and restored afterward for the session. Local
split-screen is supported in-process; remote multiplayer disables this feature because
the rod enchantment list is network-synchronized and could otherwise race the host save.

Fish difficulty adjustment is a one-time mutation of the current local screen's
`BobberBar`, after its constructor has applied vanilla tutorial and blessing rules. It
does not patch fish data or synchronize a modified value to another player's minigame.

## Automation lifecycle

The automation coordinator will use explicit states rather than unrelated tick flags:

```text
Idle -> Ready -> Casting -> WaitingForBite -> Hooking -> Minigame
  ^                                                    |
  |                                                    v
  +---------- Cooldown <- TreasureMenu <- CatchResult -+
```

`Paused` and `Faulted` are recoverable side states. Tool changes, warps, menu conflicts,
save unload, return to title, disconnect, and local-player removal must return the
session to a safe state and release any temporary changes.

Observed game state may occasionally skip an expected phase between update ticks. The
state machine records those jumps as recoveries and adopts the observed safe state;
future automation actions must still require a legal state before mutating the game.

## Configuration

Saved configuration, validated runtime configuration, and the menu's editable draft are
separate objects. The menu never mutates active configuration directly.

- Apply validates, persists, then replaces active configuration atomically.
- Cancel discards the draft.
- Reset changes only the draft until Apply is confirmed.
- Migration is version-aware and reports corrected or unsupported legacy values.

## Game access

Use this order when implementing behavior:

1. supported SMAPI events and APIs;
2. public Stardew Valley members;
3. isolated reflection access;
4. isolated Harmony patches when no supported alternative exists.

Direct access to mutable `FishingRod` and `BobberBar` state must be wrapped by narrow
adapters. Decompiled source is behavioral reference material, not a stable API contract.

## Testing strategy

- Keep decisions deterministic and independent from global game state where practical.
- Unit-test configuration migration, validation, state transitions, targeting, stop
  rules, and inventory decisions.
- Use adapters as seams for integration tests without reproducing game implementation.
- Validate actual game timing and rendering manually against the release matrix.
- Never commit or redistribute game assemblies to make CI work.

## Architecture decisions

Material decisions should be recorded under `docs/decisions/` using a short document
that states context, decision, consequences, and alternatives. Reflection, Harmony,
network messages, new assemblies, and save-data formats always require a recorded
decision.
