using PulseOreDetector.Proto;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;

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

        public static void RefreshControls(IMyTerminalBlock block)
        {

            if (MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.ControlPanel)
            {
                var myCubeBlock = block as MyCubeBlock;

                if (myCubeBlock.IDModule != null)
                {

                    var share = myCubeBlock.IDModule.ShareMode;
                    var owner = myCubeBlock.IDModule.Owner;
                    myCubeBlock.ChangeOwner(owner, share == MyOwnershipShareModeEnum.None ? MyOwnershipShareModeEnum.All : MyOwnershipShareModeEnum.None);
                    myCubeBlock.ChangeOwner(owner, share);
                }
            }
        }

        public static byte[] MessageToBytes(MyMessageObject data)
        {
            try
            {
                string itemMessage = MyAPIGateway.Utilities.SerializeToXML(data);
                byte[] itemData = Encoding.UTF8.GetBytes(itemMessage);
                return itemData;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine(e.ToString());
                return null;
            }
        }

        public static MyMessageObject BytesToMessage(byte[] bytes)
        {
            try
            {
                string itemMessage = Encoding.UTF8.GetString(bytes);
                MyMessageObject itemData = MyAPIGateway.Utilities.SerializeFromXML<MyMessageObject>(itemMessage);
                return itemData;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine(e.ToString());
                return null;
            }
        }

        public static IMyPlayer GetPlayer(ulong steamid)
        {
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players, i => i.SteamUserId == steamid);
            return players.FirstOrDefault();
        }

        public static void SendMessageToClient(MyMessageObject data, ulong steamid)
        {
            MyAPIGateway.Utilities.InvokeOnGameThread(() =>
            {
                MyAPIGateway.Multiplayer.SendMessageTo(PulseOreDetectorMod.CLIENT_ID, MessageToBytes(data), steamid);
            });
        }
    }
}
