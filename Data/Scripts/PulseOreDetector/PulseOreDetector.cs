using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;

namespace PulseOreDetector
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_TerminalBlock), false, "LargePulseDetector" , "SmallPulseDetector" )]
    class PulseOreDetector : MyGameLogicComponent
    {
        private static int CoolDown = 30;
        private static float Range = 2000;
        private static int Resolution = 3;
        private static int BatchDistance = 200;
        private static int MinHits = 2;

        private IMyTerminalBlock Block;
        private DateTime NextPulse;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            try
            {
                NextPulse = DateTime.Now;
                SetupTerminalControls<IMyTerminalBlock>();
                //NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
                // TODO update info

                Block = (IMyTerminalBlock)Entity;
            }
            catch (Exception e)
            {
                MyLog.Default.Error("Error in Pulse Scanner Init", e);
            }
        }

        static void SetupTerminalControls<T>()
        {
            var mod = PulseOreDetectorMod.Instance;

            if (mod.ControlsCreated)
                return;

            mod.ControlsCreated = true;

            var scanButton = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, T>("PulseScanStart");
            scanButton.Title = MyStringId.GetOrCompute("Scan");
            scanButton.Tooltip = MyStringId.GetOrCompute("Starts a long range scan for Ores.");
            scanButton.Visible = Control_Visible;
            scanButton.Enabled = Control_Enabled;
            scanButton.SupportsMultipleBlocks = false;
            scanButton.Action = Action;
            MyAPIGateway.TerminalControls.AddControl<T>(scanButton);

            var action = MyAPIGateway.TerminalControls.CreateAction<T>("PulseScanStart");
            action.Name?.Append("Pulse Scan");
            action.Action = Action;
            action.Enabled = Control_Enabled;
            action.ValidForGroups = false;
            MyAPIGateway.TerminalControls.AddAction<T>(action);
        }

        static PulseOreDetector GetLogic(IMyTerminalBlock block) => block?.GameLogic?.GetAs<PulseOreDetector>();

        static bool CooldownDone(PulseOreDetector logic)
        {
            if (logic == null) return false;
            return DateTime.Now > logic.NextPulse;
        }

        static bool Control_Visible(IMyTerminalBlock block)
        {
            return GetLogic(block) != null;
        }

        static bool Control_Enabled(IMyTerminalBlock block)
        {
            var logic = GetLogic(block);
            if (logic == null) return false;

            // TODO auf cooldown achten
            return true;
        }

        static void Action(IMyTerminalBlock block)
        {
            var logic = GetLogic(block);
            if (logic == null) return;

            if(!CooldownDone(logic))
            {
                // TODO Fehler anzeigen
                MyVisualScriptLogicProvider.SendChatMessageColored(string.Format("Still on cooldown"), Color.Red, "Ore Detector");
                return;
            }
            ScanForOres(logic);

            
        }

        static void ScanForOres(PulseOreDetector logic)
        {
            if (logic == null) return;
            logic.NextPulse = DateTime.Now.AddSeconds(CoolDown);

            var currentAsteroidList = new List<IMyVoxelBase>();
            var position = logic.Block.GetPosition();
            MyVisualScriptLogicProvider.SendChatMessageColored(string.Format("Pos: {0}", position), Color.Yellow, "Ore Detector");
            MyVisualScriptLogicProvider.SendChatMessageColored(string.Format("Voxelmaps: {0}", MyAPIGateway.Session.VoxelMaps != null), Color.Yellow, "Ore Detector");

            MyAPIGateway.Session.VoxelMaps.GetInstances(currentAsteroidList, v => Math.Sqrt((position - v.PositionLeftBottomCorner).LengthSquared()) < Math.Sqrt(Math.Pow(v.Storage.Size.X, 2) * 3) + Range + 500f);
            MyVisualScriptLogicProvider.SendChatMessageColored(string.Format("Got Astros"), Color.Yellow, "Ore Detector");


            List<ScanHit> scanHits = new List<ScanHit>();

            foreach (var voxelMap in currentAsteroidList)
            {
                FindMaterial(voxelMap, position, scanHits);
                MyVisualScriptLogicProvider.SendChatMessageColored(string.Format("Checked Astro {0}", voxelMap), Color.Yellow, "Ore Detector");
            }

            var findMaterial = PulseOreDetectorMod.VoxelMaterials.Select(f => f.Index).ToArray();
            foreach (ScanHit scanHit in scanHits)
            {
                if (MinHits > scanHit.Hits) continue;
                var index = Array.IndexOf(findMaterial, scanHit.Material);
                var name = PulseOreDetectorMod.VoxelMaterials[index].MinedOre;
                MyAPIGateway.Session.GPS.AddGps(MyAPIGateway.Session.Player.IdentityId, MyAPIGateway.Session.GPS.Create("POS " + name + " (" + scanHit.Hits + " Pings)", "Pulse Ore Scanner", scanHit.Position, true, true));
            }
            MyVisualScriptLogicProvider.SendChatMessageColored(string.Format("Added GPS"), Color.Yellow, "Ore Detector");


            MyVisualScriptLogicProvider.SendChatMessageColored(
                string.Format("{0} ore deposits found on {1} asteroids within {2}m range.", scanHits.Count, currentAsteroidList.Count, Range),
                Color.Green,
                "Ore Detector");
        }

        private static void FindMaterial(IMyVoxelBase voxelMap, Vector3D center, List<ScanHit> scanHits)
        {
            double checkDistance = BatchDistance * BatchDistance;  // 80 meter seperation
            var findMaterial = PulseOreDetectorMod.VoxelMaterials.Select(f => f.Index).ToArray();
            var storage = voxelMap.Storage;
            var scale = (int)Math.Pow(2, Resolution);

            //MyAPIGateway.Utilities.ShowMessage("center", center.ToString());
            var point = new Vector3I(center - voxelMap.PositionLeftBottomCorner);
            //MyAPIGateway.Utilities.ShowMessage("point", point.ToString());

            var min = ((point - (int)Range) / 64) * 64;
            min = Vector3I.Max(min, Vector3I.Zero);
            //MyAPIGateway.Utilities.ShowMessage("min", min.ToString());

            var max = ((point + (int)Range) / 64) * 64;
            max = Vector3I.Max(max, min + 64);
            //MyAPIGateway.Utilities.ShowMessage("max", max.ToString());

            //MyAPIGateway.Utilities.ShowMessage("size", voxelMap.StorageName + " " + storage.Size.ToString());

            if (min.X >= storage.Size.X ||
                min.Y >= storage.Size.Y ||
                min.Z >= storage.Size.Z)
            {
                //MyAPIGateway.Utilities.ShowMessage("size", "out of range");
                return;
            }

            var oldCache = new MyStorageData();

            //var smin = new Vector3I(0, 0, 0);
            //var smax = new Vector3I(31, 31, 31);
            ////var size = storage.Size;
            //var size = smax - smin + 1;
            //size = new Vector3I(16, 16, 16);
            //oldCache.Resize(size);
            //storage.ReadRange(oldCache, MyStorageDataTypeFlags.ContentAndMaterial, resolution, Vector3I.Zero, size - 1);

            var smax = (max / scale) - 1;
            var smin = (min / scale);
            var size = smax - smin + 1;
            oldCache.Resize(size);
            storage.ReadRange(oldCache, MyStorageDataTypeFlags.ContentAndMaterial, Resolution, smin, smax);

            //MyAPIGateway.Utilities.ShowMessage("smax", smax.ToString());
            //MyAPIGateway.Utilities.ShowMessage("size", size .ToString());
            //MyAPIGateway.Utilities.ShowMessage("size - 1", (size - 1).ToString());

            Vector3I p;
            for (p.Z = 0; p.Z < size.Z; ++p.Z)
                for (p.Y = 0; p.Y < size.Y; ++p.Y)
                    for (p.X = 0; p.X < size.X; ++p.X)
                    {
                        // place GPS in the center of the Voxel
                        Vector3D position = voxelMap.PositionLeftBottomCorner + (p * scale) + (scale / 2f) + min;

                        if (Math.Sqrt((position - center).LengthSquared()) < Range)
                        {
                            byte content = oldCache.Content(ref p);
                            byte material = oldCache.Material(ref p);

                            if (content > 0 && findMaterial.Any(m => m == material))
                            {
                                bool addHit = true;
                                foreach (ScanHit scanHit in scanHits)
                                {
                                    if (scanHit.Material == material && Vector3D.DistanceSquared(position, scanHit.Position) < checkDistance)
                                    {
                                        scanHit.Hits++;
                                        addHit = false;
                                        break;
                                    }
                                }
                                if (addHit)
                                    scanHits.Add(new ScanHit(position, material));
                            }
                        }
                    }
        }

        protected class ScanHit
        {
            public Vector3D Position;
            public byte Material;
            public int Hits;

            public ScanHit(Vector3D position, byte material)
            {
                Position = position;
                Material = material;
                Hits = 1;
            }
        }
    }
}
