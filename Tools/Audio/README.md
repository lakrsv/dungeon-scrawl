# Audio

Generates the game's newer sounds as code: the typing sounds, footsteps, the death puff, the menu clicks, the title and dungeon themes, and the cave ambience. It is a small chiptune synthesizer in plain Python (no packages needed), so everything sounds like one small machine and matches the original Ludum Dare effects.

The original effects (`ChestOpen`, `Damage`, `Explosion`, `GameOver`, `ItemApply`, `LevelText`, `Powerup`, `Shoot`, `Teleport`) are hand-made and not touched by this tool.

It lives outside `Assets/`, so Unity never imports it.

## Running it

You need Python 3, and the project open in the Unity Editor.

```sh
python Tools/Audio/build.py              # everything into Assets/Audio (the music takes about 20 seconds)
python Tools/Audio/build.py --sfx        # only the sound effects
python Tools/Audio/build.py Key Typo     # only the named sounds

# Apply import settings in the open Editor (only needed for new files; settings stay in the .meta files):
unity command script run --file Tools/Audio/ImportAudio.cs --entry ImportAudio.Run
```

`build.py` prints each sound's peak and average level. For the loops it also prints the seam: the jump from the last sample to the first, next to a typical step between samples. Anything near or below 1 loops without a click.

## Files

- `synth.py`: oscillators (anti-aliased pulse, triangle, sine, noise), envelopes, filters, echo, a small Freeverb, and WAV output.
- `sfx.py`: the sound effects. The typed-letter blip is tuned to A4. The game raises its pitch letter by letter up the dungeon theme's D minor pentatonic scale (`AudioPlayer.LetterSteps`), so typing plays along with the music.
- `music.py`: the two themes, written out as chords and melodies, and the ambience. Echo and reverb tails are folded back onto the start of each loop, so the loop point is seamless.
- `build.py`: writes `Assets/Audio/*.wav`.
- `ImportAudio.cs`: the loops stream as Vorbis; the effects load uncompressed, so they play the instant they are triggered.

## In the game

- `AudioPlayer` (Game scene) plays the effects. The letter blips have their own `AudioSource`, so changing their pitch never bends other sounds.
- `Sound.Jukebox` plays the music, the ambience, and the menu clicks. Every scene has one that names what plays there. The first one to wake survives scene loads, so music carries on across the splash screen, the menu, and level reloads; later ones hand over their tracks and remove themselves. A change of track crossfades.
- `Sound.Volume` holds the music and sound levels (full, half, quarter, off), saved in `PlayerPrefs`. The volume buttons on the main menu step through them.
