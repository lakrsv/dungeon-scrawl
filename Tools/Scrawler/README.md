# Scrawler

Scrawler is a program that plays Dungeon Scrawl. It types the words on screen, at a human speed, through the same input path as the keyboard. It was written to try to beat the game on **IMPOSSIBLE** (difficulty 3) without cheating.

It lives outside `Assets/`, so Unity never compiles it into the game or a build. It is compiled in memory and run inside a playing Editor through the Unity CLI's `script run` command (from `com.unity.pipeline`).

## Fair-play rules

- **Input.** Each letter goes through `InputController.AppendLetter(char)`, which is what `InputController.Update()` calls for every character of `Input.inputString`. Typing speed is a fixed human profile:
  - `120` (default): 0.08–0.12 s per key, and 0.20–0.30 s to read each new prompt before typing it.
  - `85`: 0.12–0.16 s per key, and 0.25–0.35 s to read each prompt.
- **Only what is on screen.** It sees:
  - explored and lit tiles;
  - sprites that are currently drawn, classified by how they look (Pest, Demon, Slime, corpse, chest);
  - prompt labels and their icons;
  - its own stats, which the HUD shows.

  It never reads enemy health, turn state, the unexplored map, or which "chest" is really an enemy.
- **Game knowledge.** It knows what a player learns from playing: enemy stats per difficulty, that mimic chests only appear on levels 2 and 4, and how turns work.
- **No interference.** It never edits health or stats, pauses, or changes code. The official run runs at time scale 1.
- **Menus.** The splash screen waits for `Input.anyKeyDown`, which can't be injected from outside, so Scrawler loads `MainMenu` itself, exactly as that keypress does. On the main menu it clicks the IMPOSSIBLE button by invoking the button's `onClick`.
- **The turn-freeze quirk.** Scrawler deliberately uses it. This was explicitly allowed; see [The turn-freeze quirk](#the-turn-freeze-quirk).

## Running it

You need:

- the project open in the Unity Editor;
- the `unity` CLI on your `PATH`;
- bash (Git Bash on Windows).

Save the open scene first, because the script switches scenes.

```sh
# The real thing: splash screen -> IMPOSSIBLE -> levels 1-5, at time scale 1.
Tools/Scrawler/scrawl.sh official            # ~120 WPM
Tools/Scrawler/scrawl.sh official --wpm 85

# Practice from any level with any stats. --speed scales game time; typing timers run in game
# time, so 4x speed plays out the same as 1x, only faster.
Tools/Scrawler/scrawl.sh practice --level 5 --health 9 --damage 8 --reach 6 --sight 8 --speed 4
```

The script follows the player's log (lines tagged `[SCRAWL]` in the Unity console) until the run ends. To stop early, exit Play mode.

Without the script, enter Play mode on `SplashScreen` and run:

```sh
unity command script run --file Tools/Scrawler/Scrawler.cs --entry Scrawler.Official --args '["120"]'
```

## How it plays

It keeps a memory of the level as it sees it: explored floor, chests and whether each is real, where it last saw each Demon, the exit, damage dealt to each enemy, and when each enemy last moved or lunged.

**Timing.** Enemies act one at a time, in a fixed rotation. After an enemy moves or attacks, it can't act again for a full rotation. Scrawler learns the rotation length by watching enemies move. That tells it how long it has before a given enemy can strike again.

**Combat.** These options are tried in order:

1. Finish an enemy it can kill before that enemy's next turn.
2. Hit-and-step: attack, then step away, when both fit inside the enemy's turn gap.
3. Fight, if it would survive the expected damage plus one extra hit.
4. Otherwise, run to the nearest tile that no visible enemy can hit. If none exists, run to the tile the fewest can hit. Escapes avoid dead ends, prefer corridors when several Pests are about, and keep away from Pests.

It never attacks Slimes, which stay passive unless attacked.

**Pests.** When a Pest is two tiles away, Scrawler waits for it to step up. Its turn has then just ended, which leaves a full rotation to hit it and step back.

**Demons.** Their fireball does 6 damage and always lands, so Scrawler avoids their sight lines. Paths and exploration steer away from where it last saw a Demon. When escaping, it prefers hiding within a Demon's range but out of its sight, which triggers the freeze below.

**Chests.** It loots when nothing is close enough to interrupt. On levels 2 and 4, a chest counts as real once it stays drawn (greyed) after leaving view. Mimics are creatures, and creatures vanish out of view. An attack prompt over a chest also gives a mimic away. It keeps clear of unverified chests.

**Navigation.**

- Levels 1–3: collect chests, explore, then take the exit.
- Level 4: nearby chests, then the exit.
- Level 5: straight for the exit.

It commits to a target until it is reached or stops being useful.

## The turn-freeze quirk

`TurnSystem` gives one enemy the turn at a time and only moves on when that enemy ends its turn. A Demon that has the turn ends it in one of three ways:

- by moving: in `MoveSystem.UpdatePosition`, when it is out of range;
- by attacking: in `AttackSystem.UpdateAIAttack`, when it is in range and in the player's field of view;
- automatically, when the player is too far away for it to notice.

If the player is **in range but out of sight**, none of those happen. `UpdatePosition` returns early because the player is in reach, and `UpdateAIAttack` returns early because the Demon isn't in the player's field of view. So the Demon keeps the turn forever, and every other enemy in the dungeon freezes. This ends when the player steps into its view (it strikes immediately) or moves out of its range.

Scrawler spots a freeze from the screen. A visible enemy that should have acted, such as one in range or one chasing, has done nothing for well over a rotation. While frozen, it loots and attacks freely, and stops as soon as anything visible moves again.

## Results

Practice runs on IMPOSSIBLE at ~120 WPM, with the final version unless noted:

| Run | Result |
|---|---|
| Full game from level 1 (fresh stats) | 1 of 4 cleared level 1 (8 kills), then died at the start of level 2. Across the last few versions, about 1 in 6 clears level 1. |
| Level 3 practice (HP 7, damage 5, reach 5, sight 6) | 0 of 4. Survived up to 21 s, with up to 7 free hits during freezes. |
| Level 5 practice (HP 9, damage 8, reach 6, sight 8) | 0 of 5. An earlier version won 1 of 4 from the same start. Spawning in sight of several Demons is usually fatal within seconds. |

Official run: _to be filled in._

## Files

- `Scrawler.cs`: the player. It does perception, memory, decisions, and typing.
- `scrawl.sh`: starts an official or practice run in the open Editor and prints the log.

## Limitations

- Scrawler reads some private fields by reflection. They include `InputController._currentInputWord`, `SpellHint._text`, `MapSystem` internals, and `Sprites`' sprite arrays. Renaming any of them breaks it.
- It handles Pests well. Demons on IMPOSSIBLE remain the hard part: one hit takes most of your health, and a level-5 spawn can be in sight of several of them.
