﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
namespace LiveSplit.OriWotW {
    public partial class MemoryManager {
        //__mainWisp.Game.Characters.SetCurrentCharacter
        private static ProgramPointer Characters = new ProgramPointer(AutoDeref.Single, new ProgramSignature(PointerVersion.V1, "488B80B80000004C8B40084D85C0743D488B15????????B90C000000E8????????488BF84885DB743C488B4B304885C9742D33D2E8????????4885C0741B48897818488B5C24504883C4405FC3", -0x4));
        //__mainWisp.GameWorld.Awake
        private static ProgramPointer GameWorld = new ProgramPointer(AutoDeref.Single, new ProgramSignature(PointerVersion.V1, "4C8BDC55565741544155415641574883EC5049C743A8FEFFFFFF49895B104C8BE933ED", 0xa7));
        //__mainWisp.Seinlevel.get_PartialHealthContainers
        private static ProgramPointer PlayerUberStateGroup = new ProgramPointer(AutoDeref.Single, new ProgramSignature(PointerVersion.V1, "488B05????????488B88B8000000488B014885C0742C488B48184885C9741D33D2E8????????4885C07423488B40184885C074148B40384883C448C3", 0x3));
        //__mainWisp.TitleScreenManager.Awake
        private static ProgramPointer TitleScreenManager = new ProgramPointer(AutoDeref.Single, new ProgramSignature(PointerVersion.V1, "9033C9FF15????????90C605????????01488B05????????F6802701000002741883B8D800000000750F488BC8E8????????488B05????????488B80B8000000488928488B05", 0x35));
        //__mainWisp.GameStateMachine.get_Instance
        private static ProgramPointer GameStateMachine = new ProgramPointer(AutoDeref.Single, new ProgramSignature(PointerVersion.V1, "9033C9FF15????????90C605????????01488B1D????????488B83B8000000488B004885C00F85C6000000488BCBE8????????488B43604885C074278B08E8", 0x14));
        //__mainWisp.GameController.OnGameAwake
        private static ProgramPointer GameController = new ProgramPointer(AutoDeref.Single, new ProgramSignature(PointerVersion.V1, "014C8975288B04244883EC20488D4C24308B0148894D20C785C0000000FFFFFFFF488B05????????F6802701000002741883B8D800000000750F488BC8", 0x45));
        //__mainWisp.SeinWorldState.Awake
        private static ProgramPointer SeinWorldState = new ProgramPointer(AutoDeref.Single, new ProgramSignature(PointerVersion.V1, "9033C9FF15????????90C605????????01488B05????????488B88B8000000488939BA0E000000488B0D????????E8????????488BD8488B77184885C0", 0x14));
        //__mainWisp.ScenesManager.Awake
        private static ProgramPointer ScenesManager = new ProgramPointer(AutoDeref.Single, new ProgramSignature(PointerVersion.V1, "9033C9FF15????????90C605????????01488B05????????488B88B8000000488931488B1D????????488BCBE8????????488B43604885C074278B08E8????????483B05????????7517", 0x14));
        public Process Program { get; set; }
        public bool IsHooked { get; set; }
        public DateTime LastHooked { get; set; }

        public MemoryManager() {
            LastHooked = DateTime.MinValue;
        }
        public string GamePointers() {
            return $"CHR: {Characters.GetPointer(Program):X} | GW: {GameWorld.GetPointer(Program):X} | PUS: {PlayerUberStateGroup.GetPointer(Program):X} | TSM: {TitleScreenManager.GetPointer(Program):X} | GSM: {GameStateMachine.GetPointer(Program):X} | GC: {GameController.GetPointer(Program):X} | SWS: {SeinWorldState.GetPointer(Program):X}";
        }
        public int MaxEnergy() {
            //Characters.Sein.Energy.m_maxEnergyCached
            return (int)Characters.Read<float>(Program, 0xb8, 0x10, 0x80, 0x38) * 2;
        }
        public int MaxHealth() {
            //Characters.Sein.Mortality.Health.m_maxHealthCached
            return (int)Characters.Read<float>(Program, 0xb8, 0x10, 0x88, 0x18, 0x34) / 5;
        }
        public int HealthFragments() {
            //PlayerUberStateGroup.Instance.PlayerUberState.m_state.Inventory.m_partialHealthContainers
            return PlayerUberStateGroup.Read<int>(Program, 0xb8, 0x0, 0x18, 0x30, 0x18, 0x38);
        }
        public int EnergyFragments() {
            //PlayerUberStateGroup.Instance.PlayerUberState.m_state.Inventory.m_partialEnergyContainers
            return PlayerUberStateGroup.Read<int>(Program, 0xb8, 0x0, 0x18, 0x30, 0x18, 0x3c);
        }
        public int Keystones() {
            //PlayerUberStateGroup.Instance.PlayerUberState.m_state.Inventory.m_keystones
            return PlayerUberStateGroup.Read<int>(Program, 0xb8, 0x0, 0x18, 0x30, 0x18, 0x28);
        }
        public int Ore() {
            //PlayerUberStateGroup.Instance.PlayerUberState.m_state.Inventory.m_ore
            return PlayerUberStateGroup.Read<int>(Program, 0xb8, 0x0, 0x18, 0x30, 0x18, 0x34);
        }
        public Vector2 Position() {
            //Characters.Sein.PlatformBehaviour.PlatformMovement.m_prevPosition
            return Characters.Read<Vector2>(Program, 0xb8, 0x10, 0x98, 0x18, 0xd0);
        }
        public string ActiveScene() {
            Vector4 position = new Vector4(Position(), 0f, 0f, true);

            //Scenes.Manager.ActiveScenes
            IntPtr scenes = (IntPtr)ScenesManager.Read<ulong>(Program, 0xb8, 0x0, 0x60);
            //.Count
            int count = Program.Read<int>(scenes, 0x18);
            //.Items
            scenes = (IntPtr)Program.Read<ulong>(scenes, 0x10);
            IntPtr[] cache = new IntPtr[count];
            for (int i = 0; i < count; i++) {
                //.Items[i].MetaData
                IntPtr runtimeScene = (IntPtr)Program.Read<ulong>(scenes, 0x20 + (i * 0x8), 0x18);

                //.DependantScene
                bool dependantScene = Program.Read<bool>(runtimeScene, 0x74);
                if (dependantScene) { continue; }
                cache[i] = runtimeScene;

                //.SceneBoundaries
                IntPtr sceneBoundaries = (IntPtr)Program.Read<ulong>(runtimeScene, 0x30);
                //.SceneBoundaries.Count
                int boundaryCount = Program.Read<int>(sceneBoundaries, 0x18);
                //.SceneBoundaries.Items
                sceneBoundaries = (IntPtr)Program.Read<ulong>(sceneBoundaries, 0x10);
                for (int j = 0; j < boundaryCount; j++) {
                    //.SceneBoundaries.Items[j]
                    Vector4 boundary = Program.Read<Vector4>(sceneBoundaries, 0x20 + (j * 0x8));
                    boundary.Y += boundary.H;
                    if (boundary.Intersects(position)) {
                        //.Scene
                        return Program.ReadString(runtimeScene, 0x10, 0x0);
                    }
                }
            }

            for (int i = 0; i < count; i++) {
                //.Items[i].MetaData
                IntPtr runtimeScene = cache[i];
                if (runtimeScene == IntPtr.Zero) { continue; }

                //.ScenePaddingBoundaries
                IntPtr sceneBoundaries = (IntPtr)Program.Read<ulong>(runtimeScene, 0x38);
                //.ScenePaddingBoundaries.Count
                int boundaryCount = Program.Read<int>(sceneBoundaries, 0x18);
                //.ScenePaddingBoundaries.Items
                sceneBoundaries = (IntPtr)Program.Read<ulong>(sceneBoundaries, 0x10);
                for (int j = 0; j < boundaryCount; j++) {
                    //.ScenePaddingBoundaries.Items[j]
                    Vector4 boundary = Program.Read<Vector4>(sceneBoundaries, 0x20 + (j * 0x8));
                    boundary.Y += boundary.H;
                    if (boundary.Intersects(position)) {
                        //.Scene
                        return Program.ReadString(runtimeScene, 0x10, 0x0);
                    }
                }
            }

            return string.Empty;
        }
        public List<WorldStateValue> WorldStates() {
            List<WorldStateValue> currentStates = new List<WorldStateValue>();
            //SeinWorldState.Instance
            foreach (WorldState key in Enum.GetValues(typeof(WorldState))) {
                int value = 0;
                string description = string.Empty;
                if (key == WorldState.DarknessLifted || key == WorldState.MistLifted || key == WorldState.WaterPurified) {
                    value = SeinWorldState.Read<byte>(Program, 0xb8, 0x0, 0x8 * (int)key, 0x40);
                    description = value > 0 ? "Completed" : string.Empty;
                } else {
                    value = SeinWorldState.Read<int>(Program, 0xb8, 0x0, 0x8 * (int)key, 0x38);
                    if (value > 0) {
                        description = SeinWorldState.Read(Program, 0xb8, 0x0, 0x8 * (int)key, 0x40, 0x10, 0x20 + (value * 0x8), 0x10, 0x0);
                    }
                }

                if (value > 0) {
                    currentStates.Add(new WorldStateValue() { State = key, Value = value, Description = description });
                }
            }

            currentStates.Sort(delegate (WorldStateValue one, WorldStateValue two) {
                return one.State.CompareTo(two.State);
            });
            return currentStates;
        }
        public bool HasAbility(AbilityType type) {
            //PlayerUberStateGroup.Instance.PlayerUberState.m_state.Abilities.m_abilitiesList
            IntPtr abilities = (IntPtr)PlayerUberStateGroup.Read<ulong>(Program, 0xb8, 0x0, 0x18, 0x30, 0x10, 0x18);
            //.Count
            int count = Program.Read<int>(abilities, 0x18);
            //.Items
            abilities = (IntPtr)Program.Read<ulong>(abilities, 0x10);
            for (int i = 0; i < count; i++) {
                Ability ability = Program.Read<Ability>(abilities, 0x20 + (i * 0x8), 0x10);
                if (ability.Type == type) {
                    return ability.HasAbility == 1;
                }
            }
            return false;
        }
        public List<AbilityType> PlayerAbilities() {
            List<AbilityType> currentAbilities = new List<AbilityType>();
            //PlayerUberStateGroup.Instance.PlayerUberState.m_state.Abilities.m_abilitiesList
            IntPtr abilities = (IntPtr)PlayerUberStateGroup.Read<ulong>(Program, 0xb8, 0x0, 0x18, 0x30, 0x10, 0x18);
            //.Count
            int count = Program.Read<int>(abilities, 0x18);
            //.Items
            abilities = (IntPtr)Program.Read<ulong>(abilities, 0x10);
            for (int i = 0; i < count; i++) {
                Ability ability = Program.Read<Ability>(abilities, 0x20 + (i * 0x8), 0x10);
                if (ability.HasAbility == 1) {
                    currentAbilities.Add(ability.Type);
                }
            }
            currentAbilities.Sort(delegate (AbilityType one, AbilityType two) {
                return one.CompareTo(two);
            });
            return currentAbilities;
        }
        public bool HasShard(ShardType type) {
            //PlayerUberStateGroup.Instance.PlayerUberState.m_state.Shards.m_shardsList
            IntPtr shards = (IntPtr)PlayerUberStateGroup.Read<ulong>(Program, 0xb8, 0x0, 0x18, 0x30, 0x20, 0x18);
            //.Count
            int count = Program.Read<int>(shards, 0x18);
            //.Items
            shards = (IntPtr)Program.Read<ulong>(shards, 0x10);
            for (int i = 0; i < count; i++) {
                Shard shard = Program.Read<Shard>(shards, 0x20 + (i * 0x8), 0x10);
                if (shard.Type == type) {
                    return shard.Gained == 1;
                }
            }
            return false;
        }
        public List<ShardType> PlayerShards() {
            List<ShardType> currentShards = new List<ShardType>();
            //PlayerUberStateGroup.Instance.PlayerUberState.m_state.Shards.m_shardsList
            IntPtr shards = (IntPtr)PlayerUberStateGroup.Read<ulong>(Program, 0xb8, 0x0, 0x18, 0x30, 0x20, 0x18);
            //.Count
            int count = Program.Read<int>(shards, 0x18);
            //.Items
            shards = (IntPtr)Program.Read<ulong>(shards, 0x10);
            for (int i = 0; i < count; i++) {
                Shard shard = Program.Read<Shard>(shards, 0x20 + (i * 0x8), 0x10);
                if (shard.Gained == 1) {
                    currentShards.Add(shard.Type);
                }
            }
            currentShards.Sort(delegate (ShardType one, ShardType two) {
                return one.CompareTo(two);
            });
            return currentShards;

            //Read from dictionary
            //List<SpiritShardType> currentShards = new List<SpiritShardType>();
            ////PlayerUberStateGroup.Instance.PlayerUberState.m_state.Shards.m_shards
            //IntPtr shards = (IntPtr)PlayerUberStateGroup.Read<ulong>(Program, 0xb8, 0x0, 0x18, 0x30, 0x20, 0x10);
            ////.Count
            //int count = Program.Read<int>(shards, 0x20);
            ////.Values
            //shards = (IntPtr)Program.Read<ulong>(shards, 0x18);
            //for (int i = 0; i < count; i++) {
            //    Shard shard = Program.Read<Shard>(shards, 0x20 + (i * 0x18) + 0x10, 0x10);
            //    if (shard.Gained == 1) {
            //        currentShards.Add(shard.Type);
            //    }
            //}
        }
        public List<EquipmentType> Inventory() {
            List<EquipmentType> currentInventory = new List<EquipmentType>();
            //PlayerUberStateGroup.Instance.PlayerUberState.m_state.Inventory.m_inventory
            IntPtr inventory = (IntPtr)PlayerUberStateGroup.Read<ulong>(Program, 0xb8, 0x0, 0x18, 0x30, 0x18, 0x10);
            //.Count
            int inventoryCount = Program.Read<int>(inventory, 0x18);
            //.Items
            inventory = (IntPtr)Program.Read<ulong>(inventory, 0x10);
            for (int i = 0; i < inventoryCount; i++) {
                InventoryItem item = Program.Read<InventoryItem>(inventory, 0x20 + (i * 0x8), 0x10);
                if (item.Gained == 1) {
                    currentInventory.Add(item.Type);
                }
            }
            currentInventory.Sort(delegate (EquipmentType one, EquipmentType two) {
                return one.CompareTo(two);
            });
            return currentInventory;
        }
        public float MapCompletion() {
            float totalCompletion = 0;
            //GameWorld.RuntimeAreas
            IntPtr areas = (IntPtr)GameWorld.Read<ulong>(Program, 0xb8, 0x0, 0x28);
            //.Count
            int areaCount = Program.Read<int>(areas, 0x18);
            //.Items
            areas = (IntPtr)Program.Read<ulong>(areas, 0x10);
            for (int i = 0; i < areaCount; i++) {
                //.Items[i].m_completionAmount
                totalCompletion += Program.Read<float>(areas, 0x20 + (i * 8), 0x34);
            }
            return totalCompletion * 100f / areaCount;
        }
        public GameWorldAreaID PlayerArea() {
            //GameWorld.CurrentArea.Area.AreaNameString
            return GameWorld.Read<GameWorldAreaID>(Program, 0xb8, 0x0, 0x30, 0x10, 0x20);
        }
        public double ElapsedTime() {
            //GameController.Instance.Timer.CurrentTime
            return GameController.Read<double>(Program, 0xb8, 0x0, 0x28, 0x20);
        }
        public bool Dead() {
            //Characters.Sein.PlatformBehaviour.Mortality.DamageReciever.m_died
            return Characters.Read<bool>(Program, 0xb8, 0x10, 0x88, 0x10, 0xe4);
        }
        public GameState GameState() {
            //GameStateMachine.m_instance.CurrentState
            return (GameState)GameStateMachine.Read<int>(Program, 0xb8, 0x0, 0x10);
        }
        public Screen MainMenuScreen() {
            //TitleScreenManager.Instance.m_currentScreen
            return (Screen)TitleScreenManager.Read<int>(Program, 0xb8, 0x0, 0xb8);
        }
        public bool IsLoadingGame() {
            //GameController.Instance.m_isLoadingGame
            return GameController.Read<bool>(Program, 0xb8, 0x0, 0x48, 0xd0, 0x20);
        }
        public bool HookProcess() {
            IsHooked = Program != null && !Program.HasExited;
            if (!IsHooked && DateTime.Now > LastHooked.AddSeconds(1)) {
                LastHooked = DateTime.Now;
                Process[] processes = Process.GetProcessesByName("OriWotW");
                Program = processes != null && processes.Length > 0 ? processes[0] : null;

                if (Program != null && !Program.HasExited) {
                    MemoryReader.Update64Bit(Program);
                    IsHooked = true;
                }
            }

            return IsHooked;
        }
        public void Dispose() {
            if (Program != null) {
                Program.Dispose();
            }
        }
    }
}