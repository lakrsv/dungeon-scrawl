// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="Scrawler.cs" author="Lars" company="None">
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), 
// to deal in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software,
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// // </copyright>
// // <summary>
// //   Plays Dungeon Scrawl through the keyboard input path, seeing only what is on screen. See README.md.
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

// Scrawler: plays Dungeon Scrawl.
//
// Fair-play rules it holds itself to:
//  * Input only through InputController.AppendLetter(char) - what Update() calls for every char of
//    Input.inputString - at human speed: 0.12-0.16s per key (~85 WPM) and 0.25-0.35s to read a new prompt.
//  * Sees only what is on screen: explored/lit tiles, drawn sprites (by their look), prompt labels, its own HUD
//    stats. Never reads enemy health, turn state, unexplored map, or which "chest" is really an enemy.
//    Enemy stats come from the game's rules (what a player learns from playing).
//  * Changes nothing: no health/stat edits, no pausing, no time scale changes in the official run.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using Controllers;
using ECS.Components;
using ECS.Components.Type;
using ECS.Systems;
using UI;
using UI.Hint;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utilities;
using Utilities.Game;
using Utilities.Game.ECSCache;
using Utilities.Game.Navigation;

using Object = UnityEngine.Object;
using Random = System.Random;

public class ScrawlerHost : MonoBehaviour
{
}

public static class Scrawler
{
    private const BindingFlags Priv = BindingFlags.NonPublic | BindingFlags.Instance;

    // Typing profile, in game seconds. "85": 0.12-0.16s per key, 0.25-0.35s to read a prompt.
    // "120": 0.08-0.12s per key, 0.20-0.30s to read a prompt.
    private static float KeyMin = 0.12f;
    private static float KeyMax = 0.16f;
    private static float ReactMin = 0.25f;
    private static float ReactMax = 0.35f;
    private static string _profile = "120";

    private static void SetProfile(string wpm)
    {
        _profile = wpm == "85" ? "85" : "120";
        if (_profile == "120") { KeyMin = 0.08f; KeyMax = 0.12f; ReactMin = 0.20f; ReactMax = 0.30f; }
        else { KeyMin = 0.12f; KeyMax = 0.16f; ReactMin = 0.25f; ReactMax = 0.35f; }
    }

    private static readonly int[] SpawnCount = { 0, 8, 12, 12, 11, 12 };

    private static ScrawlerHost _host;
    private static Random _rng = new Random();
    private static int _sceneLoads;
    private static string _status = "idle";
    private static readonly List<string> Events = new List<string>();

    private enum Kind { Pest, Slime, Demon, FakeChest, Dead }

    private enum ChestState { Unverified, Real, Fake, Gone }

    private enum ActKind { Move, Attack, Loot }

    private class Enemy
    {
        public int Id;
        public Vector2Int Pos;
        public Kind Kind;
        public Transform T;
    }

    private class Prompt
    {
        public string Word;
        public SpellHint Hint;
        public bool IsMove;
        public bool IsAttack;
        public bool IsLoot;
        public Direction Dir;
        public Transform Target;
    }

    private class View
    {
        public Vector2Int Me;
        public int Hp;
        public int Dmg;
        public int Reach;
        public int Fov;
        public readonly List<Enemy> Enemies = new List<Enemy>();
        public readonly List<KeyValuePair<Vector2Int, Transform>> ChestLooking = new List<KeyValuePair<Vector2Int, Transform>>();
        public readonly List<Prompt> Prompts = new List<Prompt>();
        public Vector2Int? Door;
    }

    private class Model
    {
        public int Level;
        public int Difficulty;
        public int Kills;
        public int LastHp;
        public float StartTime;
        public Vector2Int? Door;
        public readonly Dictionary<Vector2Int, ChestState> Chests = new Dictionary<Vector2Int, ChestState>();
        public readonly Dictionary<int, int> Dealt = new Dictionary<int, int>();
        public readonly Dictionary<int, Kind> Seen = new Dictionary<int, Kind>();
        public readonly Dictionary<int, KeyValuePair<Vector2Int, float>> DemonMemory = new Dictionary<int, KeyValuePair<Vector2Int, float>>();
        public string LastGoal;
        public View Latest;
        public readonly Dictionary<int, Vector2Int> Stable = new Dictionary<int, Vector2Int>();
        public readonly Dictionary<int, float> ActStart = new Dictionary<int, float>();
        public readonly List<float> Intervals = new List<float>();
        public float LastAction;
        public readonly Dictionary<int, float> VisibleSince = new Dictionary<int, float>();
        public bool Frozen;
        public float QuietObserved;
        public float QuietMark;
        public bool Acted;
        public Vector2Int? Goal;
        public string GoalWhy;
        public float WaitSince = -1f;
    }

    private class Act
    {
        public ActKind Kind;
        public Prompt Prompt;
        public Enemy Target;
        public string Why;
    }

    private static Sprite[] _pest, _demon, _slime, _chest, _death;

    // ---------------------------------------------------------------- entry points

    public static string Official(string wpm)
    {
        SetProfile(wpm);
        if (SceneManager.GetActiveScene().name != "SplashScreen") return "Start Play mode on the SplashScreen scene first";
        if (Math.Abs(Time.timeScale - 1f) > 0.001f) return "time scale must be 1 for the official run";
        Begin(true);
        return "Scrawler: official run started, typing profile ~" + _profile + " WPM";
    }

    // Practice from any level with given stats. Only for tuning; never used in the official run.
    public static string Practice(string timeScale, string level, string hp, string dmg, string reach, string fov, string wpm)
    {
        SetProfile(wpm);
        PlayerPrefs.SetInt("Difficulty", 3);
        PlayerPrefs.SetInt("Health", int.Parse(hp));
        PlayerPrefs.SetInt("Damage", int.Parse(dmg));
        PlayerPrefs.SetInt("Reach", int.Parse(reach));
        PlayerPrefs.SetInt("FieldOfView", int.Parse(fov));
        GameController.CurrentLevel = int.Parse(level);
        Time.timeScale = float.Parse(timeScale);
        Begin(false);
        SceneManager.LoadScene("Game");
        return "Scrawler: practice from level " + level + " at time scale " + timeScale + ", typing ~" + _profile + " WPM";
    }

    public static string Report()
    {
        var sb = new StringBuilder();
        sb.AppendLine("status: " + _status);
        foreach (var e in Events.Skip(Math.Max(0, Events.Count - 60))) sb.AppendLine(e);
        return sb.ToString();
    }

    public static string Stop()
    {
        if (_host != null) Object.Destroy(_host.gameObject);
        _host = null;
        Time.timeScale = 1f;
        return "stopped";
    }

    private static void Begin(bool official)
    {
        Stop();
        Events.Clear();
        _rng = new Random();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        var go = new GameObject("Scrawler");
        Object.DontDestroyOnLoad(go);
        _host = go.AddComponent<ScrawlerHost>();
        _host.StartCoroutine(Main(official));
    }

    private static void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        _sceneLoads++;
    }

    private static void Log(string msg)
    {
        var line = string.Format("[{0,6:F1}s] {1}", Time.timeSinceLevelLoad, msg);
        Events.Add(line);
        Debug.Log("[SCRAWL] " + line);
    }

    // ---------------------------------------------------------------- flow

    private static IEnumerator Main(bool official)
    {
        if (official)
        {
            _status = "splash screen";
            yield return new WaitForSeconds(3f);
            Log("Splash screen: pressing a key (the splash's Input.anyKeyDown handler loads MainMenu)");
            SceneManager.LoadScene("MainMenu");
            while (SceneManager.GetActiveScene().name != "MainMenu") yield return null;
            yield return new WaitForSeconds(2f);

            var button = Object.FindObjectsByType<PlayButton>(FindObjectsSortMode.None).OrderByDescending(b => b.Difficulty).First();
            var tmp = button.GetComponentInChildren<TMPro.TMP_Text>();
            var legacy = button.GetComponentInChildren<UnityEngine.UI.Text>();
            var text = tmp != null ? tmp.text : legacy != null ? legacy.text : "?";
            Log(string.Format("Main menu: clicking '{0}' (difficulty {1}, the hardest)", text, button.Difficulty));
            ((UnityEngine.UI.Button)typeof(PlayButton).GetField("_button", Priv).GetValue(button)).onClick.Invoke();
        }

        var loadsSeen = _sceneLoads;
        while (_host != null)
        {
            while (SceneManager.GetActiveScene().name != "Game" || _sceneLoads == loadsSeen) yield return null;
            loadsSeen = _sceneLoads;

            while (GameController.Instance == null || !GameController.Instance.IsPlaying) yield return null;
            var level = GameController.CurrentLevel;
            _status = "playing level " + level;

            var outcome = new string[1];
            yield return PlayLevel(level, outcome);

            if (outcome[0] == "dead")
            {
                _status = "died on level " + level;
                Log("*** DIED on level " + level + " ***");
                Time.timeScale = 1f;
                yield break;
            }

            if (level == 5)
            {
                _status = "WON";
                Log("*** Reached the exit of level 5 - game won ***");
                Time.timeScale = 1f;
                yield break;
            }

            Log("Level " + level + " complete");
        }
    }

    private static IEnumerator PlayLevel(int level, string[] outcome)
    {
        LoadSprites();
        var m = new Model { Level = level, Difficulty = PlayerPrefs.GetInt("Difficulty", 1), StartTime = Time.time };
        var v0 = Perceive(m);
        m.LastHp = v0.Hp;
        Log(string.Format("=== Level {0} === HP {1}, damage {2}, reach {3}, sight {4}", level, v0.Hp, v0.Dmg, v0.Reach, v0.Fov));

        var tracking = new bool[] { true };
        _host.StartCoroutine(TrackLoop(m, tracking));

        while (GameController.Instance != null && GameController.Instance.IsPlaying)
        {
            Act act = null;
            View v = null;
            try
            {
                v = Perceive(m);
                if (v.Hp <= 0) break;
                act = Decide(v, m);
            }
            catch (Exception e)
            {
                Log("tick error: " + e.GetType().Name + " " + e.Message + " @ " + e.StackTrace.Split('\n')[0]);
            }

            if (act == null)
            {
                yield return new WaitForSeconds(0.05f);
                continue;
            }

            yield return Execute(act, m, v);
        }

        tracking[0] = false;
        var player = ActorCache.Instance != null && ActorCache.Instance.Player != null ? ActorCache.Instance.Player.Entity : null;
        var hp = player != null ? player.GetComponent<IntegerComponent>(ComponentType.Health).Value : 0;
        outcome[0] = hp <= 0 ? "dead" : "exit";
        Log(string.Format("Level {0} ended after {1:F0}s: {2} (HP {3}, kills {4})", level, Time.time - m.StartTime, outcome[0], hp, m.Kills));
    }

    // Watch the screen every frame, so movements and lunges between keystrokes are not missed.
    private static IEnumerator TrackLoop(Model m, bool[] running)
    {
        while (running[0] && GameController.Instance != null && GameController.Instance.IsPlaying)
        {
            try
            {
                m.Latest = Perceive(m);
                Track(m.Latest, m);
            }
            catch (Exception)
            {
            }

            yield return null;
        }
    }

    // ---------------------------------------------------------------- perception (screen only)

    private static void LoadSprites()
    {
        var s = Sprites.Instance;
        var t = typeof(Sprites);
        _pest = (Sprite[])t.GetField("_pestSprites", Priv).GetValue(s);
        _demon = (Sprite[])t.GetField("_demonSprites", Priv).GetValue(s);
        _slime = (Sprite[])t.GetField("_slimeSprites", Priv).GetValue(s);
        _chest = (Sprite[])t.GetField("_chestSprites", Priv).GetValue(s);
        _death = (Sprite[])t.GetField("_deathSprites", Priv).GetValue(s);
    }

    private static Vector2Int Round(Vector3 p)
    {
        return new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y));
    }

    private static View Perceive(Model m)
    {
        var v = new View();
        var player = ActorCache.Instance.Player.Entity;
        v.Me = player.GetComponent<GridPositionComponent>().Position;
        v.Hp = player.GetComponent<IntegerComponent>(ComponentType.Health).Value;
        v.Dmg = player.GetComponent<IntegerComponent>(ComponentType.Damage).Value;
        v.Reach = player.GetComponent<IntegerComponent>(ComponentType.Reach).Value;
        v.Fov = player.GetComponent<IntegerComponent>(ComponentType.FieldOfView).Value;

        // Creatures and chest-shaped things that are currently drawn, classified by how they look.
        foreach (var actor in ActorCache.Instance.GetCached())
        {
            if (actor.Entity == player) continue;
            var r = actor.Entity.GetComponent<RenderComponent>();
            if (r == null || !r.Renderer.enabled) continue;
            var t = actor.Entity.GameObject.transform;
            var id = t.gameObject.GetInstanceID();
            var pos = Round(t.position);
            var offset = ((Vector2)t.position - (Vector2)pos).magnitude;
            Vector2Int stable;
            if (offset > 0.3f && m.Stable.TryGetValue(id, out stable)) pos = stable;
            m.Stable[id] = pos;
            if (offset > 0.2f)
            {
                // It is moving or lunging: its turn has started.
                float started;
                var has = m.ActStart.TryGetValue(id, out started);
                if (!has || Time.time - started > 0.9f)
                {
                    var floor = Math.Max(1, SpawnCount[m.Level] - m.Kills) * 0.4f / m.Difficulty * 0.8f;
                    var gap = Time.time - started;
                    if (has && gap < 6f && gap > floor) { m.Intervals.Add(gap); if (m.Intervals.Count > 12) m.Intervals.RemoveAt(0); }
                    m.ActStart[id] = Time.time;
                }

                m.LastAction = Time.time;
            }

            if (!m.VisibleSince.ContainsKey(id)) m.VisibleSince[id] = Time.time;

            var sprite = r.Renderer.sprite;
            if (_chest.Contains(sprite)) { v.ChestLooking.Add(new KeyValuePair<Vector2Int, Transform>(pos, t)); continue; }

            Kind kind;
            if (_pest.Contains(sprite)) kind = Kind.Pest;
            else if (_demon.Contains(sprite)) kind = Kind.Demon;
            else if (_slime.Contains(sprite)) kind = Kind.Slime;
            else if (_death.Contains(sprite)) kind = Kind.Dead;
            else continue;
            v.Enemies.Add(new Enemy { Id = id, Pos = pos, Kind = kind, T = t });
        }

        var chestRenderer = typeof(Chest).GetField("_renderer", Priv);
        foreach (var chest in Object.FindObjectsByType<Chest>(FindObjectsSortMode.None))
        {
            var sr = (SpriteRenderer)chestRenderer.GetValue(chest);
            if (sr == null || !sr.enabled || sr.color.a <= 0.01f) continue;
            v.ChestLooking.Add(new KeyValuePair<Vector2Int, Transform>(Round(chest.transform.position), chest.transform));
        }

        var door = Object.FindAnyObjectByType<Door>();
        if (door != null)
        {
            var sr = door.GetComponent<SpriteRenderer>();
            if (sr != null && sr.color.a > 0.01f) v.Door = Round(door.transform.position);
        }

        // Prompts on screen.
        var mh = Object.FindAnyObjectByType<MoveHints>();
        var textField = typeof(SpellHint).GetField("_text", Priv);
        var iconField = typeof(SpellHint).GetField("_icon", Priv);
        var attackIcon = typeof(SpellHint).GetField("_attackIcon", Priv);
        var follow = typeof(SpellHint).GetField("_followTarget", Priv);
        foreach (var hint in HintCache.Instance.GetCached())
        {
            if (hint == null || !hint.gameObject.activeInHierarchy) continue;
            var word = Regex.Replace(((TMPro.TMP_Text)textField.GetValue(hint)).text ?? string.Empty, "<[^>]*>", string.Empty);
            if (word.Length == 0) continue;
            var p = new Prompt { Word = word, Hint = hint };
            for (var i = 0; i < 4; i++)
            {
                if (mh != null && mh.GetHint((Direction)i) == hint) { p.IsMove = true; p.Dir = (Direction)i; }
            }

            if (!p.IsMove)
            {
                p.Target = (Transform)follow.GetValue(hint);
                if (p.Target == null) continue;
                var icon = (UnityEngine.UI.Image)iconField.GetValue(hint);
                if (icon.sprite == (Sprite)attackIcon.GetValue(hint)) p.IsAttack = true;
                else p.IsLoot = true;
            }

            v.Prompts.Add(p);
        }

        return v;
    }

    // Remember what has been seen: chests (and whether they are real), kills, damage taken, demons.
    private static void Track(View v, Model m)
    {
        var map = MapSystem.Instance.Map;
        if (v.Door.HasValue && !m.Door.HasValue)
        {
            m.Door = v.Door;
            Log("Spotted the exit at " + v.Door.Value);
        }

        foreach (var c in v.ChestLooking)
        {
            if (!m.Chests.ContainsKey(c.Key))
            {
                // Mimic chests only show up on levels 2 and 4.
                var mimicLevel = m.Level == 2 || m.Level == 4;
                m.Chests[c.Key] = mimicLevel ? ChestState.Unverified : ChestState.Real;
                Log("Spotted a chest at " + c.Key + (mimicLevel ? "" : " (no mimics on this level)"));
            }
            // Only real chests stay drawn (greyed) once out of sight; creatures disappear.
            if (m.Chests[c.Key] == ChestState.Unverified && !map.IsInFov(c.Key.x, c.Key.y))
            {
                m.Chests[c.Key] = ChestState.Real;
                Log("Chest at " + c.Key + " stays drawn out of sight: real");
            }
        }

        foreach (var pos in m.Chests.Keys.ToList())
        {
            if (m.Chests[pos] != ChestState.Unverified) continue;
            var drawn = v.ChestLooking.Any(c => c.Key == pos);
            if (map.IsExplored(pos.x, pos.y) && !map.IsInFov(pos.x, pos.y) && !drawn)
            {
                m.Chests[pos] = ChestState.Fake;
                Log("Chest at " + pos + " vanished when out of sight: it is a creature");
            }
        }

        // An attack prompt over a chest-shaped thing gives it away too.
        foreach (var p in v.Prompts.Where(p => p.IsAttack))
        {
            var tp = Round(p.Target.position);
            if (v.ChestLooking.Any(c => c.Value == p.Target) && (!m.Chests.ContainsKey(tp) || m.Chests[tp] != ChestState.Fake))
            {
                m.Chests[tp] = ChestState.Fake;
                Log("Chest at " + tp + " has an attack prompt: it is a creature");
            }
        }

        foreach (var e in v.Enemies)
        {
            Kind before;
            if (m.Seen.TryGetValue(e.Id, out before) && before != Kind.Dead && e.Kind == Kind.Dead)
            {
                m.Kills++;
                Log(before + " at " + e.Pos + " died");
            }

            m.Seen[e.Id] = e.Kind;
            if (e.Kind == Kind.Demon) m.DemonMemory[e.Id] = new KeyValuePair<Vector2Int, float>(e.Pos, Time.time);
            if (e.Kind == Kind.Dead) m.DemonMemory.Remove(e.Id);
        }

        foreach (var id in m.DemonMemory.Keys.ToList())
        {
            var mem = m.DemonMemory[id];
            var visible = v.Enemies.Any(e => e.Id == id);
            if (!visible && (Time.time - mem.Value > 20f || (map.IsInFov(mem.Key.x, mem.Key.y) && Time.time - mem.Value > 1f))) m.DemonMemory.Remove(id);
        }

        foreach (var id in m.VisibleSince.Keys.ToList())
        {
            if (!v.Enemies.Any(e => e.Id == id) && !v.ChestLooking.Any(c => c.Value.gameObject.GetInstanceID() == id)) m.VisibleSince.Remove(id);
        }

        if (m.LastAction > m.QuietMark) { m.QuietObserved = 0; m.QuietMark = m.LastAction; }
        if (m.Acted && v.Enemies.Any(e => ShouldBeActing(e, v, m))) m.QuietObserved += Time.deltaTime;

        var wasFrozen = m.Frozen;
        m.Frozen = IsFrozen(v, m);
        if (m.Frozen != wasFrozen) Log(m.Frozen ? "Everything has stopped moving: the turn order is frozen" : "Enemies are moving again");

        if (v.Hp != m.LastHp)
        {
            if (v.Hp < m.LastHp) { m.LastAction = Time.time; m.QuietObserved = 0; }
            Log(string.Format("HP {0} -> {1}", m.LastHp, v.Hp));
            m.LastHp = v.Hp;
        }
    }

    // ---------------------------------------------------------------- rules knowledge

    private static int MaxHp(Kind k, Model m)
    {
        var d = m.Difficulty;
        switch (k)
        {
            case Kind.Pest: return d;
            case Kind.Slime: return 5 * d;
            case Kind.Demon: return 4 * d;
            case Kind.FakeChest: return 2 + d;
            default: return 0;
        }
    }

    private static int Dmg(Kind k, Model m)
    {
        var d = m.Difficulty;
        switch (k)
        {
            case Kind.Pest: return d;
            case Kind.Slime: return 1 + d;
            case Kind.Demon: return 2 * d;
            case Kind.FakeChest: return 3 * d;
            default: return 0;
        }
    }

    private static int Reach(Kind k, Model m)
    {
        return k == Kind.Demon ? 2 + m.Difficulty : 1;
    }

    private static int Remaining(Enemy e, Model m)
    {
        int dealt;
        m.Dealt.TryGetValue(e.Id, out dealt);
        return Math.Max(1, MaxHp(e.Kind, m) - dealt);
    }

    private static int Hits(Enemy e, View v, Model m)
    {
        return (Remaining(e, m) + v.Dmg - 1) / v.Dmg;
    }

    private static float WordTime(int len)
    {
        return (ReactMin + ReactMax) / 2 + len * (KeyMin + KeyMax) / 2;
    }

    private static float Cycle(Model m)
    {
        var alive = Math.Max(1, SpawnCount[m.Level] - m.Kills);
        var formula = alive * 0.4f / m.Difficulty + 0.35f;
        if (m.Intervals.Count < 3) return formula;
        // Observed time between an enemy's turns; lean low to stay safe.
        var sorted = m.Intervals.OrderBy(x => x).ToList();
        return sorted[sorted.Count / 4];
    }

    private static float NextActIn(Enemy e, Model m)
    {
        float started;
        if (!m.ActStart.TryGetValue(e.Id, out started)) return -1f; // unknown
        return Math.Max(0f, Cycle(m) - (Time.time - started));
    }

    // Something in view that would have acted by now unless the turn order is stuck: a creature that can hit us,
    // or one that is chasing us (a pest within its scent range, or a demon outside its reach).
    private static bool IsFrozen(View v, Model m)
    {
        var quiet = Math.Max(1.6f, 1.25f * Cycle(m));
        return m.Acted && m.QuietObserved > quiet && v.Enemies.Any(e => ShouldBeActing(e, v, m));
    }

    private static bool ShouldBeActing(Enemy e, View v, Model m)
    {
        if (e.Kind != Kind.Pest && e.Kind != Kind.Demon) return false;
        var dist = Euclid(e.Pos, v.Me);
        return dist <= Reach(e.Kind, m)
               || (e.Kind == Kind.Pest && dist <= 8 && Manhattan(e.Pos, v.Me) >= 2)
               || (e.Kind == Kind.Demon && dist <= 5 + m.Level + m.Difficulty);
    }

    // Expected number of strikes from an enemy within the next t seconds.
    private static float Strikes(Enemy e, float t, Model m)
    {
        var cycle = Cycle(m);
        var next = NextActIn(e, m);
        if (next < 0) return t / cycle;
        return next < t ? 1 + (float)Math.Floor((t - next) / cycle) : 0;
    }

    private static float Euclid(Vector2Int a, Vector2Int b)
    {
        return Vector2.Distance(a, b);
    }

    private static int Manhattan(Vector2Int a, Vector2Int b)
    {
        return Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);
    }

    private static bool Known(Vector2Int p)
    {
        var map = MapSystem.Instance.Map;
        return p.x >= 0 && p.y >= 0 && p.x < map.Width && p.y < map.Height && map.IsExplored(p.x, p.y);
    }

    private static bool KnownFloor(Vector2Int p)
    {
        return Known(p) && MapSystem.Instance.Map.GetCell(p.x, p.y).IsWalkable;
    }

    // Line of sight over what has been seen (unseen cells are assumed open).
    private static bool Los(Vector2Int a, Vector2Int b)
    {
        int x0 = a.x, y0 = a.y, x1 = b.x, y1 = b.y;
        int dx = Math.Abs(x1 - x0), dy = Math.Abs(y1 - y0), sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx - dy;
        while (true)
        {
            if ((x0 != a.x || y0 != a.y) && (x0 != x1 || y0 != y1))
            {
                var c = new Vector2Int(x0, y0);
                if (Known(c) && !KnownFloor(c)) return false;
            }

            if (x0 == x1 && y0 == y1) return true;
            var e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }
    }

    // Enemies can only strike when they stand inside the player's field of view.
    private static bool CanStrike(Enemy e, Vector2Int tile, View v, Model m)
    {
        if (Euclid(e.Pos, tile) > Reach(e.Kind, m)) return false;
        if (tile == v.Me) return true; // it is drawn, so it is in view
        return Manhattan(e.Pos, tile) <= v.Fov && Los(tile, e.Pos);
    }

    private static List<Enemy> Hostiles(View v, Model m)
    {
        var list = v.Enemies.Where(e => e.Kind == Kind.Pest || e.Kind == Kind.Demon).ToList();
        foreach (var c in v.ChestLooking)
        {
            ChestState s;
            if (m.Chests.TryGetValue(c.Key, out s) && s == ChestState.Fake)
                list.Add(new Enemy { Id = c.Value.gameObject.GetInstanceID(), Pos = c.Key, Kind = Kind.FakeChest, T = c.Value });
        }

        return list;
    }

    private static float Danger(Vector2Int tile, View v, Model m, List<Enemy> hostiles)
    {
        float d = 0;
        foreach (var e in hostiles)
        {
            if (CanStrike(e, tile, v, m)) d += Dmg(e.Kind, m);
            else if (e.Kind == Kind.Pest && Manhattan(e.Pos, tile) == 2) d += 0.3f * Dmg(e.Kind, m);
        }

        return d;
    }

    private static bool Conflicts(string word, View v)
    {
        return v.Prompts.Any(p => p.Word != word && word.StartsWith(p.Word)) || v.Prompts.Count(p => p.Word == word) > 1;
    }

    // ---------------------------------------------------------------- decisions

    private static Act Decide(View v, Model m)
    {
        var hostiles = Hostiles(v, m);
        var threats = hostiles.Where(e => Euclid(e.Pos, v.Me) <= Reach(e.Kind, m)).ToList();
        var targets = new List<KeyValuePair<Prompt, Enemy>>();
        foreach (var p in v.Prompts.Where(p => p.IsAttack))
        {
            var e = hostiles.FirstOrDefault(h => h.T == p.Target);
            if (e != null && !Conflicts(p.Word, v)) targets.Add(new KeyValuePair<Prompt, Enemy>(p, e));
        }

        var cycle = Cycle(m);
        var attackLen = 3 + m.Level;

        if (m.Frozen)
        {
            var frozenLoot = v.Prompts.FirstOrDefault(p => p.IsLoot);
            if (frozenLoot != null && !Conflicts(frozenLoot.Word, v)) return new Act { Kind = ActKind.Loot, Prompt = frozenLoot, Why = "loot while everything is frozen" };
            var victim = targets.OrderBy(x => x.Value.Kind == Kind.Demon ? 0 : 1).ThenBy(x => Hits(x.Value, v, m)).Select(x => (KeyValuePair<Prompt, Enemy>?)x).FirstOrDefault();
            if (victim.HasValue) return new Act { Kind = ActKind.Attack, Prompt = victim.Value.Key, Target = victim.Value.Value, Why = "frozen " + victim.Value.Value.Kind + ", free hit" };
        }

        // Loot when nothing is close enough to interrupt.
        var loot = v.Prompts.FirstOrDefault(p => p.IsLoot);
        var lootSafe = threats.Count == 0 && !hostiles.Any(e => (e.Kind == Kind.Pest && Manhattan(e.Pos, v.Me) <= 3) || (e.Kind == Kind.Demon && Euclid(e.Pos, v.Me) <= Reach(Kind.Demon, m) + 1));
        if (loot != null && lootSafe && !Conflicts(loot.Word, v)) return new Act { Kind = ActKind.Loot, Prompt = loot, Why = "loot" };

        if (threats.Count > 0)
        {
            var attackTime = WordTime(attackLen);
            var stepTime = WordTime(3);
            var minNext = threats.Select(e => NextActIn(e, m)).DefaultIfEmpty(-1).Min();
            var best = targets.OrderBy(t => threats.Contains(t.Value) ? 0 : 1).ThenBy(t => Hits(t.Value, v, m))
                .ThenByDescending(t => Dmg(t.Value.Kind, m)).Select(t => (KeyValuePair<Prompt, Enemy>?)t).FirstOrDefault();

            // Expected damage if we stand and fight: kill reachable threats quickest-first.
            float time = 0, expected = 0;
            foreach (var t in threats.Where(e => targets.Any(x => x.Value == e)).OrderBy(e => Hits(e, v, m)))
            {
                time += Hits(t, v, m) * attackTime;
                expected += Dmg(t.Kind, m) * Strikes(t, time, m);
            }

            foreach (var t in threats.Where(e => targets.All(x => x.Value != e))) expected += Dmg(t.Kind, m) * Strikes(t, Math.Max(time, 1f), m);

            // Expected damage if we run instead.
            Prompt escape;
            int escapeSteps;
            float escapeExpected;
            var safeAt = BestEscape(v, m, hostiles, Math.Max(time, 2f), out escapeSteps, out escape, out escapeExpected);
            if (escape == null) escapeExpected = float.MaxValue;

            var timing = string.Format("next strike {0}, cycle ~{1:F1}s", minNext < 0 ? "unknown" : "~" + minNext.ToString("F1") + "s", cycle);
            if (best.HasValue)
            {
                var target = best.Value.Value;
                var kills = Hits(target, v, m) == 1;
                var knownNext = threats.All(e => NextActIn(e, m) >= 0);
                if (knownNext && attackTime + stepTime + 0.1f < minNext)
                    return new Act { Kind = ActKind.Attack, Prompt = best.Value.Key, Target = target, Why = string.Format("hit {0} before it can act ({1})", target.Kind, timing) };
                if (kills && threats.Count == 1 && NextActIn(target, m) >= attackTime)
                    return new Act { Kind = ActKind.Attack, Prompt = best.Value.Key, Target = target, Why = string.Format("finish {0} ({1})", target.Kind, timing) };
                var worstHit = threats.Max(e => Dmg(e.Kind, m));
                var safeFight = expected + worstHit < v.Hp - 0.5f;
                if (escape == null || safeFight || escapeExpected > expected)
                    return new Act { Kind = ActKind.Attack, Prompt = best.Value.Key, Target = target, Why = string.Format("fight {0}: ~{1:F1} dmg vs ~{2:F1} running {3} steps ({4})", target.Kind, expected, escape == null ? -1 : escapeExpected, escapeSteps, timing) };
            }

            if (escape != null)
                return new Act { Kind = ActKind.Move, Prompt = escape, Why = string.Format("evade toward {0}, {1} steps (~{2:F1} dmg vs ~{3:F1} fighting; {4})", safeAt, escapeSteps, escapeExpected, expected, timing) };
        }

        // Free shots at things that can't hit us yet.
        var shotTime = WordTime(attackLen);
        Func<Enemy, bool> shotIsSafe = e =>
            {
                // A pest that steps up while we type must still leave us time to finish it or step away.
                if (e.Kind == Kind.Pest) return Hits(e, v, m) == 1 || NextActIn(e, m) >= shotTime + 0.1f || cycle >= shotTime + WordTime(3) + 0.1f;
                return Hits(e, v, m) * shotTime <= 2 * cycle;
            };
        var free = targets.Where(t => !threats.Contains(t.Value) && shotIsSafe(t.Value)).OrderBy(t => Hits(t.Value, v, m)).Select(t => (KeyValuePair<Prompt, Enemy>?)t).FirstOrDefault();
        if (free.HasValue)
            return new Act { Kind = ActKind.Attack, Prompt = free.Value.Key, Target = free.Value.Value, Why = "free shot at " + free.Value.Value.Kind };

        // A pest two steps away will step up next: wait for it, so its turn has just ended when it arrives
        // and there is a full turn gap to hit it and step back.
        var stepping = hostiles.FirstOrDefault(e => e.Kind == Kind.Pest && Manhattan(e.Pos, v.Me) == 2);
        if (threats.Count == 0 && stepping != null && v.Hp > Dmg(Kind.Pest, m) && m.Acted)
        {
            if (m.WaitSince < 0) m.WaitSince = Time.time;
            if (Time.time - m.WaitSince < 2 * cycle)
            {
                if (m.LastGoal != "wait") { m.LastGoal = "wait"; Log("Waiting for the pest at " + stepping.Pos + " to step up"); }
                return null;
            }
        }
        else
        {
            m.WaitSince = -1f;
        }

        return Navigate(v, m, hostiles);
    }

    // Search escapes up to 6 steps. Each enemy can only strike until the path leaves its reach or sight,
    // so score every path by the strikes each enemy could land before that, plus what can still hit the end tile.
    private static Vector2Int? BestEscape(View v, Model m, List<Enemy> hostiles, float horizon, out int steps, out Prompt firstStep, out float expected)
    {
        steps = 0;
        firstStep = null;
        expected = float.MaxValue;
        var stepTime = WordTime(3);
        var occupied = new HashSet<Vector2Int>(v.Enemies.Where(e => e.Kind != Kind.Dead).Select(e => e.Pos));
        var parent = new Dictionary<Vector2Int, Vector2Int>();
        var depth = new Dictionary<Vector2Int, int> { { v.Me, 0 } };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(v.Me);
        Vector2Int? best = null;
        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            if (depth[cur] >= 6) continue;
            for (var i = 0; i < 4; i++)
            {
                var dir = (Direction)i;
                var n = cur + dir.ToVector2Int();
                if (depth.ContainsKey(n) || !KnownFloor(n) || occupied.Contains(n) || FakeChestNear(n, m)) continue;
                if (cur == v.Me && !v.Prompts.Any(p => p.IsMove && p.Dir == dir && !Conflicts(p.Word, v))) continue;
                depth[n] = depth[cur] + 1;
                parent[n] = cur;
                queue.Enqueue(n);

                var path = new List<Vector2Int>();
                for (var q = n; q != v.Me; q = parent[q]) path.Insert(0, q);
                float cost = 0;
                foreach (var e in hostiles)
                {
                    if (!CanStrike(e, v.Me, v, m) && !path.Any(q => CanStrike(e, q, v, m))) continue;
                    var outAt = -1;
                    for (var k = path.Count - 1; k >= 0 && !CanStrike(e, path[k], v, m); k--) outAt = k;
                    var exposed = outAt < 0 ? horizon : outAt * stepTime + stepTime;
                    cost += Dmg(e.Kind, m) * Strikes(e, exposed, m);
                }

                var exits = Enumerable.Range(0, 4).Count(k => { var q = n + ((Direction)k).ToVector2Int(); return q != cur && KnownFloor(q) && !occupied.Contains(q); });
                var pestGap = hostiles.Where(e => e.Kind == Kind.Pest).Select(e => Manhattan(e.Pos, n)).DefaultIfEmpty(9).Min();
                var nearDemon = hostiles.Any(e => e.Kind == Kind.Demon && Euclid(e.Pos, n) <= Reach(Kind.Demon, m));
                var pests = hostiles.Count(e => e.Kind == Kind.Pest);
                var floorAround = Enumerable.Range(0, 4).Count(k => KnownFloor(n + ((Direction)k).ToVector2Int()));
                var score = cost + (UnsafeChestNear(n, m) ? 1f : 0f) + 0.05f * depth[n] + (exits == 0 ? 2f : 0f)
                            - 0.2f * Math.Min(pestGap, 6) - (nearDemon ? 0.5f : 0f) - (pests >= 2 && floorAround == 2 ? 0.5f : 0f);
                if (score < expected) { expected = score; best = n; }
            }
        }

        if (!best.HasValue) return null;
        steps = depth[best.Value];
        var first = best.Value;
        while (parent[first] != v.Me) first = parent[first];
        var d0 = first - v.Me;
        firstStep = v.Prompts.First(p => p.IsMove && p.Dir.ToVector2Int() == d0 && !Conflicts(p.Word, v));
        return best;
    }

    private static bool FakeChestNear(Vector2Int t, Model m)
    {
        return m.Chests.Any(kv => kv.Value == ChestState.Fake && Manhattan(kv.Key, t) <= 1);
    }

    private static bool UnsafeChestNear(Vector2Int t, Model m)
    {
        foreach (var kv in m.Chests)
        {
            if ((kv.Value == ChestState.Unverified || kv.Value == ChestState.Fake) && Manhattan(kv.Key, t) <= 1) return true;
        }

        return false;
    }

    private static Act Navigate(View v, Model m, List<Enemy> hostiles)
    {
        var map = MapSystem.Instance.Map;
        int w = map.Width, h = map.Height;
        var occupied = new HashSet<Vector2Int>(v.Enemies.Where(e => e.Kind != Kind.Dead).Select(e => e.Pos));
        var remembered = m.DemonMemory.Where(kv => !v.Enemies.Any(e => e.Id == kv.Key))
            .Select(kv => new Enemy { Id = kv.Key, Pos = kv.Value.Key, Kind = Kind.Demon }).ToList();

        Func<Vector2Int, bool> passable = p => KnownFloor(p) && !occupied.Contains(p) && (p == v.Me || !FakeChestNear(p, m));
        Func<Vector2Int, float> stepCost = p =>
            {
                float c = 1;
                foreach (var e in hostiles) if (CanStrike(e, p, v, m)) c += (Dmg(e.Kind, m) >= v.Hp ? 10 : 3) * Dmg(e.Kind, m);
                foreach (var e in hostiles.Where(h => h.Kind == Kind.Pest))
                {
                    var gap = Manhattan(e.Pos, p);
                    if (gap <= 2 && Hits(e, v, m) > 1) c += (v.Dmg <= 1 ? 6 : 2) * (3 - gap);
                    if (v.Hp <= Dmg(Kind.Pest, m) && gap <= 4) c += 4 * (5 - gap);
                }

                if (UnsafeChestNear(p, m)) c += 15;
                foreach (var e in remembered) if (Euclid(e.Pos, p) <= Reach(Kind.Demon, m) + 1 && Manhattan(e.Pos, p) <= v.Fov + 2 && Los(p, e.Pos)) c += (Dmg(Kind.Demon, m) >= v.Hp ? 6 : 2.5f) * Dmg(Kind.Demon, m);
                return c;
            };

        // Dijkstra from the player over known floor.
        var dist = new Dictionary<Vector2Int, float> { { v.Me, 0 } };
        var prev = new Dictionary<Vector2Int, Vector2Int>();
        var open = new List<Vector2Int> { v.Me };
        var done = new HashSet<Vector2Int>();
        while (open.Count > 0)
        {
            var cur = open.OrderBy(p => dist[p]).First();
            open.Remove(cur);
            if (!done.Add(cur)) continue;
            for (var i = 0; i < 4; i++)
            {
                var n = cur + ((Direction)i).ToVector2Int();
                if (n.x < 0 || n.y < 0 || n.x >= w || n.y >= h || done.Contains(n) || !passable(n)) continue;
                var nd = dist[cur] + stepCost(n);
                float old;
                if (!dist.TryGetValue(n, out old) || nd < old) { dist[n] = nd; prev[n] = cur; if (!open.Contains(n)) open.Add(n); }
            }
        }

        Func<IEnumerable<Vector2Int>, Vector2Int?> nearest = cands =>
            {
                Vector2Int? best = null;
                var bd = float.MaxValue;
                foreach (var c in cands)
                {
                    float d;
                    if (c != v.Me && dist.TryGetValue(c, out d) && d < bd) { bd = d; best = c; }
                }

                return best;
            };
        Func<Vector2Int?, float> costTo = g => g.HasValue && dist.ContainsKey(g.Value) ? dist[g.Value] : float.MaxValue;

        var realChests = m.Chests.Where(kv => kv.Value == ChestState.Real).Select(kv => kv.Key).ToList();
        var chestGoal = nearest(realChests);
        var verifyGoal = nearest(dist.Keys.Where(t => m.Chests.Any(kv => kv.Value == ChestState.Unverified && Manhattan(kv.Key, t) > v.Fov && Manhattan(kv.Key, t) <= v.Fov + 3)));
        var frontierAll = dist.Keys.Where(t => Enumerable.Range(0, 4).Any(i => !Known(t + ((Direction)i).ToVector2Int()))).ToList();
        var frontier = nearest(frontierAll.Where(t => remembered.All(e => Euclid(e.Pos, t) > Reach(Kind.Demon, m) + 2))) ?? nearest(frontierAll);
        var door = m.Door.HasValue && dist.ContainsKey(m.Door.Value) ? m.Door : null;

        Vector2Int? goal = null;
        string why = null;
        if (m.Level <= 3)
        {
            if (chestGoal.HasValue) { goal = chestGoal; why = "chest"; }
            else if (verifyGoal.HasValue) { goal = verifyGoal; why = "check a chest"; }
            else if (frontier.HasValue) { goal = frontier; why = "explore"; }
            else if (door.HasValue) { goal = door; why = "exit"; }
        }
        else if (m.Level == 4)
        {
            if (chestGoal.HasValue && costTo(chestGoal) <= 25) { goal = chestGoal; why = "chest"; }
            else if (door.HasValue) { goal = door; why = "exit"; }
            else if (verifyGoal.HasValue && costTo(verifyGoal) <= 12) { goal = verifyGoal; why = "check a chest"; }
            else if (frontier.HasValue) { goal = frontier; why = "explore"; }
            else if (chestGoal.HasValue) { goal = chestGoal; why = "chest"; }
        }
        else
        {
            if (door.HasValue) { goal = door; why = "exit"; }
            else if (chestGoal.HasValue && costTo(chestGoal) <= 8) { goal = chestGoal; why = "chest"; }
            else if (frontier.HasValue) { goal = frontier; why = "explore"; }
        }

        // Stick with the current goal while it is still worth reaching and of the same kind.
        if (goal.HasValue && m.Goal.HasValue && m.GoalWhy == why && m.Goal.Value != v.Me && dist.ContainsKey(m.Goal.Value))
        {
            var g = m.Goal.Value;
            var stillGood = why == "chest" ? m.Chests.ContainsKey(g) && m.Chests[g] == ChestState.Real
                : why == "explore" ? frontierAll.Contains(g)
                : why == "check a chest" ? m.Chests.Values.Contains(ChestState.Unverified)
                : true;
            if (stillGood) goal = g;
        }

        m.Goal = goal;
        m.GoalWhy = why;
        if (!goal.HasValue) return null;

        var step = goal.Value;
        while (prev.ContainsKey(step) && prev[step] != v.Me) step = prev[step];
        var dir = step - v.Me;
        var prompt = v.Prompts.FirstOrDefault(p => p.IsMove && p.Dir.ToVector2Int() == dir);
        if (prompt == null || Conflicts(prompt.Word, v)) return null;

        var label = why + " " + goal.Value;
        if (label != m.LastGoal) { m.LastGoal = label; Log("Heading to " + label); }
        return new Act { Kind = ActKind.Move, Prompt = prompt, Why = why };
    }

    // ---------------------------------------------------------------- typing (human speed)

    private static IEnumerator Execute(Act act, Model m, View v)
    {
        if (act.Kind != ActKind.Move || act.Why.StartsWith("evade")) Log((act.Kind == ActKind.Loot ? "Loot" : act.Kind == ActKind.Attack ? "Attack" : "Move") + ": " + act.Why + " - typing '" + act.Prompt.Word + "'");

        yield return new WaitForSeconds(Uniform(ReactMin, ReactMax));
        var word = act.Prompt.Word;
        var ic = Object.FindAnyObjectByType<InputController>();
        if (ic == null) yield break;
        var append = typeof(InputController).GetMethod("AppendLetter", Priv);
        var buffer = (StringBuilder)typeof(InputController).GetField("_currentInputWord", Priv).GetValue(ic);
        var textField = typeof(SpellHint).GetField("_text", Priv);

        for (var i = 0; i < word.Length; i++)
        {
            if (ic == null || GameController.Instance == null || !GameController.Instance.IsPlaying) yield break;
            if (act.Kind == ActKind.Loot && !act.Why.Contains("frozen") && m.Latest != null && Hostiles(m.Latest, m).Any(e => Euclid(e.Pos, m.Latest.Me) <= Reach(e.Kind, m)))
            {
                Log("Loot interrupted: something can hit me");
                if (buffer.Length > 0) append.Invoke(ic, new object[] { '1' });
                yield break;
            }

            var hint = act.Prompt.Hint;
            var stillThere = hint != null && hint.gameObject.activeInHierarchy && HintCache.Instance.Contains(hint)
                             && Regex.Replace(((TMPro.TMP_Text)textField.GetValue(hint)).text, "<[^>]*>", string.Empty) == word;
            if (!stillThere || buffer.ToString() != word.Substring(0, i))
            {
                if (act.Kind != ActKind.Move || act.Why.StartsWith("evade"))
                    Log(string.Format("  abandoned '{0}' after {1} letters ({2})", word, i, !stillThere ? "prompt changed" : "input was '" + buffer + "'"));
                if (buffer.Length > 0) append.Invoke(ic, new object[] { '1' }); // a wrong key clears the input
                yield break;
            }

            append.Invoke(ic, new object[] { word[i] });
            yield return new WaitForSeconds(Uniform(KeyMin, KeyMax));
        }

        m.Acted = true;
        if (act.Kind == ActKind.Attack && act.Target != null)
        {
            int dealt;
            m.Dealt.TryGetValue(act.Target.Id, out dealt);
            m.Dealt[act.Target.Id] = dealt + v.Dmg;
        }
        else if (act.Kind == ActKind.Loot)
        {
            m.Chests[v.Me] = ChestState.Gone;
        }
    }

    private static float Uniform(float a, float b)
    {
        return a + (float)_rng.NextDouble() * (b - a);
    }
}
