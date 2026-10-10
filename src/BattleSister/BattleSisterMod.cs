using AX.ModLoader;
using DXVision;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using Entity_Registry_Tools;
using ZX;
using ZX.Commands;
using ZX.Components;
using ZX.Entities;

namespace BattleSisterMod
{
    public sealed class BattleSister : ShooterSoldier
    {
        private static DXProjectImage _portrait;

        public BattleSister() { }

        internal static void SetPortrait(DXProjectImage portrait)
        {
            _portrait = portrait;
        }

        public override List<Type> DefaultComponents
        {
            get
            {
                List<Type> components = base.DefaultComponents;
                components.Add(typeof(BattleSisterAura));
                return components;
            }
        }

        public override DXProjectImage GetPortraitImage()
        {
            return _portrait ?? Images.Portraits.get__Ranger();
        }
    }

    public sealed class BattleSisterAttack : ZXCommand
    {
        protected override void OnPerformEffect(ZXEntity actor, ZXCommandTarget target)
        {
            base.OnPerformEffect(actor, target);
        }
    }

    public sealed class BattleSisterAttackVeteran : ZXCommand
    {
        protected override void OnPerformEffect(ZXEntity actor, ZXCommandTarget target)
        {
            base.OnPerformEffect(actor, target);
        }
    }

    public sealed class ModEntry : IModEntry
    {
        private const string HarmonyId = "local.battlesister.aura";
        private Harmony _harmony;

        private readonly EntityRegistry.EntityData _entityData = new EntityRegistry.EntityData
        {
            entityName = "BattleSister",
            spriteAtlasEntityName = "HumansA",
            spriteAtlasAnimationName = "Ranger",
            useSpriteParameters = true
        };

        private readonly EntityRegistry.EntityCommandData _commandData = new EntityRegistry.EntityCommandData
        {
            BaseEntityType = typeof(Ranger),
            ModdedEntityType = typeof(BattleSister),
            AttackCommandType = typeof(BattleSisterAttack),
            VeteranAttackCommandType = typeof(EntityRegistry.NoCommand),
            TechCommandType = typeof(EntityRegistry.NoCommand)
        };

        private readonly EntityRegistry.DefaultColorData _color = new EntityRegistry.DefaultColorData
        {
            colorToReplace = Color.White,
            colorToChangeTo = Color.White,
            factorTolerance = 0,
            saturation = 1f,
            brightness = 1f,
            contrast = 1f,
            hueRotation = 0,
            colorHue = Color.Firebrick,
            // Apply portrait tint per Battle Sister UI object; the Registry's shared portrait
            // drawing state can bleed into unrelated unit portraits.
            changePortraitColors = false
        };

        public override void OnLoad(Mod mod)
        {
            BattleSisterLog.Write("OnLoad reached; mod entry was discovered by Mod Loader.");
        }

        public override void OnLoadResources()
        {
            BattleSisterLog.Write("OnLoadResources entered; registering entity and barracks command.");
            try
            {
                BattleSister.SetPortrait(CreateIsolatedRedRangerPortrait());
                EnsureRegistrationRuleRows();
                ZXCommand sisterAttack;
                ZXCommand sisterVeteranAttack;
                EntityRegistry.RegisterEntity(_entityData, _commandData, out sisterAttack, out sisterVeteranAttack, null, _color);

                ConfigureRuntimeValues(sisterAttack, sisterVeteranAttack);

                InstallAuraPatches();
                BattleSisterLog.Write("Entity registration and runtime setup completed.");
            }
            catch (Exception ex)
            {
                BattleSisterLog.Write("ERROR during resource registration: " + ex);
            }
        }

        private static DXProjectImage CreateIsolatedRedRangerPortrait()
        {
            DXProjectImage source = Images.Portraits.get__Ranger();
            Bitmap sourceBitmap = source == null ? null : source.Bitmap;
            if (sourceBitmap == null && source != null && source.Image != null)
                sourceBitmap = source.Image.PreviewBitmap;
            if (sourceBitmap == null)
            {
                BattleSisterLog.Write("Could not read the Ranger portrait bitmap; using the unmodified source portrait.");
                return source;
            }

            Bitmap recolored = new Bitmap(sourceBitmap);
            for (int y = 0; y < recolored.Height; y++)
            for (int x = 0; x < recolored.Width; x++)
            {
                Color pixel = recolored.GetPixel(x, y);
                if (pixel.A == 0 || pixel.GetSaturation() < 0.12f) continue;
                float hue = pixel.GetHue();
                if (hue >= 70f && hue <= 175f)
                    recolored.SetPixel(x, y, ColorFromHsl(2f, pixel.GetSaturation(), pixel.GetBrightness(), pixel.A));
            }

            // Do not MemberwiseClone DXProjectImage: its internal DXImageFile reference
            // would be shared with Ranger, and renaming the clone moves Ranger.png.
            DXProjectImage copy = new DXProjectImage();
            copy.Project = DXProject.Current;
            // The game's Portraits category is atlas-optimized. A runtime-created image
            // is absent from that atlas, so ImageArea indexes a missing atlas entry and
            // crashes when army buttons render. Keep this generated portrait uncategorized.
            copy.Category = null;
            copy.Name = "BattleSisterPortrait";
            copy.ID = source.ID ^ 0x4253495354455255UL;
            // Image is backed by DXImageFile; assigning the recolored bitmap writes a
            // unique PNG and refreshes this new image's independent file cache.
            copy.Bitmap = recolored;
            return copy;
        }

        private static Color ColorFromHsl(float hue, float saturation, float lightness, int alpha)
        {
            float chroma = (1f - Math.Abs(2f * lightness - 1f)) * saturation;
            float h = hue / 60f;
            float x = chroma * (1f - Math.Abs(h % 2f - 1f));
            float r = 0f, g = 0f, b = 0f;
            if (h < 1f) { r = chroma; g = x; }
            else if (h < 2f) { r = x; g = chroma; }
            else if (h < 3f) { g = chroma; b = x; }
            else if (h < 4f) { g = x; b = chroma; }
            else if (h < 5f) { r = x; b = chroma; }
            else { r = chroma; b = x; }
            float m = lightness - chroma / 2f;
            return Color.FromArgb(alpha, (int)((r + m) * 255f), (int)((g + m) * 255f), (int)((b + m) * 255f));
        }

        private static void EnsureRegistrationRuleRows()
        {
            DXTable entities = ZXGame.get_Current().TableManagerDefinitions.Tables["Entities"];
            DXTable commands = ZXGame.get_Current().TableManagerDefinitions.Tables["Commands"];

            CloneRuleRow(entities, "Ranger", "BattleSister");
            CloneRuleRow(commands, "RangerAttack", "BattleSisterAttack");
            CloneRuleRow(commands, "RangerAttackVeteran", "BattleSisterAttackVeteran");

            int attackCommandColumn;
            string[] entityRow;
            if (entities.Rows.TryGetValue("BattleSister", out entityRow) &&
                entities.Cols.TryGetValue("AttackCommand", out attackCommandColumn))
                entityRow[attackCommandColumn] = "BattleSisterAttack";

            BattleSisterLog.Write("Prepared ZXRules rows for BattleSister and its two attack commands.");
        }

        private static void CloneRuleRow(DXTable table, string sourceId, string targetId)
        {
            if (table.Rows.ContainsKey(targetId))
                return;
            string[] source;
            if (!table.Rows.TryGetValue(sourceId, out source))
                throw new InvalidOperationException("Cannot clone missing ZXRules row '" + sourceId + "' in table '" + table.Name + "'.");
            table.Rows.Add(targetId, (string[])source.Clone());
        }

        public override void OnUnload()
        {
            if (_harmony != null)
                _harmony.UnpatchAll(HarmonyId);
            AuraState.Clear();
        }

        private static void ConfigureRuntimeValues(ZXCommand attack, ZXCommand veteranAttack)
        {
            DXEntityTemplate rangerTemplate = DXProject.Current.EntityTemplates.Values
                .FirstOrDefault(t => t.ClassType == typeof(Ranger));
            DXEntityTemplate sisterTemplate = DXProject.Current.EntityTemplates.Values
                .FirstOrDefault(t => t.ClassType == typeof(BattleSister));
            ZXEntity ranger = rangerTemplate == null ? null : rangerTemplate.Entity as ZXEntity;
            ZXEntity sister = sisterTemplate == null ? null : sisterTemplate.Entity as ZXEntity;
            if (ranger == null || sister == null || ranger.get_Params() == null)
            {
                BattleSisterLog.Write("Ranger or BattleSister template parameters were unavailable.");
                return;
            }

            ZXEntityDefaultParams unitValues = CopyProperties(ranger.get_Params(), new ZXEntityDefaultParams("BattleSister"));
            attack = ResolveAttackCommand(attack, typeof(BattleSisterAttack), "BattleSisterAttack");
            // The veteran shares the custom attack command; Entity Registry's veteran portrait
            // style hook tints pooled portrait objects and can leak the tint to unrelated units.
            veteranAttack = null;
            unitValues.Name = "战斗修女";
            unitValues.Description = "身披红色战甲的精锐女战士。生命值350，攻击伤害为游侠的1.5倍，射程比游侠多1格，攻击速度比同级游侠高1。她的生命光环庇护周围6格内的游侠：提供100点优先承受伤害的临时生命，并使攻击伤害提高20%。训练需要额外100金币和2钢铁。";
            unitValues.Life = 350;
            unitValues.WatchRange = ranger.get_Params().WatchRange + 1;
            unitValues.GoldCost = ranger.get_Params().GoldCost + 100;
            unitValues.IronCost = 2;
            unitValues.BuildingFrom = typeof(SoldiersCenter);
            unitValues.BuildingRequisite = typeof(SoldiersCenter);
            unitValues.AttackCommandBasic = attack;
            unitValues.AttackCommandVeteran = attack;
            unitValues.PanelPosition = FindFreeSoldiersCenterPanelPosition();
            AddEntityDefaults("BattleSister", unitValues);
            sister.SetParams();

            CopyRangerAttackValues(attack, "BattleSisterAttack", 1.5f);

            BattleSisterLog.Write("BattleSister combat setup: basic=" +
                (unitValues.AttackCommandBasic == null ? "null" : unitValues.AttackCommandBasic.GetType().Name) +
                "; veteran=" + (unitValues.AttackCommandVeteran == null ? "null" : unitValues.AttackCommandVeteran.GetType().Name) +
                "; damage=" + (attack == null || attack.get_Params() == null ? "unavailable" : attack.get_Params().Damage.ToString()) +
                "; range=" + (attack == null || attack.get_Params() == null ? "unavailable" : attack.get_Params().ActionRange.ToString()));

            RegisterTrainCommand();
        }

        private static ZXCommand ResolveAttackCommand(ZXCommand registered, Type commandType, string id)
        {
            if (registered != null) return registered;
            ZXCommand existing = ZXGame.get_Current().Commands.FirstOrDefault(c => c.GetType() == commandType);
            if (existing != null) return existing;

            ZXCommand created = (ZXCommand)Activator.CreateInstance(commandType);
            created.ReadParams();
            ZXGame.get_Current().Commands.Add(created);
            if (created.get_Params() != null)
            {
                if (created.get_Params().DefaultValues == null)
                    created.get_Params().DefaultValues = new ZXCommandDefaultParams(id);
                if (!ZXDefaultParams.get_All().Contains(created.get_Params()))
                    ZXDefaultParams.get_All().Add(created.get_Params());
            }
            BattleSisterLog.Write("Recovered missing attack command registration: " + id + ".");
            return created;
        }

        private static int FindFreeSoldiersCenterPanelPosition()
        {
            HashSet<int> used = new HashSet<int>(ZXGame.get_Current().DicDefaultParamsPerTemplate.Values
                .Where(p => p.BuildingFrom == typeof(SoldiersCenter))
                .Select(p => p.PanelPosition));
            for (int slot = 1; slot <= 14; slot++)
                if (slot != 11 && !used.Contains(slot))
                    return slot;
            BattleSisterLog.Write("No free SoldiersCenter panel position was found; retaining the Ranger panel position.");
            ZXEntityDefaultParams ranger = ZXGame.get_Current().GetParamsFromEntityType(typeof(Ranger));
            return ranger == null ? 0 : ranger.PanelPosition;
        }

        private static void CopyRangerAttackValues(ZXCommand target, string id, float damageFactor)
        {
            if (target == null)
                return;

            ZXCommand baseCommand = ZXGame.get_Current().Commands
                .FirstOrDefault(c => c.GetType() == (id == "BattleSisterAttack"
                    ? typeof(RangerAttack) : typeof(RangerAttackVeteran)));
            ZXCommandDefaultParams source = baseCommand == null ? null : baseCommand.get_Params();
            if (source == null)
                return;

            ZXCommandDefaultParams values = CopyProperties(source, new ZXCommandDefaultParams(id));
            values.Damage = (int)Math.Round(source.Damage * damageFactor, MidpointRounding.AwayFromZero);
            values.ActionRange = source.ActionRange + 1f;
            int rangerCycle = source.TimeAction + source.TimePreAction + source.TimePostAction;
            if (rangerCycle > 0)
            {
                double desiredCycle = 1000.0 / (1000.0 / rangerCycle + 1.0);
                values.TimeAction = Math.Max(1,
                    (int)Math.Round(desiredCycle) - source.TimePreAction - source.TimePostAction);
            }
            values.AvailableFor = new List<Type> { typeof(BattleSister) };
            ZXCommandDefaultParams registeredValues = AddCommandDefaults(id, values);
            target.set_Params(registeredValues);
        }

        private static void RegisterTrainCommand()
        {
            // EntityRegistry.RegisterEntity already creates a Train command for every registered Unit.
            // The barracks discovers units through EntityDefaultParams.BuildingFrom and then calls
            // Train.For(unitType); Train commands are not ShowAsCommand entries.
            List<Train> sisterTrains = ZXGame.get_Current().Commands.OfType<Train>()
                .Where(c => c.EntityType == typeof(BattleSister)).ToList();
            Train train = sisterTrains.FirstOrDefault();
            if (train == null)
            {
                train = new Train(typeof(BattleSister));
                train.ReadParams();
                ZXGame.get_Current().Commands.Add(train);
            }
            if (train.get_Params() == null)
            {
                BattleSisterLog.Write("Registered Train command has no parameters; barracks button cannot be configured.");
                return;
            }
            // Avoid making Train appear through the generic AvailableFor command path as well.
            train.get_Params().ShowAsCommand = false;
            foreach (Train duplicate in sisterTrains.Skip(1))
                ZXGame.get_Current().Commands.Remove(duplicate);
            CCommandable.ClearAvailableCommandsCache();

            ZXEntityDefaultParams unit = ZXGame.get_Current().GetParamsFromEntityType(typeof(BattleSister));
            BattleSisterLog.Write("Configured barracks training. BuildingFrom=" +
                (unit == null || unit.BuildingFrom == null ? "none" : unit.BuildingFrom.Name) +
                "; BuildingRequisite=" +
                (unit == null || unit.BuildingRequisite == null
                    ? "unresolved"
                    : unit.BuildingRequisite.Name) +
                "; TrainCommand=" + (Train.For(typeof(BattleSister)) == null ? "missing" : "ready"));
        }

        private static T CopyProperties<T>(T source, T destination) where T : class
        {
            foreach (PropertyInfo property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!property.CanRead || !property.CanWrite || property.GetIndexParameters().Length != 0)
                    continue;
                try { property.SetValue(destination, property.GetValue(source, null), null); }
                catch { }
            }
            return destination;
        }

        private static void AddEntityDefaults(string id, ZXEntityDefaultParams values)
        {
            ZXEntityDefaultParams record;
            if (!ZXGame.get_Current().DicDefaultParamsPerTemplate.TryGetValue(id, out record))
            {
                record = new ZXEntityDefaultParams(id);
                ZXGame.get_Current().DicDefaultParamsPerTemplate[id] = record;
                ZXDefaultParams.get_All().Add(record);
            }

            // Keep the exact record created by EntityRegistry. The entity template already holds
            // this instance, so replacing the dictionary value leaves the template on stale params.
            CopyProperties(values, record);
            record.DefaultValues = values;
        }

        private static ZXCommandDefaultParams AddCommandDefaults(string id, ZXCommandDefaultParams values)
        {
            ZXCommandDefaultParams record = new ZXCommandDefaultParams(id);
            CopyProperties(values, record);
            ZXCommandDefaultParams snapshot = CopyProperties(values, new ZXCommandDefaultParams(id));
            snapshot.DefaultValues = null;
            record.DefaultValues = snapshot;
            AddDefaultRecord(id, record);
            return record;
        }

        private static void AddDefaultRecord(string id, ZXDefaultParams record)
        {
            List<ZXDefaultParams> all = ZXDefaultParams.get_All();
            for (int i = all.Count - 1; i >= 0; --i)
                if (all[i].ID == id)
                    all.RemoveAt(i);
            all.Add(record);
        }

        private void InstallAuraPatches()
        {
            _harmony = new Harmony(HarmonyId);
            MethodInfo damage = AccessTools.Method(typeof(CLife), "AddDamage",
                new[] { typeof(int), typeof(bool), typeof(ZXDamageType) });
            MethodInfo attack = AccessTools.Method(typeof(ZXCommand), "PerformAttack",
                new[] { typeof(ZXEntity), typeof(ZXEntity), typeof(PointF) });
            MethodInfo trainVisibility = AccessTools.Method(typeof(Train), "IsVisibleFor",
                new[] { typeof(ZXEntity) });
            MethodInfo applyMod = AccessTools.Method(typeof(ZXDefaultParams), "ApplyMod",
                new[] { typeof(ZXParameterModificator) });
            HarmonyMethod campaignModGuard = new HarmonyMethod(typeof(AuraPatches), "BeforeCampaignMod");
            campaignModGuard.priority = Priority.Last;
            _harmony.Patch(applyMod, prefix: campaignModGuard);
            _harmony.Patch(trainVisibility, prefix: new HarmonyMethod(typeof(AuraPatches), "BeforeTrainVisibility"));
            _harmony.Patch(damage, prefix: new HarmonyMethod(typeof(AuraPatches), "BeforeDamage"));
            _harmony.Patch(attack, prefix: new HarmonyMethod(typeof(AuraPatches), "BeforeAttack"),
                finalizer: new HarmonyMethod(typeof(AuraPatches), "RestoreAttackDamage"));

        }
    }

    internal static class BattleSisterLog
    {
        internal static void Write(string message)
        {
            string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + message + Environment.NewLine;
            try
            {
                string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    "Mods", "Battle Sister", "BattleSister.log");
                System.IO.File.AppendAllText(path, line);
            }
            catch { }
            DXLog.Write("[BattleSister] " + message);
        }
    }

    public sealed class BattleSisterAura : ZXComponent
    {
        private const int ScanIntervalMs = 250;
        private int _nextScan;
        private readonly HashSet<ulong> _previousTargets = new HashSet<ulong>();
        private DXObject _auraRange;

        public override string Description { get { return "Grants nearby Rangers temporary life and attack damage."; } }
        public override DXTypeOfComponentAvailability Availability { get { return DXTypeOfComponentAvailability.OnlyInGame; } }

        protected override void OnEntityAttached()
        {
            base.Entity.EventOnUpdate += ScanAura;
            base.Entity.EventOnUpdateSceneObjectParallel += UpdateAuraRange;
        }

        protected override void OnSceneAdded()
        {
            base.OnSceneAdded();
            _auraRange = new DXObject(Images.Effects.get__AttackRange())
            {
                Size = new SizeF(12f, 12f),
                Z = 2147483547,
                Visible = false,
                Opacity = 0.55f,
                ColorHue = Color.LightSkyBlue
            };
            _auraRange.Animation.NewClip(true, false).NewTransition(20000).DeltaAngle = 90f;
            DXIsometricGridObject.Current.IsometricMiddle.AddObject(_auraRange);
            UpdateAuraRange();
        }

        private void UpdateAuraRange()
        {
            if (_auraRange == null || Entity == null) return;
            ZXEntity sister = Entity as ZXEntity;
            _auraRange.Center = Entity.Position;
            _auraRange.Visible = sister != null && sister.get_CSelectable() != null && sister.get_CSelectable().Selected;
        }

        private void ScanAura()
        {
            int now = DXGame.Current.GameTimeInt;
            if (now < _nextScan) return;
            _nextScan = now + ScanIntervalMs;

            ZXEntity sister = Entity as ZXEntity;
            if (sister == null || sister.get_CLife() == null || !sister.get_CLife().IsAlive)
            {
                AuraState.RemoveSource(Entity.ID);
                _previousTargets.Clear();
                return;
            }

            HashSet<ulong> current = new HashSet<ulong>();
            List<DXEntity> nearby = DXWorldGrid.GetEntitiesInCircle(sister.Position, 6f, (uint)ZXBodyMask.Empire);
            if (nearby != null)
            {
                foreach (DXEntity entity in nearby)
                {
                    ZXEntity ranger = entity as ZXEntity;
                    if (ranger != null && ranger.GetType() == typeof(Ranger) && ranger.get_CLife() != null && ranger.get_CLife().IsAlive)
                        current.Add(ranger.ID);
                }
            }

            AuraState.UpdateSource(Entity.ID, _previousTargets, current);
            _previousTargets.Clear();
            foreach (ulong id in current) _previousTargets.Add(id);
        }

        protected override void OnEntityDetached()
        {
            base.OnEntityDetached();
            Entity.EventOnUpdate -= ScanAura;
            Entity.EventOnUpdateSceneObjectParallel -= UpdateAuraRange;
            AuraState.RemoveSource(Entity.ID);
            _previousTargets.Clear();
        }

        protected override void OnSceneRemoved()
        {
            base.OnSceneRemoved();
            _auraRange?.RemoveFromScene();
            _auraRange = null;
        }
    }

    internal static class AuraState
    {
        private static readonly Dictionary<ulong, HashSet<ulong>> Sources = new Dictionary<ulong, HashSet<ulong>>();
        private static readonly Dictionary<ulong, int> Shield = new Dictionary<ulong, int>();

        internal static bool HasAura(ulong rangerId) { return Sources.ContainsKey(rangerId); }

        internal static void UpdateSource(ulong sourceId, HashSet<ulong> previous, HashSet<ulong> current)
        {
            foreach (ulong id in previous)
                if (!current.Contains(id)) RemoveTargetSource(id, sourceId);
            foreach (ulong id in current)
            {
                HashSet<ulong> sources;
                if (!Sources.TryGetValue(id, out sources))
                {
                    sources = new HashSet<ulong>();
                    Sources[id] = sources;
                    Shield[id] = 100;
                }
                sources.Add(sourceId);
            }
        }

        internal static void RemoveSource(ulong sourceId)
        {
            foreach (ulong id in Sources.Keys.ToArray())
                RemoveTargetSource(id, sourceId);
        }

        private static void RemoveTargetSource(ulong rangerId, ulong sourceId)
        {
            HashSet<ulong> sources;
            if (!Sources.TryGetValue(rangerId, out sources)) return;
            sources.Remove(sourceId);
            if (sources.Count == 0)
            {
                Sources.Remove(rangerId);
                Shield.Remove(rangerId);
            }
        }

        internal static int Absorb(ulong rangerId, int incoming)
        {
            int shield;
            if (incoming <= 0 || !Shield.TryGetValue(rangerId, out shield)) return 0;
            int absorbed = Math.Min(shield, incoming);
            Shield[rangerId] = shield - absorbed;
            return absorbed;
        }

        internal static void Clear()
        {
            Sources.Clear();
            Shield.Clear();
        }
    }

    internal static class AuraPatches
    {
        internal static bool BeforeTrainVisibility(Train __instance, ref bool __result)
        {
            if (__instance != null && __instance.EntityType == typeof(BattleSister))
            {
                // Campaign saves know only vanilla mission unlocks; custom units have no unlock row.
                // BuildingFrom still restricts the command to the Soldiers Center, and Train's
                // resource validation remains active when the player queues it.
                __result = true;
                return false;
            }
            return true;
        }

        internal static bool BeforeCampaignMod(ZXParameterModificator mod)
        {
            if (mod == null || string.IsNullOrEmpty(mod.Parameter)) return true;

            Type parameterType = mod.ParametersType == ZXParameterModificator.ZXParametersType.Entity
                ? typeof(ZXEntityDefaultParams)
                : typeof(ZXCommandDefaultParams);
            PropertyInfo property = mod.Property;
            if (property != null && property.DeclaringType != null &&
                property.DeclaringType.IsAssignableFrom(parameterType))
            {
                RepairDefaultSnapshots(parameterType);
                return true;
            }

            PropertyInfo corrected = parameterType.GetProperty(mod.Parameter,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (corrected == null)
            {
                BattleSisterLog.Write("Skipped incompatible campaign modifier: " + mod +
                    "; parameter type=" + parameterType.FullName + "; property not found.");
                return false;
            }

            BattleSisterLog.Write("Corrected campaign modifier reflection property: " + mod +
                "; old declaring type=" + (property == null || property.DeclaringType == null
                    ? "null" : property.DeclaringType.FullName) +
                "; new declaring type=" + corrected.DeclaringType.FullName + ".");
            mod.Property = corrected;
            RepairDefaultSnapshots(parameterType);
            return true;
        }

        private static void RepairDefaultSnapshots(Type parameterType)
        {
            foreach (ZXDefaultParams item in ZXDefaultParams.get_All())
            {
                if (item == null || !parameterType.IsInstanceOfType(item) ||
                    (item.DefaultValues != null && parameterType.IsInstanceOfType(item.DefaultValues)))
                    continue;

                string oldSnapshotType = item.DefaultValues == null
                    ? "null" : item.DefaultValues.GetType().FullName;
                ZXDefaultParams snapshot = (ZXDefaultParams)Activator.CreateInstance(parameterType, item.ID);
                foreach (PropertyInfo member in parameterType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (!member.CanRead || !member.CanWrite || member.GetIndexParameters().Length != 0 ||
                        member.Name == "DefaultValues")
                        continue;
                    try { member.SetValue(snapshot, member.GetValue(item, null), null); }
                    catch { }
                }
                item.DefaultValues = snapshot;
                BattleSisterLog.Write("Rebuilt incompatible default snapshot for " + parameterType.Name +
                    " ID='" + item.ID + "'; prior snapshot type=" + oldSnapshotType + ".");
            }
        }

        internal static bool BeforeDamage(CLife __instance, ref int __0)
        {
            ZXEntity target = __instance.Entity as ZXEntity;
            if (target != null && target.GetType() == typeof(Ranger))
                __0 -= AuraState.Absorb(target.ID, __0);
            return true;
        }

        internal static void BeforeAttack(ZXCommand __instance, ZXEntity actor, ref int __state)
        {
            __state = -1;
            if (actor == null || actor.GetType() != typeof(Ranger) || !AuraState.HasAura(actor.ID)) return;
            ZXCommandDefaultParams parameters = __instance.get_Params();
            if (parameters == null) return;
            __state = parameters.Damage;
            parameters.Damage = (int)Math.Round(__state * 1.2f, MidpointRounding.AwayFromZero);
        }

        internal static Exception RestoreAttackDamage(ZXCommand __instance, int __state, Exception __exception)
        {
            if (__state >= 0 && __instance.get_Params() != null)
                __instance.get_Params().Damage = __state;
            return __exception;
        }
    }
}
