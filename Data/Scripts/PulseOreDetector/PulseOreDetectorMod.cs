using PulseOreDetector.Proto;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace PulseOreDetector
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    class PulseOreDetectorMod : MySessionComponentBase
    {
        public const ushort CLIENT_ID = 1799;
        public const ushort SERVER_ID = 1800;


        public static PulseOreDetectorMod Instance;

        public IMyTerminalControlButton ScanButton;
        public IMyTerminalAction ScanAction;

        public bool ControlsCreated = false;

        public static MyVoxelMaterialDefinition[] VoxelMaterials;

        public override void LoadData()
        {
            Instance = this;
            VoxelMaterials = MyDefinitionManager.Static.GetVoxelMaterialDefinitions().Where(v => v.IsRare).ToArray();
            MyAPIGateway.TerminalControls.CustomActionGetter += AddAction;
            MyAPIGateway.TerminalControls.CustomControlGetter += AddControl;

            AddMessageHandler();

            if (MyAPIGateway.Multiplayer.IsServer)
            {
                MyLog.Default.WriteLine("PulseOreDetector INIT Server");
                Config.Load();
            } 
            else
            {
                MyMessageObject message = new MyMessageObject()
                {
                    SteamId = MyAPIGateway.Session.Player.SteamUserId,
                    Type = MyMessageType.RequesetConfig
                };
                byte[] data = Utilities.MessageToBytes(message);
                MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                {
                    MyAPIGateway.Multiplayer.SendMessageToServer(SERVER_ID, data);
                });
            }
        }

        protected override void UnloadData()
        {
            Instance = null;
            MyAPIGateway.TerminalControls.CustomActionGetter -= AddAction;
            MyAPIGateway.TerminalControls.CustomControlGetter -= AddControl;
            RemoveMessageHandler();
        }

        private void AddAction(IMyTerminalBlock block, List<IMyTerminalAction> actions)
        {
            if(block.BlockDefinition.SubtypeId.Equals("LargePulseDetector"))
            {
                actions.Add(ScanAction);
            }
        }

        private void AddControl(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            if (block.BlockDefinition.SubtypeId.Equals("LargePulseDetector"))
            {
                controls.Add(ScanButton);
            }
        }

        public void AddMessageHandler()
        {
            //register all our events and stuff
            MyAPIGateway.Multiplayer.RegisterMessageHandler(CLIENT_ID, HandleServerData);
            MyAPIGateway.Multiplayer.RegisterMessageHandler(SERVER_ID, HandlePlayerData);
        }

        public void RemoveMessageHandler()
        {
            //unregister them when the game is closed
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(CLIENT_ID, HandleServerData);
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(SERVER_ID, HandlePlayerData);
        }

        // CLIENT hat Daten vom Server Erhalten. Entweder Chatnachricht oder Dialog
        public void HandleServerData(byte[] data)
        {
            MyLog.Default.WriteLine(string.Format("Received Server Data: {0} bytes", data.Length));
            if (MyAPIGateway.Multiplayer.IsServer && MyAPIGateway.Session.LocalHumanPlayer == null)
                return;

            MyMessageObject request = Utilities.BytesToMessage(data);

            if (request == null)
                return;

            switch(request.Type)
            {
                case MyMessageType.SupplyConfig:
                    Config.Instance = request.Config;
                    break;
            }
        }

        // SERVER hat Daten vom Spieler erhalten - Vermutlich ein Befehl
        public void HandlePlayerData(byte[] data)
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
                return;

            MyLog.Default.WriteLine(string.Format("Received Player Data: {0} bytes", data.Length));
            MyMessageObject request = Utilities.BytesToMessage(data);
            if (request == null)
                return;

            IMyPlayer player = Utilities.GetPlayer(request.SteamId);
            if (player == null)
                return;

            switch (request.Type)
            {
                case MyMessageType.RequesetConfig:
                    Utilities.SendMessageToClient(new MyMessageObject()
                    {
                        Type = MyMessageType.SupplyConfig,
                        Config = Config.Instance
                    }, request.SteamId);
                    break;
            }
        }

    }
}
