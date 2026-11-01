// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="BossSystem.cs" author="Lars" company="None">
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
// //   Starts the fight with the Scrawl on the last level.
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

namespace ECS.Systems
{
    using Boss;

    using Components;

    using Controllers;

    using Sound;

    using UnityEngine;

    using Utilities.Camera;
    using Utilities.Game.ECSCache;

    /// <summary>
    /// On the last level, keeps the walk across the bridge quiet (no music, just the wind), and wakes the Scrawl when
    /// the knight reaches MapSystem.BossRow. The fight itself is Boss.Scrawl.
    /// </summary>
    public class BossSystem : MonoBehaviour, IInitializeSystem, IExecuteSystem
    {
        [SerializeField]
        private Scrawl _scrawl;

        [SerializeField]
        private InputController _input;

        [SerializeField]
        private AttackSystem _attack;

        [SerializeField]
        private ItemSystem _items;

        [SerializeField]
        private CameraMovement _camera;

        [SerializeField]
        private BossBar _bar;

        private bool _waiting;

        public void Initialize()
        {
            _waiting = MapSystem.IsBossLevel;
            if (_waiting) Jukebox.PlayMusic(null);
        }

        public void Execute()
        {
            if (!_waiting) return;
            var player = ActorCache.Instance.Player;
            if (player == null || player.Entity.GetComponent<GridPositionComponent>().Position.y < MapSystem.BossRow) return;
            _waiting = false;
            _scrawl.Begin(_input, _attack, _items, _camera, _bar);
        }
    }
}
