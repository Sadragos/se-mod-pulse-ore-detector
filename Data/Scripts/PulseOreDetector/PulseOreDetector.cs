using PulseOreDetector.Proto;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
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
        private IMyTerminalBlock Block;
        private DateTime NextPulse;

        IEnumerator<string> ScanRun;
        string CurrentScanStatus = "Idle";

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
                NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;

                Block = (IMyTerminalBlock)Entity;
                Block.AppendingCustomInfo += AppendInfo;
            }
            catch (Exception e)
            {
                MyLog.Default.Error("Error in Pulse Scanner Init", e);
            }
        }

        private bool IsScanning { get { return ScanRun != null;  } }

        private void AppendInfo(IMyTerminalBlock block, StringBuilder sb)
        {
            var logic = GetLogic(block);
            if (logic == null) return;

            var diffInSeconds = (int)(NextPulse - DateTime.Now).TotalSeconds;
            if(IsScanning)
            {
                sb.AppendLine(CurrentScanStatus);
            } else if(diffInSeconds <= 0)
            {
                sb.AppendLine("Idle");
            } else
            {
                sb.AppendFormat("Detector on Cooldown: {0}\n", diffInSeconds);
            }
        }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();

            if (ScanRun != null)
            {
                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
                return;
            }
            PulseOreDetectorMod.Instance.ScanButton.UpdateVisual();
            Block.RefreshCustomInfo();
            Utilities.RefreshControls(Block);
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();
            if(ScanRun == null)
            {
                NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
                return;
            } else
            {
                bool next = false;
                try
                {
                    next = ScanRun.MoveNext();
                } catch (Exception e)
                {
                    MyLog.Default.WriteLine("PulseOreDetector Error while Scanning " + e.ToString());
                }
                if(!next || ScanRun.Current == null)
                {
                    ScanRun = null;
                } else
                {
                    CurrentScanStatus = ScanRun.Current;
                }
                Block.RefreshCustomInfo();
                Utilities.RefreshControls(Block);
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
            PulseOreDetectorMod.Instance.ScanButton = scanButton;

            var action = MyAPIGateway.TerminalControls.CreateAction<T>("PulseScanStart");
            action.Name = new StringBuilder("Pulse Scan");
            action.Action = Action;
            action.Enabled = Control_Enabled;
            action.ValidForGroups = false;
            PulseOreDetectorMod.Instance.ScanAction = action;
        }

        static PulseOreDetector GetLogic(IMyTerminalBlock block) => block?.GameLogic?.GetAs<PulseOreDetector>();

        static bool CooldownDone(PulseOreDetector logic)
        {
            if (logic == null) return false;
            return !logic.IsScanning && DateTime.Now > logic.NextPulse;
        }

        static bool Control_Visible(IMyTerminalBlock block)
        {
            return Config.Instance != null && GetLogic(block) != null;
        }

        static bool Control_Enabled(IMyTerminalBlock block)
        {
            if (Config.Instance == null) return false;
            var logic = GetLogic(block);
            if (logic == null) return false;
            return CooldownDone(logic);
        }

        static void Action(IMyTerminalBlock block)
        {
            var logic = GetLogic(block);
            if (logic == null) return;

            if(!CooldownDone(logic))
            {
                var diffInSeconds = (int) (logic.NextPulse - DateTime.Now).TotalSeconds;
                Utilities.ShowNotificationLocal(string.Format("The Pulse Ore Detector is still on Cooldown for [{0}] Seconds.", diffInSeconds), 3000, MyFontEnum.Red);

                return;
            }
            logic.ScanRun = ScanForOres(logic);
        }

        static IEnumerator<string> ScanForOres(PulseOreDetector logic)
        {
            PulseOreDetectorMod.Instance.ScanButton.UpdateVisual();

            var currentAsteroidList = new List<IMyVoxelBase>();
            var position = logic.Block.GetPosition();

            MyAPIGateway.Session.VoxelMaps.GetInstances(currentAsteroidList, v => Math.Sqrt((position - v.PositionLeftBottomCorner).LengthSquared()) < Math.Sqrt(Math.Pow(v.Storage.Size.X, 2) * 3) + Config.Instance.MaxRange + 500f);
            yield return "Got Material Information";

            List<ScanHit> scanHits = new List<ScanHit>();

            bool overload = false;
            int voxel = 0;
            foreach (var voxelMap in currentAsteroidList)
            {
                voxel++;
                yield return "Starting Scan " + voxel + " / " + currentAsteroidList.Count;

                IEnumerator<Progress> currentScan = FindMaterial(voxelMap, position, scanHits);

                while (true)
                {
                    bool next = false;
                    try
                    {
                        next = currentScan.MoveNext();
                        if (!next || currentScan.Current == null) break;
                    } catch (Exception e)
                    {
                        overload = true;
                        break;
                    }
                    float percent = (float)currentScan.Current.Current / currentScan.Current.Total * 100f;
                    string result = "Scan " + voxel + " / " + currentAsteroidList.Count + " -> " + percent.ToString("0.00") + "%" + "\n" + currentScan.Current.Current + " / " + currentScan.Current.Total;
                    if(overload)
                    {
                        result += "\nSENSORY OVERLOAD! Too much Input.";
                    }
                    yield return result;
                }
            }

            yield return "Scan complete. Analyzing Data.";

            var findMaterial = PulseOreDetectorMod.VoxelMaterials.Select(f => f.Index).ToArray();
            int current = 0;
            int hits = 0;
            foreach (ScanHit scanHit in scanHits)
            {
                current++;
                MySize size = Config.Instance.SizeDescriptions.FindLast(sd => sd.MinSize <= scanHit.Hits);
                if (size != null)
                {
                    hits++;
                    var index = Array.IndexOf(findMaterial, scanHit.Material);
                    var name = PulseOreDetectorMod.VoxelMaterials[index].MinedOre;
                    MyAlias alias = Config.Instance.OreNames.Find(on => on.Input.Equals(name));
                    name = alias != null ? alias.Output : name;
                    string gps = string.Format(Config.Instance.GPSFormat, name, size.Name);
                    MyAPIGateway.Session.GPS.AddGps(MyAPIGateway.Session.Player.IdentityId, MyAPIGateway.Session.GPS.Create(gps, "Pulse Ore Scanner", scanHit.Position, true, true));
                }
                yield return "Adding GPS Markes " + current + " / " + scanHits.Count;
            }
            yield return "Added GPS Points for relevant Spots";
            logic.NextPulse = DateTime.Now.AddSeconds(Config.Instance.CooldownSeconds);
            Utilities.ShowNotificationLocal(string.Format("[{0}] ore deposits found on [{1}] asteroids within [{2}m] range.", hits, currentAsteroidList.Count, Config.Instance.MaxRange));
        }

        private static IEnumerator<Progress> FindMaterial(IMyVoxelBase voxelMap, Vector3D center, List<ScanHit> scanHits)
        {
            var progress = new Progress(); ;
            double checkDistance = Config.Instance.BatchDistance * Config.Instance.BatchDistance; 
            var findMaterial = PulseOreDetectorMod.VoxelMaterials.Select(f => f.Index).ToArray();
            var storage = voxelMap.Storage;
            var scale = (int)Math.Pow(2, Config.Instance.Resolution);

            var point = new Vector3I(center - voxelMap.PositionLeftBottomCorner);

            var min = ((point - (int)Config.Instance.SafeMaxRange) / 64) * 64;
            min = Vector3I.Max(min, Vector3I.Zero);

            var max = ((point + (int)Config.Instance.SafeMaxRange) / 64) * 64;
            max = Vector3I.Max(max, min + 64);

            if (min.X >= storage.Size.X || min.Y >= storage.Size.Y || min.Z >= storage.Size.Z)
            {
                yield return null;
            }

            var oldCache = new MyStorageData();

            var smax = (max / scale) - 1;
            var smin = (min / scale);
            var size = smax - smin + 1;

            progress.Total = (long)size.X * (long)size.Y * (long)size.Z;
            yield return progress;

            oldCache.Resize(size);
            storage.ReadRange(oldCache, MyStorageDataTypeFlags.ContentAndMaterial, Config.Instance.Resolution, smin, smax);
            yield return progress;

            int step = 0;
            Vector3I p;
            for (p.Z = 0; p.Z < size.Z; ++p.Z)
                for (p.Y = 0; p.Y < size.Y; ++p.Y)
                    for (p.X = 0; p.X < size.X; ++p.X)
                    {
                        // place GPS near the center of the Voxel
                        Vector3D position = voxelMap.PositionLeftBottomCorner + (p * scale) + (scale / 2f) + min;

                        if (Math.Sqrt((position - center).LengthSquared()) < Config.Instance.SafeMaxRange)
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
                        progress.Current++;
                        if (step++ >= Config.Instance.MaxChecksPerTick)
                        {
                            step = 0;
                            yield return progress;
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

        protected class Progress
        {
            public long Current = 0;
            public long Total = 0;
            public bool Overload = false;
        }

    }
}
