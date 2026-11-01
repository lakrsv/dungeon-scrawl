# The Scrawl: the level 5 boss

The design for the final fight, agreed with the user on 2026-11-01, as built. Build status is at the bottom; keep it up to date.

## The idea

The game is called Dungeon Scrawl, so the final boss is the thing that does the scrawling: **the Scrawl**, a creature of living ink in the void under the dungeon. It writes the words the dungeon is made of. That gives the mechanics their meaning:

- **Every word you type at it unwrites a piece of it.** Its hands and its mask carry the words that hurt them.
- **Dodges are warding words.** When it winds up, short words flare beside you, one for each way you can step.
- **The head speaks.** Its mouth bursts open, and its beam is a stream of glowing letters (SCRAWLED INK, UNWRITTEN WORDS) pouring down one lane of the bridge.

Look (drawn in `Tools/PixelArt/boss.py`):

- **Hands:** gaunt claws of violet-sheened ink, long jointed fingers ending in bone talons, with gold handwriting glowing under the skin. Thin arms rise out of the void and dither away into it. Charging, a hand splays its talons and burns red.
- **Head:** a long, angular bone mask with a pointed chin, lit on one side and shadowed on the other, hollow eye sockets with pinpoint red eyes in a scowl, and a mouth stitched shut. It sits in a peaked hood of ink with a ragged hem, and two pen nibs sweep up from the hood like horns. Charging its beam, its rim burns red and the stitches burst into a glowing seam; below half health, the mask cracks.
- The same outlines and pixel scale as the original creatures, just enormous; the ink reads against the void (#07060b) by its violet rim.

## Level 5

Level 5 is handcrafted instead of random (it replaces the old twelve-Demon level):

1. **The walk.** A small landing, then a plank bridge with rails, three tiles wide, running north into the dark. You walk it with the normal prompts. The music is gone; only the cave ambience is left.
2. **It rises.** At row `MapSystem.BossRow` the bridge shakes and the camera moves up to frame the fight. Two hands claw up out of the void on either side, the mask drifts down out of the dark at the far end of the bridge, and THE SCRAWL is announced. The boss theme starts, and the move prompts go away: from now on you only move by dodging.
3. **The fight** (below).
4. **The end.** The mask shakes, cracks and falls apart, and whatever is left of the hands sinks into the void. The music falls silent, the camera returns to the knight, and the exit appears on the far landing; walking into it runs the ending (CONGRATULATIONS, then the victory screen).

## The fight

The loop is dodge, punish, dodge. The Scrawl acts in turns, one at a time: **left hand, right hand, beam**, with a short pause before each.

- **Lanes.** The bridge is three lanes wide; the knight stands in one.
- **A hand's slam.**
  - *Telegraph:* the hand rises over the lanes it will hit, splays its talons, trembles and burns red. A red tint darkens those lanes, a rising charge sound plays, and the dodge words appear with a draining timer bar.
  - *The rule:* the slam covers your lane and the lane on the hand's side, so the safe step is always away from the hand. In the centre lane both dodge words show and only one is safe; on an edge lane only one shows, because the other side is the void (and a hand reaching across from the far side covers only your lane).
  - *Dodge:* typing a dodge word steps the knight one lane that way. Typing the wrong one steps you into the slam.
  - *Impact:* camera shake, smoke, the bridge booming; 3 damage if you're in it (4 on Impossible).
  - *Punish:* after a slam the hand grips the edge of the bridge for a few seconds, and only then does its attack word appear. That window is when you can hurt it.
- **The head** can be attacked at any time; its word is always up. It has much more health than a hand, and killing it ends the fight even with hands left.
- **The head's beam:** the mouth charges and a stream of letters pours down your lane. It hits only your lane, so either dodge is safe, but it lands sooner than a slam (0.7 of the warning). 2 damage (3 on Impossible).
- **Breaking a hand** sinks it into the void, and its turns become breathers: so going for the hands makes the fight calmer, going for the head makes it shorter. With both hands gone, the head drifts closer and beams every turn.
- **Desperation:** once the head is cracked (half health or less), everything comes 15% faster.
- **Difficulty** (Normal, Hard, Impossible): slam warning 2.5, 1.8, 1.3 s; a hand rests 3.5, 3, 2.6 s; the pause between turns is 1.2, 0.9, 0.6 s; health is scaled by 1, 1.25, 1.5.
- **Words:** dodges are 3 letters, so they can be typed under pressure; hand words are 6, head words 8. No two words on screen share a first letter, and with WASD quick moves on, none start with W, A, S or D. A struck part's next word appears when the blow lands.
- **Health** is counted in strikes of the knight's own damage, so power collected on levels 1 to 4 shortens the fight, but only so far: before difficulty, a hand takes 3 strikes and the head 12, times 1 + 0.5 / power. A knight with no power needs half as many again; at power 5, a hand takes 4 strikes on Hard and the head 16.
- **Stats:** each part broken counts as a kill; beating the Scrawl adds 1000 points (times the difficulty, like the rest of the score).

Balance, measured with the probe's autopilot and the Scrawler (see Testing): at a steady 60 wpm with power 3 on Hard, the fight takes about 50 seconds and costs a few hearts (all from warnings that came mid-word). The Scrawler, at 85 wpm with 8 hearts and power 5, beat it on Impossible in about 70 seconds, taking one hit.

## Interface

- **Boss bar:** at the bottom centre, in the menus' frame style: THE SCRAWL, with a health bar each for the left hand, the head and the right hand.
- **Prompts:** attack words on plaques over each hand and beside the mask. Dodge words on plaques either side of the knight, with left and right arrows and a timer bar under the plaque that drains gold, then red.
- **Lane warnings:** a dark red tint on the lanes about to be hit, deepening as the blow nears.

## Audio

Generated in `Tools/Audio`:

- **The Scrawl's theme** (`ScrawlTheme`): D minor like the dungeon theme, so typing still plays along, but faster (132 bpm) and darker: a flat second (Eb) for menace, a grinding bass, a bell that tolls at the top of every phrase.
- **Effects:** `BossRumble` (it rises), `BossCharge` (a hand or the mask charging), `BossSlam`, `BossBeam`, `BossHit` (a strike landing on it, over the fireball's own sounds), `BossSink` (a hand breaking), `BossDeath` (the mask shattering and the howl into the dark).

## How it fits the code

- `MapSystem`: on the last level (`MapSystem.IsBossLevel`) it builds the bridge map instead of a random one: still a RogueSharp `Map`, so walking, lighting and visibility work. The bridge is painted with three `BridgeTile`s (left rail, planks, right rail); the knight starts at `SpawnPoint`.
- `ActorSpawnerSystem.SpawnLevel5Actors` spawns only the player. `TurnSystem` copes with no creatures taking turns.
- `ItemSystem` keeps the exit hidden on the boss level until `OpenExit()`.
- `ECS.Systems.BossSystem` (the last of the `Systems`) keeps the walk quiet and starts the fight when the knight reaches `BossRow`.
- `Boss.Scrawl` (the `Scrawl` object in `Game.unity`) runs the fight as a coroutine: intro, the turns, words, damage, defeat. `ScrawlHand` and `ScrawlHead` are plain sprite objects animated with DOTween, not grid creatures; `BossBar` is the bar on the `Game` canvas.
- `InputController`: `AddWord`/`RemoveWord` for words that run an action when typed (the dodges and strikes), and `MovementLocked` to put the move prompts away.
- Reused: the prompt plaques (`SpellHint` gained dodge icons, a timer bar and fixed offsets), `Fireball` for strikes, `TextPopup`, `Effect`, `SpriteFlash`, `CameraMovement.Shake` and its new `Focus`, `AttackSystem.HurtPlayer`, `RunStats`, the ending.

## Testing

- **In the game:** in the Editor or a development build, start a run and press **F8** during a level. It jumps to the bridge with a knight kitted out as from a run that looted about half the chests (8 hearts, power 5, light 6); walk north to wake the Scrawl. Pressing F8 on the bridge starts it over. F9 and F10 still win or lose on the spot.
- **Watching the Scrawler fight it:** `Tools/Scrawler/scrawl.sh practice --level 5 --health 8 --damage 5 --reach 5 --sight 6` (practice runs are on Impossible; see its README for the clean-up).
- **From the command line:** `Tools/Probe/probe.sh Level 5`, then `Knight`, `Fight` and `Autopilot` (see `Tools/Probe/README.md`).

## Build status

1. [x] Mockup of the arena, hands, mask and prompts, approved by the user on 2026-11-01. (Second version, after "the hands look a bit derpy, so does the face": claws instead of mitten hands and a fist; a long angular mask instead of a round one.)
2. [x] The bridge level and the walk.
3. [x] The boss sprites and their animations.
4. [x] The fight logic.
5. [x] Audio.
6. [x] Balance on Hard and Impossible with the autopilot and the Scrawler; the Scrawler fights it; docs. Still to do by hand: play it yourself on all three difficulties, and listen to the theme and effects.
