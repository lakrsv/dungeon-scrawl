// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="RenderComponent.cs" author="Lars" company="None">
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

namespace ECS.Components
{
    using ECS.Entities;

    using UnityEngine;

    using Utilities.Game;

    public class RenderComponent : ComponentBase
    {
        public RenderComponent(Entity owner)
            : base(owner)
        {
        }

        public SpriteRenderer Renderer { get; private set; }

        public SpriteRenderer Shadow { get; private set; }

        public SpriteFlash Flash { get; private set; }

        // The logical sprite. What is drawn is the current frame of its idle animation.
        public Sprite Sprite { get; set; }

        // Offsets the idle animation so creatures don't all bob in step.
        public float AnimationOffset { get; private set; }

        public bool ShowShadow { get; set; }

        public override void OnAdd()
        {
            Renderer = Owner.GameObject.GetComponentInChildren<SpriteRenderer>();
            Renderer.sprite = Sprite;
            Renderer.sortingOrder = SortOrder.ActorDefault;

            // Actors are pooled: clear whatever the last creature left behind.
            Renderer.flipX = false;
            Renderer.color = Color.white;

            var shadow = Owner.GameObject.transform.Find("Shadow");
            Shadow = shadow != null ? shadow.GetComponent<SpriteRenderer>() : null;
            ShowShadow = true;

            Flash = Renderer.GetComponent<SpriteFlash>();
            AnimationOffset = Random.value * 10f;
            base.OnAdd();
        }

        /// <summary>
        /// Turns the sprite to face along a horizontal direction. Sprites are drawn facing left.
        /// </summary>
        public void Face(float directionX)
        {
            if (directionX > 0.01f) Renderer.flipX = true;
            else if (directionX < -0.01f) Renderer.flipX = false;
        }
    }
}