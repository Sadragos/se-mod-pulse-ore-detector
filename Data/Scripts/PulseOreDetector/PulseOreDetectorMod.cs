using Sandbox.Definitions;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Components;

namespace PulseOreDetector
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    class PulseOreDetectorMod : MySessionComponentBase
    {

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
        }

        protected override void UnloadData()
        {
            Instance = null;
            MyAPIGateway.TerminalControls.CustomActionGetter -= AddAction;
            MyAPIGateway.TerminalControls.CustomControlGetter -= AddControl;
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

    }
}
