// A probe into a playing Editor, for checking the game from the command line (see README.md):
//   Tools/Probe/probe.sh State
//   Tools/Probe/probe.sh Type "kick" 0.12
// It is compiled in memory by the Unity CLI's `script run`, so it never ships with the game.

using System.Collections;
using System.IO;
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
        log.AppendLine(string.Format("music {0}  sound {1}  keys {2}", Volume.MusicLevel, Volume.SoundLevel, MoveKeys.Name));
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

    /// <summary>
    /// Jumps straight to a level. To 5, the bridge and the Scrawl, the knight goes kitted out like F8 does it.
    /// </summary>
    public static string Level(string level)
    {
        var game = Object.FindAnyObjectByType<GameController>();
        if (int.Parse(level) == RunStats.Levels) ECS.Entities.Blueprint.Player.SavePlayerStats(8, 5, 5, 6);
        if (game != null) game.IsPlaying = false;
        GameController.CurrentLevel = int.Parse(level);
        SceneManager.LoadScene("Game");
        return "loading level " + level;
    }

    /// <summary>
    /// Sets the knight's hearts (up to 10) and damage, e.g. to test the Scrawl without a whole run behind you.
    /// </summary>
    public static string Knight(string health, string damage)
    {
        var player = Utilities.Game.ECSCache.ActorCache.Instance.Player;
        player.Entity.GetComponent<ECS.Components.IntegerComponent>(ECS.Components.Type.ComponentType.Health).Value = int.Parse(health);
        player.Entity.GetComponent<ECS.Components.IntegerComponent>(ECS.Components.Type.ComponentType.Damage).Value = int.Parse(damage);
        var hearts = Object.FindAnyObjectByType<UI.HealthDisplay>();
        if (hearts != null) hearts.SetHealth(int.Parse(health));
        return "knight: " + health + " hearts, damage " + damage;
    }

    /// <summary>
    /// The fight with the Scrawl: where the knight stands, and what is left of each part.
    /// </summary>
    public static string Fight()
    {
        var log = new StringBuilder();
        var player = Utilities.Game.ECSCache.ActorCache.Instance.Player;
        if (player != null)
        {
            var at = player.Entity.GetComponent<ECS.Components.GridPositionComponent>().Position;
            var health = player.Entity.GetComponent<ECS.Components.IntegerComponent>(ECS.Components.Type.ComponentType.Health);
            var damage = player.Entity.GetComponent<ECS.Components.IntegerComponent>(ECS.Components.Type.ComponentType.Damage);
            log.AppendLine(string.Format("knight at {0}  lane {1}  health {2}  damage {3}", at, at.x - ECS.Systems.MapSystem.BridgeX, health != null ? health.Value : -1, damage != null ? damage.Value : -1));
        }

        foreach (var hand in Object.FindObjectsByType<global::Boss.ScrawlHand>(FindObjectsInactive.Include))
            log.AppendLine(string.Format("  {0}: {1}/{2}  active {3}  at {4}", hand.name, hand.Health, hand.MaxHealth, hand.gameObject.activeSelf, hand.transform.position));
        foreach (var head in Object.FindObjectsByType<global::Boss.ScrawlHead>(FindObjectsInactive.Include))
            log.AppendLine(string.Format("  head: {0}/{1}  active {2}  at {3}", head.Health, head.MaxHealth, head.gameObject.activeSelf, head.transform.position));
        log.Append(Hints());
        return log.ToString();
    }

    private static float _busyUntil;

    /// <summary>
    /// Fights the Scrawl for a while, typing a letter every `gap` seconds: the safe dodge when one is offered, else a
    /// resting hand's word, else the head's. Mode `safe` plays like that; `unsafe` takes the wrong dodge, to test
    /// getting hit; `hands` leaves the head alone until both hands are broken, to reach the last phase.
    /// </summary>
    public static string Autopilot(string seconds, string gap, string mode)
    {
        var input = Object.FindAnyObjectByType<InputController>();
        var append = typeof(InputController).GetMethod("AppendLetter", Priv);
        var interval = float.Parse(gap, System.Globalization.CultureInfo.InvariantCulture);
        var duration = float.Parse(seconds, System.Globalization.CultureInfo.InvariantCulture);
        _busyUntil = 0f;
        var dummy = 0f;
        DOTween.To(() => dummy, x => dummy = x, 1f, duration).OnUpdate(() =>
            {
                if (Time.time < _busyUntil) return;
                var word = Choose(mode);
                if (word == null) return;
                var sequence = DOTween.Sequence();
                foreach (var c in word)
                {
                    var letter = c;
                    sequence.AppendCallback(() => append.Invoke(input, new object[] { letter }));
                    sequence.AppendInterval(interval);
                }

                _busyUntil = Time.time + interval * word.Length + 0.1f;
            });
        return "autopilot for " + seconds + "s";
    }

    private static string Choose(string mode)
    {
        var unsafeDodge = mode == "unsafe";
        var handsLeft = false;
        foreach (var hand in Object.FindObjectsByType<global::Boss.ScrawlHand>())
            handsLeft |= hand.Alive;
        var player = Utilities.Game.ECSCache.ActorCache.Instance.Player;
        if (player == null) return null;
        var knight = player.Entity.GetComponent<ECS.Components.GridPositionComponent>().Position;
        var hints = new System.Collections.Generic.List<SpellHint>();
        foreach (var hint in HintCache.Instance.GetCached())
            if (hint.gameObject.activeInHierarchy && !string.IsNullOrEmpty(hint.Word) && hint.Word.Length > 1) hints.Add(hint);

        // A dodge sits beside the knight; the lane shadow shows where the blow will fall.
        var shadow = GameObject.Find("LaneShadow");
        var low = shadow.transform.position.x - shadow.transform.localScale.x / 8f;
        var high = shadow.transform.position.x + shadow.transform.localScale.x / 8f;
        foreach (var hint in hints)
        {
            if (hint.Word.Length != 3 || Mathf.Abs(hint.transform.position.y - knight.y) > 1f) continue;
            var to = knight.x + (hint.transform.position.x < knight.x ? -1 : 1);
            var safe = to < low || to > high;
            if (safe != unsafeDodge) return hint.Word;
        }

        foreach (var hint in hints)
            if (hint.Word.Length == 6) return hint.Word;
        if (mode == "hands" && handsLeft) return null;
        foreach (var hint in hints)
            if (hint.Word.Length == 8) return hint.Word;
        return null;
    }

    /// <summary>
    /// Records the Game view for a while: every frame as a PNG in `folder`, with the game's clock fixed to `fps`
    /// frames a second (Time.captureFramerate), so the frames are evenly spaced however long each takes to save.
    /// frames.tsv lists each frame's game time and level. It carries on across level loads.
    /// </summary>
    public static string Record(string seconds, string fps, string folder)
    {
        var old = Object.FindAnyObjectByType<ProbeRecorder>();
        if (old != null) Object.Destroy(old.gameObject);
        var go = new GameObject("ProbeRecorder");
        Object.DontDestroyOnLoad(go);
        var recorder = go.AddComponent<ProbeRecorder>();
        recorder.Folder = folder;
        recorder.Rate = int.Parse(fps);
        recorder.Frames = Mathf.RoundToInt(float.Parse(seconds, System.Globalization.CultureInfo.InvariantCulture) * recorder.Rate);
        return string.Format("recording {0} frames at {1} fps to {2}", recorder.Frames, recorder.Rate, folder);
    }

    public static string StopRecording()
    {
        var recorder = Object.FindAnyObjectByType<ProbeRecorder>();
        if (recorder == null) return "not recording";
        var frames = recorder.Written;
        Object.Destroy(recorder.gameObject);
        Time.captureFramerate = 0;
        return "stopped after " + frames + " frames";
    }

    public static string Popup(string text, string size)
    {
        var camera = Camera.main.transform.position;
        ObjectPools.Instance.GetPooledObject<TextPopup>().Enable(text, new Vector2(camera.x, camera.y), 6f, float.Parse(size, System.Globalization.CultureInfo.InvariantCulture));
        return "popup " + text;
    }

    /// <summary>
    /// Sets the player's settings, e.g. to put them back after a test: music and sound 0-3, keys wasd, zqsd or letters.
    /// </summary>
    public static string Settings(string music, string sound, string keys)
    {
        Volume.MusicLevel = int.Parse(music);
        Volume.SoundLevel = int.Parse(sound);
        MoveKeys.Keys = keys == "zqsd" ? MoveKeys.Layout.Zqsd : keys == "letters" ? MoveKeys.Layout.Letters : MoveKeys.Layout.Wasd;
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

/// <summary>
/// Saves the Game view every frame for Probe.Record.
/// </summary>
public class ProbeRecorder : MonoBehaviour
{
    public string Folder;

    public int Rate;

    public int Frames;

    public int Written;

    private IEnumerator Start()
    {
        Directory.CreateDirectory(Folder);
        var index = new StringBuilder("frame\ttime\tscene\tlevel\n");
        Time.captureFramerate = Rate;
        for (Written = 0; Written < Frames; Written++)
        {
            yield return new WaitForEndOfFrame();
            var shot = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(Folder, string.Format("f{0:00000}.png", Written)), shot.EncodeToPNG());
            Destroy(shot);
            index.AppendFormat("{0}\t{1:0.000}\t{2}\t{3}\n", Written, Time.time, SceneManager.GetActiveScene().name, GameController.CurrentLevel);
            File.WriteAllText(Path.Combine(Folder, "frames.tsv"), index.ToString());
        }

        Time.captureFramerate = 0;
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        Time.captureFramerate = 0;
    }
}
