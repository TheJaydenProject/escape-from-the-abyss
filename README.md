# Escape from the Abyss

> A first-person horror game built in Unity 2022.3 — navigate two terror-filled environments, evade intelligent AI monsters, and find your way out before the darkness consumes you.

**Platform:** Windows 10/11 (64-bit) | **Engine:** Unity 2022.3.10f1 | **Course:** I3E IP / Assignment 2

---

## Table of Contents

- [Game Overview](#game-overview)
- [Gameplay Features](#gameplay-features)
- [Controls](#controls)
- [Levels](#levels)
- [Scoring System](#scoring-system)
- [Technical Architecture](#technical-architecture)
  - [Script Overview](#script-overview)
  - [AI System (FSM)](#ai-system-fsm)
  - [Interaction System](#interaction-system)
  - [Game State & Progression](#game-state--progression)
  - [Audio System](#audio-system)
  - [Scoring Formula](#scoring-formula)
- [Installation & Running](#installation--running)
- [Known Issues](#known-issues)
- [Credits & Assets](#credits--assets)

---

## Game Overview

**Escape from the Abyss** is a first-person psychological horror experience that traps the player in shifting, unreliable environments designed to unsettle and disorient. The game progresses from subtle environmental betrayal to active survival horror, demanding both exploration and evasion to reach the end.

---

## Gameplay Features

| Feature | Description |
|---|---|
| Two hand-crafted levels | The Corridor and The Library — each with distinct mechanics |
| Dynamic AI enemies | Three patrolling monsters with FOV detection, memory, and chase logic |
| Collectible progression | 25 VHS tapes unlock the final exit sequence |
| Environmental manipulation | Doors and passages shift when the player's back is turned |
| Scoring system | Final score based on time taken and number of deaths |
| Persistent HUD system | Context-aware prompts guide the player through each objective |
| Full audio design | Spatial footsteps, jump scares, ambient loops, pickup SFX |
| Main menu & end screen | Options, credits, how-to-play, and final score display |
| Volume persistence | Master volume saved via PlayerPrefs across sessions |
| Pause support | Freezes game state cleanly, re-enables on resume |

---

## Controls

| Action | Input |
|---|---|
| Move | `WASD` |
| Look | Mouse |
| Interact (pick up / open) | `E` |
| Pause | `Esc` |

---

## Levels

### Level 1 — The Corridor

An L-shaped hallway lit just enough to keep the player moving forward. A locked door at the far end promises an exit — but trying it yields nothing. When the player turns back, the locked door has vanished and a new exit has opened behind them.

This level introduces **environmental betrayal**: the world cannot be trusted, spatial memory is unreliable, and false goals are used to manufacture dread.

### Level 2 — The Library

A sprawling, fog-filled maze of repetitive bookshelves that destroy spatial orientation. Three monsters patrol the aisles, turning every route into a potential threat.

**Objective:** Collect all 25 VHS tapes scattered through the maze, insert them into the central computer to spawn an exit key, then unlock and escape through the final door.

This level layers **survival horror** on top of the disorientation from Level 1 — the player must explore without a map while avoiding enemies that actively hunt them.

---

## Scoring System

Your final score is displayed on the end-game screen alongside your time and death count.

- **Faster completion → higher score**
- **Fewer deaths → higher score**
- Both penalties use smooth curves rather than hard cutoffs, so every second and every life counts

A clean run (fast, zero deaths) scores near the maximum. A slow run with many deaths scores near the minimum.

---

## Technical Architecture

This section is for developers and technical reviewers. The project contains **19 C# scripts** across gameplay, AI, audio, UI, and state management systems.

### Script Overview

| Script | Responsibility |
|---|---|
| `GameManager.cs` | Central singleton — tracks VHS count, deaths, timer, HUD state, scene spawning, scoring |
| `AIChaseController.cs` | Monster AI — patrol, chase, catch, death/respawn sequence, camera switching |
| `SightSensor.cs` | AI vision — FOV cone, multi-point raycasting, memory timer |
| `PlayerRaycastInteraction.cs` | Camera raycast interaction system with layer-specific prompts |
| `IInteractable.cs` | Interface contract implemented by all interactable objects |
| `CollectibleVHS.cs` | VHS tape pickup — SFX, UI panel, GameManager notification |
| `CollectibleKey.cs` | Exit key pickup — SFX, hide/destroy, GameManager notification |
| `ComputerInteraction.cs` | Terminal that spawns the key once the VHS milestone is reached |
| `ExitDoor.cs` | Final door — locked feedback or win state depending on key possession |
| `LockedDoor.cs` | Locked door with one-time reveal of a hidden object |
| `SceneChangeDoor.cs` | Door that loads a new scene with pitch-adjusted SFX timing |
| `DoorTrigger.cs` | One-shot trigger that swaps object visibility for level events |
| `FootstepsSFX.cs` | Footstep audio loop driven by movement speed with sustain/fade logic |
| `GameEndSfx.cs` | Looping end-game music controller |
| `MasterVolumeSlider.cs` | Logarithmic AudioMixer volume slider with PlayerPrefs persistence |
| `MainMenu.cs` | Menu navigation, panel management, cursor/timescale setup |
| `ReturnToMenu.cs` | Resets game state and returns to main menu scene |
| `catchTrigger.cs` | Collider near the enemy that calls `AIChaseController.Caught()` |
| `ChaserTEST.cs` | Legacy prototype AI (replaced by AIChaseController) |

---

### AI System (FSM)

The monster AI is built across two decoupled components: **`AIChaseController`** (behavior and state transitions) and **`SightSensor`** (perception).

#### States

```
Idle ──► Walk (Patrol)
  │            │
  └────────────┼──► Chase ──► Caught
               │               │
               └───────────────┘ (after respawn + cooldown)
```

| State | Behaviour |
|---|---|
| **Idle** | Standing still at waypoint |
| **Walk / Patrol** | Moving between `patrolPoints` via NavMeshAgent |
| **Chase** | Re-pathing to player's last known position every 0.2 s |
| **Caught** | Stops movement, plays SFX, switches to death camera, respawns player |

#### Transitions

- `Idle ↔ Walk` — toggled by patrol waypoint arrival and wait timer
- `Idle/Walk → Chase` — triggered when `SightSensor.PlayerSeen` becomes true
- `Chase → Idle` — triggered when sight is lost after `memoryTime` expires
- `Chase → Caught` — triggered when player enters the `CatchTrigger` collider
- `Caught → Idle` — after respawn completes and `caughtCooldown` elapses

#### SightSensor — Vision System

```
Broad phase:  OverlapSphereNonAlloc (detection radius)
      │
Narrow phase: FOV cone check via dot product
      │
      ▼
LOS check:    Raycast from eye position to head / chest / hips
              (multi-point — player detected if ANY point is clear)
      │
Memory:       Player remains "seen" for memoryTime after losing direct LOS
```

- **Staggered ticks** — random start offset prevents all monsters checking vision on the same frame
- **Non-alloc physics** — pre-allocated collider buffer avoids GC pressure
- **Gizmo debug** — draws detection sphere and FOV cone bounds in the Scene view

#### Death & Respawn Sequence

1. `CatchTrigger` fires → calls `AIChaseController.Caught()`
2. NavMesh stops, caught SFX plays, Animator trigger fires
3. Death camera activates, audio listener transferred
4. HUD hidden for cinematic effect
5. Player movement disabled
6. 1.8 s delay → player teleported to spawn point
7. Main camera and HUD restored, movement re-enabled
8. `caughtCooldown` timer prevents instant re-capture
9. `GameManager.IsHandlingDeath` flag prevents concurrent double-processing

---

### Interaction System

All interactable objects implement the `IInteractable` interface:

```csharp
public interface IInteractable {
    string PromptText { get; }
    void Interact(PlayerInteractorRaycast interactor);
}
```

`PlayerRaycastInteraction` fires a camera-forward raycast each frame. On hit:
- The hit layer is bitwise-ANDed against each layer mask to identify the object type
- The correct HUD prompt is shown (`VHSpromptPanel`, `KeypromptPanel`, `DoorPromptPanel`, or computer-specific panels)
- Pressing `E` calls `interactable.Interact(this)`

Computer prompt changes dynamically:
- **Before milestone** — "Insert VHS tapes" prompt
- **After milestone** — "Activate terminal" prompt
- **After use** — prompt locked out permanently via `_computerUsed` flag

---

### Game State & Progression

`GameManager` is a persistent singleton (`DontDestroyOnLoad`) that coordinates all progression:

```
Scene load
    │
    ▼
Player spawned at scene-specific spawn point
    │
    ▼
VHS collection (0 → 25)   ──►  Milestone: show instructions, unlock computer
    │
    ▼
Computer interaction       ──►  Key spawns, key pickup instructions shown
    │
    ▼
Key collected              ──►  Exit door unlocked, pickup HUD shown
    │
    ▼
Exit door opened           ──►  ShowGameEndHUD() called
                                Time frozen, score computed, end screen displayed
```

#### HUD Stage Machine

| Stage | HUD shown |
|---|---|
| Scene entry | Intro/instruction flash |
| Collecting VHS | VHS counter (scene-gated) |
| Milestone reached | Milestone flash + persistent reminder |
| Key spawned | Key spawn flash + persistent reminder |
| Key collected | Key pickup flash |
| Game end | End screen (time, deaths, score) |

Events fired:
- `OnVHSCountChanged(int current, int target)` — updates counter text
- `OnVHSMilestone` — unlocks computer
- `OnDeathCountChanged(int count)` — tracks death total

---

### Audio System

| Feature | Implementation |
|---|---|
| Footsteps | Speed-threshold detection, sustained movement gate, optional fade-out via coroutine |
| Spatial audio | All pickups and UI sounds set `spatialBlend = 0` (full 2D) explicitly |
| Deferred destruction | Objects with audio wait `clip.length / pitch` before destroying |
| Audio on death | Listener transferred to death camera; restored after respawn |
| Volume persistence | Linear slider → logarithmic dB conversion → saved to PlayerPrefs |
| End-game loop | Dedicated `EndHudAudioLoop` component with `PlayLoop()` / `StopLoop()` |

---

### Scoring Formula

The score uses smooth mathematical curves instead of step penalties:

**Time factor** — logistic curve (S-curve), penalises long runs gradually:
```
timeFactor = 1 / (1 + exp(k × (minutesOver - halfPoint)))
```

**Death factor** — power law, heavy penalty past ~10 deaths:
```
deathFactor = 1 / (1 + (deaths / D₀)^P)
```

**Final score:**
```
score = MIN_SCORE + (MAX_SCORE - MIN_SCORE) × timeFactor × deathFactor
```

This means a perfect run scores near `MAX_SCORE`, while extreme values (50+ deaths, 30+ minutes) approach `MIN_SCORE` but never reach zero.

---

## Installation & Running

### Requirements

- **OS:** Windows 10 or 11 (64-bit)
- **Unity Build Version:** 2022.3.10f1
- **CPU:** Intel i5 or equivalent
- **RAM:** 8 GB minimum
- **GPU:** DirectX 11 compatible
- **Input:** Keyboard and mouse required

### Steps

1. Download the build from the [Releases](../../releases) page
2. Extract the ZIP file
3. Run `EscapeFromTheAbyss.exe`
4. Recommended resolution: **2560×1440** at **High** quality for the best experience

---

## Known Issues

- **AI pathfinding stall** — Occasionally a monster may play the walking animation in place. It will recover and resume its patrol route automatically.

---

## Credits & Assets

### 3D Assets

| Asset | Source |
|---|---|
| Monster | [Sketchfab — Smiler (Backrooms)](https://sketchfab.com/3d-models/smiler-backrooms-ea10d7f4750341f0ada6a12a6530014a) |
| Wall, Floor, Ceiling, Ceiling Light | Self-made |
| Door, Bookshelf, Books, Table | Self-made |
| Curved Table, Pillar, Computer, VHS, Key | Self-made |

### Audio

| Sound Effect | Source |
|---|---|
| Caught / jumpscare | [Pixabay](https://pixabay.com/sound-effects/jumpscare-94984/) |
| VHS pick up | [YouTube](https://www.youtube.com/watch?v=qZhEa5-A1rM) |
| Footsteps | [Pixabay](https://pixabay.com/sound-effects/01-24-footsteps-rug-slippers-slow-pace-73912/) |
| Insert VHS | [Pixabay](https://pixabay.com/sound-effects/vhs-caset-220318/) |
| Key pickup | [YouTube](https://www.youtube.com/watch?v=06ZSmICS0hA) |
| Locked door | [Freesound](https://freesound.org/people/Leady/sounds/12739/) |
| Open door | [Pixabay](https://pixabay.com/sound-effects/dorm-door-opening-6038/) |
| Victory / end music | [Pixabay](https://pixabay.com/sound-effects/escape-from-hell-looping-tune-228492/) |

---
