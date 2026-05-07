# Escape from the Abyss

A first-person horror game built in Unity 6 where you navigate two increasingly hostile environments, evade patrolling AI monsters, and find a way out.

**Platform:** Windows 10/11 (64-bit) | **Engine:** Unity 6.4 (6000.4.5f1)

<img width="2224" height="1236" alt="Screenshot 2025-08-13 065302" src="https://github.com/user-attachments/assets/9a0fa1b8-9cc4-423c-9e19-5840e1d028b1" />
<img width="2195" height="1201" alt="Screenshot 2025-08-13 073223" src="https://github.com/user-attachments/assets/df47c169-8ada-46af-a2cd-f9dfb6ee9064" />
<img width="2222" height="1226" alt="Screenshot 2025-08-13 065232" src="https://github.com/user-attachments/assets/e5a1962f-6037-4fee-a3ed-bdbce90adfe4" />

---

## Table of Contents

- [Game Overview](#game-overview)
- [Gameplay Features](#gameplay-features)
- [Controls](#controls)
- [Levels](#levels)
- [Design Rationale](#design-rationale)
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

Escape from the Abyss is a first-person psychological horror game across two levels. The first is built around environmental manipulation and misdirection. The second drops you into a fog-filled monster maze where the only way out is to search every corner and stay hidden long enough to find it.

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

## Design Rationale

Both levels are built around spatial theory: using layout, visibility, and environmental logic to generate tension and uncertainty without relying entirely on scripted events or cheap scares.

### Level 1 - Spatial Betrayal

The L-shape of the corridor is deliberate. It cuts off long sightlines, keeping the player in a reactive state where planning is difficult. The locked door at the end is a false goal: it signals progress, builds expectation, and then denies it. When the player backtracks, the environment contradicts itself. The locked door disappears and a new one appears at the start. There is no explanation.

This spatial inversion is the core mechanic of the level. It does not rely on a monster or a jump scare. The environment itself becomes the threat by proving it cannot be trusted. The player carries that instability into Level 2.

Key principles applied: restricted visibility, false goal, spatial inversion, environmental contradiction.

### Level 2 - Claustrophobic Immersion

The library uses repetitive bookshelves and fog to prevent the player from forming a reliable mental map. Every aisle looks the same. Depth perception is flattened. The space feels both large and suffocating at the same time.

The three monsters are placed in overlapping patrol zones so that safe corridors are never permanently safe. Their routes are fixed, but they will break from their zone to chase the player, which means any aisle can become dangerous at any point. Navigation becomes a constant read of risk rather than a straightforward search.

The computer at the centre of the level acts as a structural anchor. All 25 VHS tapes funnel back to it. The progression chain, tapes to computer to key to exit, keeps the player moving through dangerous space repeatedly rather than finding the exit and rushing straight to it. Tension is maintained until the last moment because the exit door is hidden among shelving that looks identical to everything else.

Key principles applied: repetition and disorientation, fog-restricted sightlines, patrol zones as dynamic hazards, distributed objectives, structured progression nodes.

### Combined Arc

The two levels form a deliberate progression. The Corridor establishes that the environment is unreliable. The Library scales that instability with size, repetition, and active threats. By the time the player reaches the exit, the sense of relief is earned because the design has been working against them the entire time.

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
<img width="1372" height="1105" alt="image" src="https://github.com/user-attachments/assets/94eeaee3-6f9a-4ca7-8e14-265973074a8d" />
<br>

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

---


#### SightSensor - Vision System

```
How the monster detects the player (four stages, all must pass):

1. RANGE CHECK
   Physics.OverlapSphereNonAlloc fires every 0.1s around the monster.
   If the player is not within detectionRadius, all further checks are skipped.
   Using NonAlloc means no heap allocation per tick — the result writes into
   a pre-allocated buffer, keeping GC pressure flat across all three monsters.

2. FIELD OF VIEW CHECK
   Even if the player is close enough, are they actually in front of the monster?
   The monster has a configurable fovAngle (a cone of vision, e.g. 90 degrees).
   The check uses a dot product between the monster's forward vector and the
   direction to the player, then compares against cos(fovAngle / 2).
   If the player is outside that cone — e.g. directly behind the monster — the
   check fails and no raycast is fired.

3. LINE OF SIGHT CHECK
   The player is in range and in the FOV — but is there a wall between them?
   Three raycasts fire from the monster's eye position (transform + eyeHeight offset)
   toward three points on the player's body:
     - Head  (localOffset y = 1.6)
     - Chest (localOffset y = 1.2)
     - Hips  (localOffset y = 0.9)
   The player is detected if ANY single ray reaches them unobstructed.
   This means crouching behind a shelf can block two rays, but a partial
   exposure like a head poking out is still enough to trigger a chase.

4. MEMORY
   Once spotted, the monster does not immediately forget the player if they
   duck behind cover. A _forgetAt timestamp is set to Time.time + memoryTime,
   keeping PlayerSeen true for that window even after direct LOS breaks.
   This prevents the player from rapidly blinking in and out of cover to
   trivially reset a chase.
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
- The matching HUD prompt activates
- Pressing `E` calls `Interact()` on the hit object

The computer prompt has three states:
- Before VHS milestone: shows a collect VHS tapes prompt
- After milestone: shows an activate terminal prompt
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

#### Gameplay Loop
<img width="1035" height="1134" alt="image" src="https://github.com/user-attachments/assets/9172277b-7aae-4e1a-8112-4cb1f2be3073" />

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

A clean run sits close to `MAX_SCORE`. High death counts or very long runs push the score down smoothly rather than dropping off at a specific threshold.

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
