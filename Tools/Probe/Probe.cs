// A probe into a playing Editor, for checking the game from the command line (see README.md):
//   Tools/Probe/probe.sh State
//   Tools/Probe/probe.sh Type "kick" 0.12
// It is compiled in memory by the Unity CLI's `script run`, so it never ships with the game.

using System.Reflection;
using System.Text;

using Controllers;

using DG.Tweening;

using Sound;

using UI;
using UI.Hint;
using UI.Notification;

using UnityEngine;
using UnityEngine.SceneManagement;

using Utilities.Game.ObjectPool;

using Object = UnityEngine.Object;

public static class Probe
{
    private const BindingFlags Priv = BindingFlags.NonPublic | BindingFlags.Instance;

    /// <summary>
    /// The scene, time, settings and what the jukebox is playing.
    /// </summary>
    public static string State()
    {
        var log = new StringBuilder();
        var game = Object.FindAnyObjectByType<GameController>();
        log.AppendLine(string.Format(
            "scene {0}  time {1:0.0}  timeScale {2}  level {3}  playing {4}  paused {5}",
            SceneManager.GetActiveScene().name, Time.time, Time.timeScale, GameController.CurrentLevel,
            game != null && game.IsPlaying, game != null && game.IsPaused));
        log.AppendLine(string.Format("music {0}  sound {1}  wasd {2}", Volume.MusicLevel, Volume.SoundLevel, MoveKeys.Wasd));
        foreach (var jukebox in Object.FindObjectsByType<Jukebox>())
            foreach (var source in jukebox.GetComponents<AudioSource>())
                if (source.clip != null)
                    log.AppendLine(string.Format("  {0}: playing {1}, volume {2:0.000}, at {3:0.0}s", source.clip.name, source.isPlaying, source.volume, source.time));
        return log.ToString();
    }

    public static string Load(string scene)
    {
        SceneManager.LoadScene(scene);
        return "loading " + scene;
    }

    /// <summary>
    /// The prompts on screen and their words. Only in the Game scene.
    /// </summary>
    public static string Hints()
    {
        var log = new StringBuilder();
        foreach (var hint in HintCache.Instance.GetCached())
        {
            var text = hint.GetComponentInChildren<TMPro.TMP_Text>();
            log.AppendLine(string.Format("  {0}: {1}  ('{2}')", hint.name, hint.Word, text != null ? text.text : string.Empty));
        }

        return log.ToString();
    }

    /// <summary>
    /// Types like a person, one letter every `interval` seconds, through the same path as the keyboard.
    /// </summary>
    public static string Type(string text, string interval)
    {
        var input = Object.FindAnyObjectByType<InputController>();
        var append = typeof(InputController).GetMethod("AppendLetter", Priv);
        var gap = float.Parse(interval, System.Globalization.CultureInfo.InvariantCulture);
        var sequence = DOTween.Sequence();
        foreach (var c in text)
        {
            var letter = c;
            sequence.AppendCallback(() => append.Invoke(input, new object[] { letter }));
            sequence.AppendInterval(gap);
        }

        return "typing " + text;
    }

    public static string Stats()
    {
        return string.Format(
            "letters {0}  mistakes {1}  word letters {2}  kills {3}  chests {4}  cleared {5}  reached {6}  won {7}  {8:0.0}s  {9} wpm  {10}%  score {11}  best {12}  new {13}",
            RunStats.Letters, RunStats.Mistakes, RunStats.WordLetters, RunStats.Kills, RunStats.Chests, RunStats.LevelsCleared,
            RunStats.LevelReached, RunStats.Won, RunStats.Seconds, RunStats.WordsPerMinute, RunStats.Accuracy, RunStats.Score,
            RunStats.Best, RunStats.NewBest);
    }

    /// <summary>
    /// Wins the run on the spot, like F9. Only while a level is being played.
    /// </summary>
    public static string Win()
    {
        var game = GameController.Instance;
        if (!game.IsPlaying) return "not playing yet";
        GameController.CurrentLevel = RunStats.Levels;
        game.EndLevel();
        return "won";
    }

    /// <summary>
    /// Loses the run on the spot, like F10.
    /// </summary>
    public static string Die()
    {
        GameController.Instance.GameOver();
        return "game over";
    }

    /// <summary>
    /// Drives the pause menu as Escape and its buttons would: open, resume, options, back, quit.
    /// </summary>
    public static string Pause(string action)
    {
        var menu = Object.FindAnyObjectByType<PauseMenu>();
        switch (action)
        {
            case "open":
                menu.Open();
                break;
            case "resume":
                menu.Resume();
                break;
            case "options":
                menu.OpenOptions();
                break;
            case "back":
                menu.CloseOptions();
                break;
            case "quit":
                menu.QuitToTitle();
                break;
        }

        return action + ": " + State();
    }

    public static string Popup(string text, string size)
    {
        var camera = Camera.main.transform.position;
        ObjectPools.Instance.GetPooledObject<TextPopup>().Enable(text, new Vector2(camera.x, camera.y), 6f, float.Parse(size, System.Globalization.CultureInfo.InvariantCulture));
        return "popup " + text;
    }

    /// <summary>
    /// Sets the player's settings, e.g. to put them back after a test: music and sound 0-3, wasd true or false.
    /// </summary>
    public static string Settings(string music, string sound, string wasd)
    {
        Volume.MusicLevel = int.Parse(music);
        Volume.SoundLevel = int.Parse(sound);
        MoveKeys.Wasd = bool.Parse(wasd);
        return State();
    }

    /// <summary>
    /// Forgets the best score: test runs save one.
    /// </summary>
    public static string ClearBest()
    {
        PlayerPrefs.DeleteKey("BestScore");
        PlayerPrefs.Save();
        return "best score cleared";
    }
}
