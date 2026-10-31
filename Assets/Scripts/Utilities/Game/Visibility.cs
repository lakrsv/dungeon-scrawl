// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="Visibility.cs" author="Lars-Kristian Svenoy" company="Cardlike">
// //  Copyright @ 2018 Cardlike. All rights reserved.
// // </copyright>
// // <summary>
// //   The colours of light and darkness.
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

namespace Utilities.Game
{
    using UnityEngine;

    /// <summary>
    /// The colours the dungeon is tinted with. Sprites are multiplied by these, so they double as light.
    /// </summary>
    public static class Visibility
    {
        // Out of view but remembered: a dim, cold blue.
        public static readonly Color ExploredInvisible = new Color(0.24f, 0.26f, 0.40f, 1);

        public static readonly Color Invisible = Color.clear;

        public static readonly Color Visible = Color.white;

        // The player's light: warm where they stand, dimmer and cooler at the edge of their sight.
        private static readonly Color LightNear = new Color(1.00f, 0.96f, 0.86f, 1);

        private static readonly Color LightFar = new Color(0.52f, 0.48f, 0.62f, 1);

        /// <summary>
        /// The light at a fraction t of the way from the player to the edge of their sight.
        /// </summary>
        public static Color Lit(float t)
        {
            return Color.Lerp(LightNear, LightFar, t * t);
        }
    }
}