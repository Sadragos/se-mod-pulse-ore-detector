using Sandbox.Definitions;
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
        public bool ControlsCreated = false;

        public static MyVoxelMaterialDefinition[] VoxelMaterials;

        public override void LoadData()
        {
            Instance = this;
            VoxelMaterials = MyDefinitionManager.Static.GetVoxelMaterialDefinitions().Where(v => v.IsRare).ToArray();
        }

        protected override void UnloadData()
        {
            Instance = null;
        }
    }
}
