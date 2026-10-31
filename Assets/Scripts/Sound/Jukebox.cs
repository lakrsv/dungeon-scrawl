// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="Jukebox.cs" author="Lars" company="None">
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
// //   Plays the music, the ambience and the menu sounds across every scene.
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

namespace Sound
{
    using DG.Tweening;

    using UnityEngine;

    /// <summary>
    /// Plays the music, the ambience and the menu sounds. Every scene has a jukebox that says what plays there.
    /// The first one to wake stays for the rest of the game, so the music carries on through scene loads without a
    /// break; the ones that wake later hand it their tracks and remove themselves.
    /// </summary>
    public class Jukebox : MonoBehaviour
    {
        private const float FadeDuration = 1.5f;

        private static Jukebox _instance;

        [SerializeField]
        private AudioClip _music;

        [SerializeField]
        private AudioClip _ambience;

        [SerializeField]
        private float _musicVolume = 0.35f;

        [SerializeField]
        private float _ambienceVolume = 0.4f;

        [SerializeField]
        private AudioClip _hover;

        [SerializeField]
        private AudioClip _select;

        [SerializeField]
        private float _menuVolume = 0.4f;

        private Track _musicTrack;

        private Track _ambienceTrack;

        private AudioSource _menuSource;

        /// <summary>
        /// Fades over to another piece of music, or out to silence when given null.
        /// </summary>
        public static void PlayMusic(AudioClip clip)
        {
            if (_instance != null) _instance._musicTrack.Play(clip);
        }

        /// <summary>
        /// The pointer came onto a menu button.
        /// </summary>
        public static void Hover()
        {
            if (_instance != null) _instance.PlayMenuSound(_instance._hover);
        }

        /// <summary>
        /// A menu choice was made. It plays on through a scene load.
        /// </summary>
        public static void Select()
        {
            if (_instance != null) _instance.PlayMenuSound(_instance._select);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                _instance._musicTrack.Play(_music);
                _instance._ambienceTrack.Play(_ambience);
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            _musicTrack = new Track(gameObject, _musicVolume);
            _ambienceTrack = new Track(gameObject, _ambienceVolume);
            _menuSource = gameObject.AddComponent<AudioSource>();
            _menuSource.playOnAwake = false;

            _musicTrack.Play(_music);
            _ambienceTrack.Play(_ambience);
        }

        private void Update()
        {
            _musicTrack.ApplyVolume(Volume.Music);
            _ambienceTrack.ApplyVolume(Volume.Music);
        }

        private void PlayMenuSound(AudioClip clip)
        {
            if (clip != null) _menuSource.PlayOneShot(clip, _menuVolume * Volume.Sound);
        }

        /// <summary>
        /// One looping layer. It has two sources, so a new loop can fade in while the old one fades out.
        /// </summary>
        private class Track
        {
            private readonly AudioSource[] _sources = new AudioSource[2];

            private readonly float[] _levels = new float[2];

            private readonly float _volume;

            private AudioClip _clip;

            private int _current;

            public Track(GameObject owner, float volume)
            {
                _volume = volume;
                for (var i = 0; i < _sources.Length; i++)
                {
                    var source = owner.AddComponent<AudioSource>();
                    source.loop = true;
                    source.playOnAwake = false;
                    source.volume = 0f;
                    _sources[i] = source;
                }
            }

            public void Play(AudioClip clip)
            {
                if (clip == _clip) return;
                _clip = clip;

                Fade(_current, 0f);
                if (clip == null) return;

                // If the other source is still fading out this same loop, bring it back from where it is.
                _current = 1 - _current;
                var source = _sources[_current];
                if (source.clip != clip || !source.isPlaying)
                {
                    source.clip = clip;
                    source.Play();
                }

                Fade(_current, 1f);
            }

            public void ApplyVolume(float gain)
            {
                for (var i = 0; i < _sources.Length; i++) _sources[i].volume = _levels[i] * _volume * gain;
            }

            private void Fade(int index, float level)
            {
                var source = _sources[index];
                DOTween.Kill(source);
                var tween = DOTween.To(() => _levels[index], x => _levels[index] = x, level, FadeDuration)
                    .SetEase(Ease.Linear)
                    .SetTarget(source);
                if (level <= 0f) tween.OnComplete(source.Stop);
            }
        }
    }
}
