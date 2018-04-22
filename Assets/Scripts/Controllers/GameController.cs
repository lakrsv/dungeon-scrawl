// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="GameController.cs" author="Lars" company="None">
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

namespace Controllers
{
    using System.Collections;

    using ECS.Components;
    using ECS.Entities.Blueprint;
    using ECS.Systems;

    using UI.Tweeners;

    using UnityEngine;
    using UnityEngine.SceneManagement;

    using Utilities.Camera;
    using Utilities.Game;
    using Utilities.Game.ECSCache;

    public class GameController : MonoSingleton<GameController>
    {
        public static int CurrentLevel = 1;

        public bool IsPlaying { get; set; }

        public bool PlayerHasMoved { get; set; }

        [SerializeField]
        private BoardTweener _board;

        [SerializeField]
        private CameraMovement _cameraMovement;

        [SerializeField]
        private RenderSystem _renderSystem;

        public void EndLevel()
        {
            PlayerHasMoved = false;
            IsPlaying = false;

            if (CurrentLevel == 5)
            {
                // Game Is Done!
                StartCoroutine(GameDone());
            }
            else
            {
                CurrentLevel++;
                Player.SavePlayerStats();
                StartCoroutine(GoToNextLevel());
            }
        }

        public void GameOver()
        {
            CurrentLevel = 1;
            IsPlaying = false;
            _renderSystem.Execute();
            Player.ClearPlayerStats();

            StartCoroutine(GameOverRoutine());
        }

        private IEnumerator GameOverRoutine()
        {
            yield return _board.BoardDisappearOpposite();
            SceneManager.LoadScene("GameOver");
        }

        private IEnumerator GameDone()
        {
            yield return null;
        }

        private IEnumerator GoToNextLevel()
        {
            yield return _board.BoardDisappear();
            SceneManager.LoadScene("Game");
        }

        private void Awake()
        {
            PlayerHasMoved = false;
            IsPlaying = false;
        }

        private IEnumerator Start()
        {
            yield return null;

            _board.gameObject.SetActive(false);

            _renderSystem.Execute();
            ActorCache.Instance.Player.Entity.GetComponent<RenderComponent>().Renderer.enabled = true;

            _cameraMovement.SetPosition(
                ActorCache.Instance.Player.Entity.GetComponent<GridPositionComponent>().Position);

            yield return _board.BoardAppear();
            IsPlaying = true;
        }

    }
}