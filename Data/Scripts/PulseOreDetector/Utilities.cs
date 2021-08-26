using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game;
using VRage.Game.ModAPI;

namespace PulseOreDetector
{
    class Utilities
    {
        private static IMyHudNotification LocalNotification;
        public static void ShowNotificationLocal(string message, int delay = 3000, string font = MyFontEnum.White)
        {
            if (LocalNotification == null)
            {
                LocalNotification = MyAPIGateway.Utilities.CreateNotification(message, delay, font);
            }
            LocalNotification.Hide();
            LocalNotification.Font = font;
            LocalNotification.Text = message;
            LocalNotification.AliveTime = delay;
            LocalNotification.Show();
        }
    }
}
