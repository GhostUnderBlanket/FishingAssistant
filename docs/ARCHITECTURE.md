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
narrow adapter and writes only its vertical bar speed. A pure proportional controller
targets the fish while vanilla code retains ownership of fish movement, catch progress,
treasure, perfect status, and the final catch result.

Treasure targeting is an unsaved per-screen runtime preference. Its pure policy starts
targeting only after fish progress is high, uses hysteresis while pursuing the chest,
and returns to the fish before catch progress reaches the failure boundary.

Treasure chance overrides are decided once when a new `BobberBar` is observed. The
adapter changes only the bar's treasure flags, `Default` preserves the vanilla roll,
and festival fishing always keeps its vanilla result.

Automatic eating is a local-screen action over the owning player's inventory. A pure
policy selects food deterministically, while the game adapter starts the vanilla eat
animation and decrements the selected stack only after the game accepts consumption.

Late-night warnings and their pending stop request are per-screen runtime state. The
assistant disables automation only after the current rod, minigame, and reward menu are
idle; it never pauses shared world time, which keeps host and farmhand behavior aligned.

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
