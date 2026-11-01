// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="AudioPlayer.cs" author="Lars" company="None">
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
// //   TODO - Insert file description
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using Sound;

using UnityEngine;

using Utilities.Game;

public class AudioPlayer : MonoSingleton<AudioPlayer>
{
    // The pitch of each letter of a word, in semitones above the key sound (an A): up the D minor pentatonic scale
    // of the dungeon theme, so typing plays along with the music.
    private static readonly int[] LetterSteps = { 0, 3, 5, 8, 10, 12, 15, 17, 20, 22, 24 };

    private readonly Dictionary<Type, List<AudioClip>> _audioClips = new Dictionary<Type, List<AudioClip>>();

    [SerializeField]
    private AudioSource _audioSource;

    // Letters play on their own source, so changing its pitch never bends the other sounds.
    [SerializeField]
    private AudioSource _keySource;

    [SerializeField]
    private List<Clip> _clips;

    public enum Type
    {
        Powerup,

        Shoot,

        Explosion,

        Damage,

        ChestOpen,

        ItemApply,

        Teleport,

        LevelText,

        GameOver,

        Key,

        Word,

        Typo,

        Step,

        Death,

        BossRumble,

        BossCharge,

        BossSlam,

        BossBeam,

        BossHit,

        BossSink,

        BossDeath
    }

    /// <summary>
    /// Plays a sound of the type. When there are several clips of it, a random one plays.
    /// </summary>
    public void PlayAudio(Type type)
    {
        var clip = Pick(type);
        if (clip != null) _audioSource.PlayOneShot(clip);
    }

    /// <summary>
    /// Plays the blip of a typed letter; letter 0 is the first of the word. Each letter sounds a step higher.
    /// </summary>
    public void PlayLetter(int letter, float volume = 1f)
    {
        var step = LetterSteps[Mathf.Clamp(letter, 0, LetterSteps.Length - 1)];
        _keySource.pitch = Mathf.Pow(2f, step / 12f);
        _keySource.PlayOneShot(Pick(Type.Key), volume);
    }

    private AudioClip Pick(Type type)
    {
        List<AudioClip> clips;
        return _audioClips.TryGetValue(type, out clips) ? clips[UnityEngine.Random.Range(0, clips.Count)] : null;
    }

    private void Awake()
    {
        foreach (var clip in _clips)
        {
            List<AudioClip> clips;
            if (!_audioClips.TryGetValue(clip.Type, out clips))
            {
                clips = new List<AudioClip>();
                _audioClips.Add(clip.Type, clips);
            }

            clips.Add(clip.AClip);
        }

        _audioSource.volume *= Volume.Sound;
        _keySource.volume *= Volume.Sound;
    }

    [Serializable]
    public class Clip
    {
        public AudioClip AClip;

        public Type Type;
    }
}
