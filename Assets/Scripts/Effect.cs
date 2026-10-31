// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="Effect.cs" author="Lars" company="None">
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
// //   A pooled one-shot sprite effect.
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

using UnityEngine;

using Utilities.Game.ObjectPool;

/// <summary>
/// Plays a set of frames once at a position, then returns to its pool. Used for hit sparks and death puffs.
/// </summary>
public class Effect : MonoBehaviour, IPoolable
{
    [SerializeField]
    private SpriteRenderer _renderer;

    [SerializeField]
    private float _framesPerSecond = 14f;

    private Sprite[] _frames;

    private float _startTime;

    public bool IsEnabled
    {
        get
        {
            return gameObject.activeInHierarchy;
        }
    }

    public void Enable()
    {
        gameObject.SetActive(true);
    }

    public void Disable()
    {
        _frames = null;
        gameObject.SetActive(false);
    }

    public void Play(Sprite[] frames, Vector2 position)
    {
        _frames = frames;
        _startTime = Time.time;
        transform.position = position;
        _renderer.sprite = frames[0];
    }

    private void Update()
    {
        if (_frames == null) return;

        var index = Mathf.FloorToInt((Time.time - _startTime) * _framesPerSecond);
        if (index >= _frames.Length)
        {
            Disable();
            return;
        }

        _renderer.sprite = _frames[index];
    }
}
