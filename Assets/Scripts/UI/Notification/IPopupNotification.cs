// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="IPopupNotification.cs" author="Lars-Kristian Svenoy" company="Cardlike">
// //  Copyright @ 2018 Cardlike. All rights reserved.
// // </copyright>
// // <summary>
// //   TODO - Insert file description
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

namespace UI.Notification
{
    using UnityEngine;

    public interface IPopupNotification
    {
        void Enable(string message, Vector2 position, float duration);
    }
}