# Project Layout & Roadmap

Studio-style scaffold for a third-person FPS built on Unity 6 LTS + URP +
Cinemachine 3 + the new Input System. The folder structure mirrors a small
professional studio's separation of concerns so new disciplines (animation,
audio, AI, design) can land in their own swim-lane without stepping on
each other's toes.

> Engine baseline: **Unity 6 LTS**, URP 17.4, Cinemachine 3.1, Input System 1.19.
> Existing template content (`AlterunaFPS/`, `Blocks/`, `Core/`, `Platformer/`,
> `Shooter/`, `template-api/`, `TutorialInfo/`) is left untouched — this scaffold
> sits alongside it so you can mine those samples for art/animation/prefabs.

## Folder Structure

```text
Assets
├── Art
│   ├── Characters
│   ├── Weapons
│   ├── Environment
│   └── Materials
├── Audio
│   ├── Weapons
│   ├── Ambience
│   ├── Weather
│   └── Music
├── Animations
├── Prefabs
│   ├── Player
│   ├── Enemies
│   ├── Weapons
│   ├── Environment
│   └── UI
├── Scripts
│   ├── Common         # shared (Health/IDamageable)
│   ├── Player         # PlayerController + Input/Movement/Camera/Animator
│   ├── Weapons        # WeaponBase, RifleWeapon, WeaponManager, Bullet
│   ├── AI             # EnemyController + FSM states + Vision/Combat/Cover
│   ├── WaveSystem     # WaveManager, EnemySpawner, WaveData (ScriptableObject)
│   ├── UI             # HUD, PauseMenu
│   ├── Audio          # AudioManager (Unity Audio Mixer wrapper)
│   └── Managers       # GameManager (singleton), WeatherManager
├── VFX
├── Scenes
└── Resources
```

Empty folders carry a `.gitkeep` so the structure is preserved in git. As soon
as you drop any real asset in, Unity will write its own `.meta` file for the
folder.

## Namespacing

All new code lives under the `Game.*` namespaces:

| Namespace | Folder |
| --- | --- |
| `Game.Common`     | `Scripts/Common`     |
| `Game.Player`     | `Scripts/Player`     |
| `Game.Weapons`    | `Scripts/Weapons`    |
| `Game.AI`         | `Scripts/AI`         |
| `Game.AI.States`  | `Scripts/AI/States`  |
| `Game.WaveSystem` | `Scripts/WaveSystem` |
| `Game.UI`         | `Scripts/UI`         |
| `Game.Audio`      | `Scripts/Audio`      |
| `Game.Managers`   | `Scripts/Managers`   |

## Phased Roadmap

The scaffold matches the phases in the brief. Each phase builds on the
previous; resist the urge to skip ahead.

### Phase 1 — Player & Camera ✅ *scaffold ready*

**Files:** `PlayerController.cs`, `PlayerMovement.cs`, `PlayerInput.cs`,
`PlayerCamera.cs`, `PlayerAnimator.cs`, `Input/PlayerInputActions.inputactions`.

**Setup steps in a fresh scene:**

1. **Project Settings → Player → Active Input Handling** = *Input System
   Package (New)* (or *Both*).
2. Create an empty GameObject called **Player**.
3. Add `CharacterController` (size to fit your mesh).
4. Add `PlayerController` — `RequireComponent` will pull in `PlayerInput`
   and `PlayerMovement` automatically.
5. On `PlayerInput`, drag
   `Assets/Scripts/Player/Input/PlayerInputActions.inputactions` into the
   *Input Actions* field.
6. Add a child empty `GroundCheck` at the soles, assign it on `PlayerMovement`,
   and set the *Ground Mask*.
7. Add `Game.Common.Health` for damage tracking (used by HUD and weapons).
8. **Camera rig:** add a `CinemachineBrain` to the Main Camera. Create two
   `CinemachineCamera` GameObjects (e.g. `CM_Hip` and `CM_ADS`), each with
   a `CinemachineThirdPersonFollow` pointing at empty *Follow* targets on
   the player (one over-the-shoulder, one tighter for ADS).
9. Drop `PlayerCamera` somewhere convenient (the Player or the camera rig
   root) and assign both Cinemachine cameras + the `PlayerInput`.

**Inputs already bound:** WASD/left-stick (Move), mouse/right-stick (Look),
Space/A (Jump), LShift/L3 (Run), RMB/LT (Aim), LMB/RT (Fire), R/X (Reload),
Esc/Start (Pause).

### Phase 2 — Weapons

`WeaponBase` is abstract: subclass it for new weapons. `RifleWeapon` is a
hitscan starter. `WeaponManager` hangs off the player and routes Fire/Reload
input to the active weapon. `Bullet` is an optional projectile path.

### Phase 3 — Enemy AI

`EnemyController` registers a state for each entry in the brief (`Idle`,
`Patrol`, `Investigate`, `SeekCover`, `Attack`, `Dead`). `EnemyVision` is a
sight-cone, `EnemyCombat` does spread-based hitscan, `EnemyCoverSystem`
picks objects tagged `Cover` that break LOS to the threat.

> **Tags / Layers required:** add a `Cover` tag and set up a `Player` layer
> (or whatever the enemy targets) on `EnemyVision.targetMask`.

### Phase 4 — Wave System

`WaveData` is a ScriptableObject (`Create → Game → WaveSystem → Wave Data`).
`WaveManager` consumes a list of waves, applies per-wave health/damage/accuracy
multipliers, watches for all spawned `Health.OnDied` events, then advances.

### Phase 5 — Sci-Fi Warehouse Map

Pure level-design pass. Bake a NavMesh, tag covers, drop spawn points, wire
them to `EnemySpawner.spawnPoints`.

### Phase 6 — Weather

`WeatherManager` handles rain/splash particle systems, lightning flash on a
directional light, and delayed thunder via an AudioSource.

### Phase 7 — UI

`HUD` binds Health, current weapon ammo, and wave info to Canvas UI widgets.
`PauseMenu` toggles via the Pause input action and routes through
`GameManager.TogglePause()`.

## Recommended Next Improvements

- Create an `.asmdef` (`Game.Runtime.asmdef`) inside `Assets/Scripts/` once the
  module count grows, to formalise dependencies on `Unity.InputSystem` and
  `Unity.Cinemachine`. Skipped for now to keep iteration friction low.
- Wire `PlayerController.Health.OnDied` into `GameManager.NotifyPlayerDied()`
  so the HUD/PauseMenu/AudioManager can react to player death from one place.
- Author starter `WaveData` assets per difficulty tier in
  `Assets/Resources/WaveSystem/`.

## Outside-of-`Assets` notes

Keep the existing `.gitignore` and `.gitattributes` as-is. The
`ProjectSettings/` and `Packages/` folders are committed; everything under
`Library/`, `Temp/`, `Logs/`, `UserSettings/` should remain ignored.
