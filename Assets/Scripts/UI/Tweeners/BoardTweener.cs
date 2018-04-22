// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="BoardTweener.cs" author="Lars" company="None">
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

namespace UI.Tweeners
{
    using System.Collections;

    using DG.Tweening;

    using ECS.Components;

    using UnityEngine;

    using Utilities.Camera;
    using Utilities.Game.ECSCache;

    public class BoardTweener : MonoBehaviour
    {
        public IEnumerator BoardAppear()
        {
            transform.DOMove(new Vector3(0, -10f, 0), 1.0f).From().SetEase(Ease.OutCubic).OnStart(() => gameObject.SetActive(true));
            yield return new WaitForSeconds(1.0f);
        }

        public IEnumerator BoardDisappear()
        {
            var playerPos = ActorCache.Instance.Player.Entity.GetComponent<GridPositionComponent>();
            Camera.main.transform.DOMove(new Vector3(playerPos.Position.x, playerPos.Position.y, -10), 1.0f).SetEase(Ease.OutBack);
            yield return new WaitForSeconds(1.0f);
            Camera.main.DOOrthoSize(0f, 2.0f).SetEase(Ease.InBounce);
            ////transform.DOMove(new Vector3(0, -100, 0), 1.0f);
            yield return new WaitForSeconds(2.0f);
        }
    }
}