# Probe

Checks the game from the command line while it plays in the Editor: what scene and music are up, what the prompts say, typing a word, winning or losing on the spot, driving the pause menu, and screenshots of the Game view. It is how the graphics, sound and interface work in this repo was verified.

It lives outside `Assets/`, so Unity never compiles it into the game. `Probe.cs` is compiled in memory by the Unity CLI's `script run`.

## Use

You need the project open in the Editor, the `unity` CLI, bash (Git Bash on Windows) and Python 3.

```sh
unity command editor.playmode start --project-path .
# The Editor only renders frames while focused. Keep it ticking for the CLI (play mode resets this):
unity command editor set_auto_tick --enable true --project-path .

Tools/Probe/probe.sh State                 # scene, time, settings, music
Tools/Probe/probe.sh Hints                 # the prompts on screen (Game scene)
Tools/Probe/probe.sh Type "kick" 0.12      # type a word, a letter every 0.12s
Tools/Probe/probe.sh Stats                 # the run's numbers so far
Tools/Probe/probe.sh Win                   # win on the spot (like F9); Die loses (like F10)
Tools/Probe/probe.sh Pause open            # also resume, options, back, quit
Tools/Probe/probe.sh Popup "-LEVEL 2-" 2   # a floating message, at size 1 or 2
Tools/Probe/probe.sh Load MainMenu         # load a scene, as its buttons would
Tools/Probe/capture.sh shot.png            # the Game view at 1920x1080

unity command editor.playmode stop --project-path .
```

The Escape key, the arrow keys and holding a key down can't be sent from outside: try those by hand.

## Clean up after testing

- A test run that ends saves a best score: `Tools/Probe/probe.sh ClearBest`.
- If a test changed the player's settings, put them back: `Tools/Probe/probe.sh Settings 2 2 true` (music, sound, WASD).
- Playing modifies `Assets/TextMesh Pro/Resources/Fonts & Materials/Electronic Highway Sign SDF.asset`. Revert it before committing: `git checkout -- "Assets/TextMesh Pro/Resources/Fonts & Materials/Electronic Highway Sign SDF.asset"`.

## Gotchas

- Wait for a level to be playing (`State` shows `playing True`) before `Win`, typing or pausing: during the level's start-up, the game turns play back on when it finishes.
- A player left standing next to a monster dies. If `Hints` fails with a null reference, check `State`: you may be on the game over screen.
- `unity recompile` sometimes answers `400 Bad Request` while the Editor is refreshing assets. Wait a few seconds and run it again.
- To play a whole level without typing yourself, use the Scrawler bot (`Tools/Scrawler`).
