// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="Fireball.cs" author="Lars" company="None">
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

using DG.Tweening;

using UnityEngine;

using Utilities.Game.ObjectPool;

public class Fireball : MonoBehaviour, IPoolable
{
    public bool IsEnabled
    {
        get
        {
            return gameObject.activeInHierarchy;
        }
    }

    public void Disable()
    {
        gameObject.SetActive(false);
    }

    public void Enable()
    {
        gameObject.SetActive(true);
    }

    public void Animate(Vector2Int from, Vector2Int to, float duration, Action onCompleted)
    {
        gameObject.transform.position = (Vector2)from;
        gameObject.transform.DOMove((Vector2)to, duration).SetEase(Ease.InBack).OnComplete(() =>
            {
                onCompleted();
                ExplodeThenDisable();
            });
    }

    public void ExplodeThenDisable()
    {
        ObjectPools.Instance.GetPooledObject<Explosion>().Explode(transform.position);
        Disable();
    }
}