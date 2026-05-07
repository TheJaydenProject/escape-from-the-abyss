# Escape from the Abyss

A first-person horror game built in Unity 6 where you navigate two increasingly hostile environments, evade patrolling AI monsters, and find a way out.

**Platform:** Windows 10/11 (64-bit) | **Engine:** Unity 6.4 (6000.4.5f1)

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
  - [Game State and Progression](#game-state-and-progression)
  - [Audio System](#audio-system)
  - [Scoring Formula](#scoring-formula)
- [Installation and Running](#installation-and-running)
- [Known Issues](#known-issues)
- [Credits and Assets](#credits-and-assets)

---

## Game Overview

Escape from the Abyss is a first-person psychological horror game across two levels. The first level is built around environmental manipulation and misdirection. The second drops you into a fog-filled monster maze where the only way out is to search every corner and stay hidden long enough to find it.

---

## Gameplay Features

| Feature | Description |
|---|---|
| Two hand-crafted levels | The Corridor and The Library, each with different mechanics and pacing |
| AI enemies | Three patrolling monsters with FOV detection, memory, and chase behaviour |
| Collectible progression | Collecting 25 VHS tapes unlocks the exit sequence |
| Environmental manipulation | Doors and exits shift when the player's back is turned |
| Scoring | Final score calculated from time taken and number of deaths |
| Context-aware HUD | Prompts update as the player progresses through each objective |
| Audio design | Spatial footsteps, jump scare SFX, ambient loops, and pickup feedback |
| Main menu and end screen | Options, credits, how-to-play panel, and final score display |
| Volume persistence | Master volume saved between sessions via PlayerPrefs |
| Pause | Cleanly freezes and restores game state |

---

## Controls

| Action | Input |
|---|---|
| Move | `WASD` |
| Look | Mouse |
| Interact | `E` |
| Pause | `Esc` |

---

## Levels

### Level 1 - The Corridor

An L-shaped hallway where limited lighting keeps you moving forward. A locked door at the far end looks like the exit, but interacting with it does nothing. Turn around, and the door is gone. A new exit has appeared at the start.

The goal here is disorientation. The level teaches the player that the environment itself cannot be trusted.

### Level 2 - The Library

A large fog-filled maze built from repetitive bookshelves. Three monsters patrol fixed routes, and they will chase you the moment they spot you.

**Objective:** Find all 25 VHS tapes spread throughout the maze. Insert them into the central computer to spawn a key. Pick up the key, then get out through the exit door.

The maze layout makes navigation difficult by design. Every aisle looks the same, there is no map, and the monsters turn safe paths into dead ends.

---

## Scoring System

Your final score is shown at the end alongside your completion time and death count.

- Faster runs score higher
- Fewer deaths score higher
- Both factors use smooth curves, not hard penalties, so there is no cliff where one extra death or one extra minute tanks the score

---

## Technical Architecture

The project contains 19 C# scripts covering gameplay, AI, audio, UI, and state management.

### Script Overview

| Script | Responsibility |
|---|---|
| `GameManager.cs` | Persistent singleton tracking VHS count, deaths, timer, HUD state, spawning, and scoring |
| `AIChaseController.cs` | Monster behaviour: patrol, chase, catch detection, death and respawn sequence |
| `SightSensor.cs` | AI vision: FOV cone, multi-point raycasting, memory timer |
| `PlayerRaycastInteraction.cs` | Camera raycast interaction with layer-specific prompt handling |
| `IInteractable.cs` | Interface implemented by all interactable objects |
| `CollectibleVHS.cs` | VHS tape pickup with SFX, UI panel display, and GameManager callback |
| `CollectibleKey.cs` | Exit key pickup with SFX, visual hiding, and GameManager callback |
| `ComputerInteraction.cs` | Terminal that spawns the key once the VHS milestone is hit |
| `ExitDoor.cs` | Final door: plays locked feedback or triggers win state based on key possession |
| `LockedDoor.cs` | Locked door with a one-time object reveal on first interaction |
| `SceneChangeDoor.cs` | Door that loads another scene with pitch-corrected SFX timing |
| `DoorTrigger.cs` | One-shot trigger that swaps object visibility for scripted level events |
| `FootstepsSFX.cs` | Footstep audio driven by movement speed, with sustain and optional fade |
| `GameEndSfx.cs` | Looping end-game music with play and stop controls |
| `MasterVolumeSlider.cs` | Logarithmic AudioMixer volume slider with PlayerPrefs persistence |
| `MainMenu.cs` | Menu panel navigation, cursor handling, and scene loading |
| `ReturnToMenu.cs` | Resets time scale and cursor state, then loads the main menu |
| `catchTrigger.cs` | Collider on the monster that calls `AIChaseController.Caught()` on player contact |
| `ChaserTEST.cs` | Early prototype AI (superseded by AIChaseController, kept for reference) |

---

### AI System (FSM)

The monster AI splits across two components: `AIChaseController` handles state transitions and behaviour, while `SightSensor` handles perception independently.

#### States

```
Idle <---> Walk (Patrol)
  |              |
  +--------------+----> Chase ----> Caught
                                      |
                         (respawn + cooldown, back to Idle)
```

| State | Behaviour |
|---|---|
| Idle | Waiting at a waypoint |
| Walk / Patrol | Moving between `patrolPoints` via NavMeshAgent |
| Chase | Re-pathing toward the player's last known position every 0.2s |
| Caught | Stops movement, plays SFX, cuts to death camera, respawns the player |

#### Transitions

- `Idle / Walk -> Chase` when `SightSensor.PlayerSeen` becomes true
- `Chase -> Idle` when sight is lost and `memoryTime` runs out
- `Chase -> Caught` when the player enters the `CatchTrigger` collider
- `Caught -> Idle` after the respawn sequence completes and `caughtCooldown` expires

#### SightSensor - Vision System

```
Broad phase:  Physics.OverlapSphereNonAlloc within detection radius
      |
Narrow phase: FOV cone check using dot product
      |
LOS check:    Raycast to three body points (head, chest, hips)
              Player is visible if any one point has a clear line of sight
      |
Memory:       PlayerSeen stays true for memoryTime after direct LOS breaks
```

Notable implementation details:
- Vision ticks are staggered with a random start offset so all three monsters do not check on the same frame
- Uses `OverlapSphereNonAlloc` with a pre-allocated buffer to avoid per-frame GC allocations
- Scene view gizmos draw the detection radius and FOV cone for easier tuning

#### Death and Respawn Sequence

1. `CatchTrigger` fires and calls `AIChaseController.Caught()`
2. NavMesh stops, caught SFX plays, Animator trigger fires
3. Death camera activates and takes the audio listener
4. HUD is hidden
5. Player movement is disabled
6. After 1.8s the player teleports to the spawn point
7. Main camera and HUD are restored, movement re-enabled
8. `caughtCooldown` blocks immediate re-capture
9. `GameManager.IsHandlingDeath` prevents concurrent death processing if multiple triggers fire at once

---

### Interaction System

All interactable objects implement a shared interface:

```csharp
public interface IInteractable {
    string PromptText { get; }
    void Interact(PlayerInteractorRaycast interactor);
}
```

`PlayerRaycastInteraction` shoots a forward raycast from the camera each frame. When it hits something:
- The layer is bitwise-checked against each layer mask to identify the object type
- The matching HUD prompt activates (`VHSpromptPanel`, `KeypromptPanel`, `DoorPromptPanel`, or one of the computer prompts)
- Pressing `E` calls `Interact()` on the hit object

The computer prompt has three states:
- Before VHS milestone: shows a "collect VHS tapes" prompt
- After milestone: shows an "activate terminal" prompt  
- After the terminal is used: prompt is permanently hidden via `_computerUsed`

---

### Game State and Progression

`GameManager` is a `DontDestroyOnLoad` singleton that owns all progression state:

```
Scene loads
    |
Player spawns at the scene-specific spawn point
    |
Collect VHS tapes (0 to 25) --> Milestone hit: computer unlocks, instructions shown
    |
Interact with computer --> Key spawns, key stage instructions shown
    |
Pick up the key --> Exit door unlocks, pickup HUD shown
    |
Open the exit door --> ShowGameEndHUD() called
                       Time freezes, score calculated, end screen shown
```

#### HUD Stage Progression

| Stage | HUD shown |
|---|---|
| Scene entry | Intro instruction flash |
| Collecting VHS | VHS counter (only shows in configured scenes) |
| Milestone reached | Milestone flash plus a persistent reminder |
| Key spawned | Key spawn flash plus a persistent reminder |
| Key collected | Key pickup flash |
| Game end | End screen with time, deaths, and score |

Events broadcast by GameManager:
- `OnVHSCountChanged(int current, int target)` updates the counter text
- `OnVHSMilestone` signals that the computer is now usable
- `OnDeathCountChanged(int count)` tracks the running death total

---

### Audio System

| Feature | Implementation |
|---|---|
| Footsteps | Speed threshold detection with a sustain timer before audio starts; optional fade-out coroutine on stop |
| Spatial audio | All pickup and UI sounds explicitly set `spatialBlend = 0` for full 2D playback |
| Deferred destruction | Objects with audio compute `clip.length / pitch` and delay `Destroy()` until playback finishes |
| Death audio | Audio listener transfers to the death camera during the sequence and transfers back after respawn |
| Volume slider | Linear slider value converted to dB via `Log10(v) * 20` before applying to the AudioMixer |
| End-game music | Dedicated `EndHudAudioLoop` component with explicit `PlayLoop()` and `StopLoop()` calls |

---

### Scoring Formula

Score uses continuous curves rather than fixed step penalties.

**Time factor** (logistic S-curve, penalises slow runs gradually):
```
timeFactor = 1 / (1 + exp(k * (minutesOver - halfPoint)))
```

**Death factor** (power law, heavier penalty past around 10 deaths):
```
deathFactor = 1 / (1 + (deaths / D0) ^ P)
```

**Final score:**
```
score = MIN_SCORE + (MAX_SCORE - MIN_SCORE) * timeFactor * deathFactor
```

A clean run sits close to `MAX_SCORE`. High death counts or very long runs push the score down smoothly rather than falling off a cliff at a specific threshold.

---

## Installation and Running

### Requirements

- OS: Windows 10 or 11 (64-bit)
- Unity build version: 6.4 (6000.4.5f1)
- CPU: Intel i5 or equivalent
- RAM: 8 GB minimum
- GPU: DirectX 11 compatible
- Input: Keyboard and mouse

### Steps

1. Download the build from the [Releases](../../releases) page
2. Extract the ZIP
3. Run `EscapeFromTheAbyss.exe`
4. Recommended: 2560x1440 resolution at High quality settings

---

## Known Issues

- **AI pathfinding stall:** A monster may occasionally play the walk animation in place without moving. It will recover on its own and return to its patrol route.

---

## Credits and Assets

### 3D Assets

| Asset | Source |
|---|---|
| Monster | [Sketchfab - Smiler (Backrooms)](https://sketchfab.com/3d-models/smiler-backrooms-ea10d7f4750341f0ada6a12a6530014a) |
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
| Victory music | [Pixabay](https://pixabay.com/sound-effects/escape-from-hell-looping-tune-228492/) |

---
