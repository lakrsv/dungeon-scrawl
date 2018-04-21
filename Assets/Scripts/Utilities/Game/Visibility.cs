// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="Visibility.cs" author="Lars-Kristian Svenoy" company="Cardlike">
// //  Copyright @ 2018 Cardlike. All rights reserved.
// // </copyright>
// // <summary>
// //   TODO - Insert file description
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

namespace Utilities.Game
{
    using UnityEngine;

    public static class Visibility
    {
        public static readonly Color ExploredInvisible = new Color(0.50f, 0.50f, 0.50f, 1);

        public static readonly Color Invisible = Color.clear;

        public static readonly Color Visible = Color.white;
    }
}